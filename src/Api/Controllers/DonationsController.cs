using Api.ActionFilters;
using Api.Reports;
using Dto.Donations;
using Microsoft.AspNetCore.Mvc;
using Services.Donations;
using Services.Administration;
using Services.Financial;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    public class DonationsController : BaseController
    {
        private readonly IDonationsService _donationsService;
        private readonly ILogger<DonationsController> _logger;
        private readonly IFinancialService _financialService;
        private readonly IAdministrationService _adminService;

        public DonationsController(IDonationsService donationsService, ILogger<DonationsController> logger, IFinancialService financialService, IAdministrationService adminService)
        {
            _donationsService = donationsService;
            _logger = logger;
            _financialService = financialService;
            _adminService = adminService;
        }

        [HttpGet("DonationAccounts")]
        public IActionResult DonationAccounts()
        {
            var accounts = _financialService.GetAccounts().AsEnumerable()
                .Where(account => int.TryParse(account.AccountCode, out var code) && code >= 41000 && code <= 41999)
                .OrderBy(account => account.AccountCode)
                .Select(account => new Dto.Financial.Account
                {
                    Id = account.Id,
                    AccountCode = account.AccountCode,
                    AccountName = account.AccountName
                }).ToList();
            return Ok(accounts);
        }

        [HttpGet]
        [Route("DonationInvoices")]
        public IActionResult DonationInvoices(int? donorId = null)
        {
            _logger.LogInformation("Getting all donation invoices");

            var donationInvoices = _donationsService.GetDonationInvoices();
            if (donorId.HasValue)
                donationInvoices = donationInvoices.Where(d => d.DonorId == donorId.Value).ToList();
            IList<DonationInvoice> donationInvoicesDto = new List<DonationInvoice>();

            foreach (var invoice in donationInvoices)
            {
                var invoiceDto = new DonationInvoice
                {
                    Id = invoice.Id,
                    No = invoice.No,
                    DonorId = invoice.DonorId,
                    DonorName = GetDonorName(invoice.Donor),
                    Amount = invoice.Amount,
                    PaymentType = invoice.PaymentType,
                    GlAccountId = invoice.GlAccountId,
                    GlAccountCode = invoice.GlAccount?.AccountCode ?? string.Empty,
                    GlAccountName = invoice.GlAccount?.AccountName ?? string.Empty,
                    DonationDate = invoice.Date,
                    ReferenceNo = invoice.ReferenceNo,
                    Purpose = invoice.Purpose,
                    IsTaxReceiptIssued = invoice.IsTaxReceiptIssued,
                    TaxReceiptNo = invoice.TaxReceiptNo,
                    AdvantageAmount = invoice.AdvantageAmount,
                    ReceiptIssuedDate = invoice.ReceiptIssuedDate,
                    Posted = invoice.Posted
                };

                foreach (var line in invoice.DonationInvoiceLines)
                {
                    var lineDto = new DonationInvoiceLine
                    {
                        Id = line.Id,
                        ItemId = line.ItemId,
                        MeasurementId = line.MeasurementId,
                        Quantity = line.Quantity,
                        Amount = line.Amount,
                        Notes = line.Notes
                    };
                    invoiceDto.DonationInvoiceLines!.Add(lineDto);
                }

                donationInvoicesDto.Add(invoiceDto);
            }

            return Ok(donationInvoicesDto);
        }

        [HttpGet("DonationReceipt")]
        public IActionResult DonationReceipt(int id)
        {
            var invoice = _donationsService.GetDonationInvoiceById(id);
            if (invoice == null)
                return NotFound();

            if (!invoice.IsTaxReceiptIssued || string.IsNullOrEmpty(invoice.TaxReceiptNo))
                return BadRequest("An official tax receipt has not been issued for this donation.");

            return Ok(BuildReceipt(invoice, _adminService.GetDefaultCompany()));
        }

        [HttpGet("DonationReceipts")]
        public IActionResult DonationReceipts(DateTime from, DateTime to)
        {
            if (to.Date < from.Date)
                return BadRequest("The end date cannot be before the start date.");
            var company = _adminService.GetDefaultCompany();
            var receipts = _donationsService.GetDonationInvoices()
                .Where(d => d.IsTaxReceiptIssued && !string.IsNullOrEmpty(d.TaxReceiptNo)
                    && (d.ReceiptIssuedDate ?? d.Date).Date >= from.Date && (d.ReceiptIssuedDate ?? d.Date).Date <= to.Date)
                .Select(d => BuildReceipt(d, company))
                .OrderBy(r => r.DonorName, StringComparer.CurrentCultureIgnoreCase).ThenBy(r => r.IssuedDate).ThenBy(r => r.ReceiptNo)
                .ToList();
            return Ok(receipts);
        }

        [HttpGet("DonationReceiptsPdf")]
        public IActionResult DonationReceiptsPdf(DateTime from, DateTime to)
        {
            if (to.Date < from.Date)
                return BadRequest("The end date cannot be before the start date.");
            var result = DonationReceipts(from, to) as OkObjectResult;
            var receipts = (List<DonationReceipt>)result!.Value!;
            if (receipts.Count == 0)
                return NotFound("No tax receipts were issued between the selected dates.");
            return File(DonationReceiptPdf.Generate(receipts), "application/pdf",
                $"donation-tax-receipts-{from:yyyyMMdd}-{to:yyyyMMdd}.pdf");
        }

        private static DonationReceipt BuildReceipt(Core.Domain.Donations.DonationInvoiceHeader invoice, Core.Domain.Company? company)
        {
            var donor = invoice.Donor;
            var cra = company?.CRA ?? string.Empty;
            var location = string.Join(", ", new[] { company?.City, company?.Province }.Where(part => !string.IsNullOrWhiteSpace(part)));

            return new DonationReceipt
            {
                ReceiptNo = invoice.TaxReceiptNo,
                CharityName = company?.Name ?? string.Empty,
                CharityStreet = company?.Street ?? string.Empty,
                CharityCity = company?.City ?? string.Empty,
                CharityProvince = company?.Province ?? string.Empty,
                CharityPostalCode = company?.PostalCode ?? string.Empty,
                RegistrationNumber = cra.Length == 0 || cra.Contains("RR", StringComparison.OrdinalIgnoreCase) ? cra : cra + " RR 0001",
                IssuedDate = invoice.ReceiptIssuedDate ?? invoice.Date,
                LocationIssued = location,
                DonorName = GetDonorName(donor),
                DonorStreet = donor?.Street ?? string.Empty,
                DonorCity = donor?.City ?? string.Empty,
                DonorProvince = donor?.Province ?? string.Empty,
                DonorPostalCode = donor?.PostalCode ?? string.Empty,
                DonationDate = invoice.Date,
                Amount = invoice.Amount,
                AdvantageAmount = invoice.AdvantageAmount,
                EligibleAmount = invoice.Amount - invoice.AdvantageAmount
            };
        }

        [HttpGet]
        [Route("DonationInvoice")]
        public IActionResult DonationInvoice(int id)
        {
            _logger.LogInformation("Getting donation invoice with id: {Id}", id);

            var invoice = _donationsService.GetDonationInvoiceById(id);

            if (invoice == null)
                return NotFound();

            var invoiceDto = new DonationInvoice
            {
                Id = invoice.Id,
                No = invoice.No,
                DonorId = invoice.DonorId,
                DonorName = GetDonorName(invoice.Donor),
                Amount = invoice.Amount,
                PaymentType = invoice.PaymentType,
                GlAccountId = invoice.GlAccountId,
                GlAccountCode = invoice.GlAccount?.AccountCode ?? string.Empty,
                GlAccountName = invoice.GlAccount?.AccountName ?? string.Empty,
                DonationDate = invoice.Date,
                ReferenceNo = invoice.ReferenceNo,
                Purpose = invoice.Purpose,
                IsTaxReceiptIssued = invoice.IsTaxReceiptIssued,
                TaxReceiptNo = invoice.TaxReceiptNo,
                AdvantageAmount = invoice.AdvantageAmount,
                ReceiptIssuedDate = invoice.ReceiptIssuedDate,
                Posted = invoice.Posted
            };

            foreach (var line in invoice.DonationInvoiceLines)
            {
                var lineDto = new DonationInvoiceLine
                {
                    Id = line.Id,
                    ItemId = line.ItemId,
                    MeasurementId = line.MeasurementId,
                    Quantity = line.Quantity,
                    Amount = line.Amount,
                    Notes = line.Notes
                };
                invoiceDto.DonationInvoiceLines!.Add(lineDto);
            }

            return Ok(invoiceDto);
        }

        [HttpPost("CreateDonationInvoice")]
        [ServiceFilter(typeof(ValidationFilterAttribute))]
        public IActionResult CreateDonationInvoice([FromBody] DonationInvoice donationInvoiceDto)
        {
            var result = _donationsService.CreateDonationInvoice(donationInvoiceDto);

            if (result.IsFailure)
                return BadRequest(result.Error.Message);

            return Ok(result.Value);
        }

        [HttpPost("UpdateDonationInvoice")]
        [ServiceFilter(typeof(ValidationFilterAttribute))]
        public IActionResult UpdateDonationInvoice([FromBody] DonationInvoice donationInvoiceDto)
        {
            var result = _donationsService.UpdateDonationInvoice(donationInvoiceDto);

            if (result.IsFailure)
                return BadRequest(result.Error.Message);

            return Ok(result.Value);
        }

        [HttpDelete("DeleteDonationInvoice")]
        public async Task<IActionResult> DeleteDonationInvoice(int id)
        {
            var result = await _donationsService.DeleteDonationInvoiceAsync(id);

            if (result.IsFailure)
                return BadRequest(result.Error.Message);

            return NoContent();
        }

        private static string GetDonorName(Core.Domain.Donations.Donor donor)
        {
            if (donor == null) return string.Empty;
            return donor.DonorType == "Company" ? donor.CompanyName : (donor.FirstName + " " + donor.LastName).Trim();
        }
    }
}
