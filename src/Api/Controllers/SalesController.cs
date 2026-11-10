using Api.ActionFilters;
using Core.Domain;
using Dto.Sales;
using Microsoft.AspNetCore.Mvc;
using Services.Administration;
using Services.Financial;
using Services.Inventory;
using Services.Sales;
using Services.TaxSystem;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    public class SalesController : BaseController
    {
        private readonly IAdministrationService _adminService;
        private readonly ISalesService _salesService;
        private readonly IFinancialService _financialService;
        private readonly IInventoryService _inventoryService;
        private readonly ITaxService _taxService;
        private readonly ILogger<SalesController> _logger;

        public SalesController(IAdministrationService adminService,
            ISalesService salesService,
            IFinancialService financialService,
            IInventoryService inventoryService,
            ITaxService taxService,
            ILogger<SalesController> logger)
        {
            _adminService = adminService;
            _salesService = salesService;
            _financialService = financialService;
            _inventoryService = inventoryService;
            _taxService = taxService;
            _logger = logger;
        }

        [HttpPost]
        [Route("SaveCustomer")]
        public IActionResult SaveCustomer([FromBody] Dto.Sales.Customer customerDto)
        {
            bool isNew = customerDto.Id == 0;
            Core.Domain.Sales.Customer? customer = null;

            if (isNew)
            {
                customer = new Core.Domain.Sales.Customer();

                customer.Party = new Core.Domain.Party()
                {
                    PartyType = Core.Domain.PartyTypes.Customer,
                };

                customer.PrimaryContact = new Core.Domain.Contact()
                {
                    ContactType = Core.Domain.ContactTypes.Customer,
                    Party = new Core.Domain.Party()
                    {
                        PartyType = Core.Domain.PartyTypes.Contact
                    }
                };
            }
            else
            {
                customer = _salesService.GetCustomerById(customerDto.Id);
            }

            customer.No = customerDto.No;
            customer.Party.Name = customerDto.Name;
            customer.Party.Phone = customerDto.Phone;
            customer.Party.Email = customerDto.Email;
            customer.Party.Fax = customerDto.Fax;
            customer.Party.Website = customerDto.Website;
            customer.Party.CompanyStreet = customerDto.CompanyAddress?.Street1;
            customer.Party.CompanyProvince = customerDto.CompanyAddress?.Province;
            customer.Party.CompanyPostalCode = customerDto.CompanyAddress?.PostalCode;
            customer.Party.CompanyCountry = customerDto.CompanyAddress?.Country;
            customer.Party.ShippingStreet = customerDto.ShippingAddress?.Street1;
            customer.Party.ShippingProvince = customerDto.ShippingAddress?.Province;
            customer.Party.ShippingPostalCode = customerDto.ShippingAddress?.PostalCode;
            customer.Party.ShippingCountry = customerDto.ShippingAddress?.Country;
            customer.PrimaryContact.FirstName = customerDto.PrimaryContact!.FirstName;
            customer.PrimaryContact.LastName = customerDto.PrimaryContact.LastName;
            customer.PrimaryContact.Party.Name = customerDto.PrimaryContact.Party.Name;
            customer.PrimaryContact.Party.Phone = customerDto.PrimaryContact.Party.Phone;
            customer.PrimaryContact.Party.Email = customerDto.PrimaryContact.Party.Email;
            customer.PrimaryContact.Party.Fax = customerDto.PrimaryContact.Party.Fax;
            customer.PrimaryContact.Party.Website = customerDto.PrimaryContact.Party.Website;
            customer.AccountsReceivableAccountId = customerDto.AccountsReceivableId;
            customer.SalesAccountId = customerDto.SalesAccountId;
            customer.CustomerAdvancesAccountId = customerDto.PrepaymentAccountId;
            customer.SalesDiscountAccountId = customerDto.SalesDiscountAccountId;
            customer.PaymentTermId = customerDto.PaymentTermId;
            customer.TaxGroupId = customerDto.TaxGroupId;
            customer.ModifiedBy = GetUserNameFromRequestHeader();

            if (isNew)
                _salesService.AddCustomer(customer);
            else
                _salesService.UpdateCustomer(customer);

            return Ok();
        }

        [HttpGet]
        [Route("Customer")]
        public IActionResult Customer(int id)
        {
            try
            {
                var customer = _salesService.GetCustomerById(id);

                var customerDto = new Dto.Sales.Customer()
                {
                    Id = customer.Id,
                    No = customer.No,
                    AccountsReceivableId = customer.AccountsReceivableAccountId.GetValueOrDefault(),
                    SalesAccountId = customer.SalesAccountId.GetValueOrDefault(),
                    PrepaymentAccountId = customer.CustomerAdvancesAccountId.GetValueOrDefault(),
                    SalesDiscountAccountId = customer.SalesDiscountAccountId.GetValueOrDefault(),
                    PaymentTermId = customer.PaymentTermId.GetValueOrDefault(),
                    TaxGroupId = customer.TaxGroupId.GetValueOrDefault()
                };
                customerDto.Name = customer.Party.Name;
                customerDto.Email = customer.Party.Email;
                customerDto.Website = customer.Party.Website;
                customerDto.Phone = customer.Party.Phone;
                customerDto.Fax = customer.Party.Fax;
                customerDto.CompanyAddress = new Dto.Common.Address
                {
                    Street1 = customer.Party.CompanyStreet,
                    Province = customer.Party.CompanyProvince,
                    PostalCode = customer.Party.CompanyPostalCode,
                    Country = customer.Party.CompanyCountry
                };
                customerDto.ShippingAddress = new Dto.Common.Address
                {
                    Street1 = customer.Party.ShippingStreet,
                    Province = customer.Party.ShippingProvince,
                    PostalCode = customer.Party.ShippingPostalCode,
                    Country = customer.Party.ShippingCountry
                };

                if (customer.PrimaryContact != null)
                {
                    customerDto.PrimaryContact!.FirstName = customer.PrimaryContact.FirstName;
                    customerDto.PrimaryContact.LastName = customer.PrimaryContact.LastName;
                    customerDto.PrimaryContact.Party.Email = customer.PrimaryContact.Party.Email;
                    customerDto.PrimaryContact.Party.Phone = customer.PrimaryContact.Party.Phone;
                    customerDto.PrimaryContact.Party.Fax = customer.PrimaryContact.Party.Fax;
                    customerDto.PrimaryContact.Party.Website = customer.PrimaryContact.Party.Website;
                    customerDto.PrimaryContact.Party.Name = customer.PrimaryContact.Party.Name;
                }

                return new ObjectResult(customerDto);
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex);
            }
        }

        [HttpGet]
        [Route("Customers")] // api/Sales/Customers
        public IActionResult Customers()
        {
            IList<Dto.Sales.Customer> customersDto = new List<Dto.Sales.Customer>();
            try
            {
                var customers = _salesService.GetCustomers().Where(p => p.Party != null);
                foreach (var customer in customers)
                {
                    var customerDto = new Dto.Sales.Customer()
                    {
                        Id = customer.Id,
                        No = customer.No,
                    };

                    customerDto.Name = customer.Party.Name;
                    customerDto.Email = customer.Party.Email;
                    customerDto.Website = customer.Party.Website;
                    customerDto.Phone = customer.Party.Phone;
                    customerDto.Fax = customer.Party.Fax;
                    customerDto.Balance = customer.Balance;
                    customerDto.PrepaymentAccountId = customer.CustomerAdvancesAccountId;
                    customerDto.Contact = customer.PrimaryContact.FirstName + " " + customer.PrimaryContact.LastName;
                    customerDto.TaxGroup = customer.TaxGroup == null ? string.Empty : customer.TaxGroup.Description;
                    customersDto.Add(customerDto);
                }

                return new ObjectResult(customersDto);
            }
            catch (Exception ex)
            {
                return new ObjectResult(ex);
            }
        }

        [HttpGet]
        [Route("SalesOrders")] // api/Sales/SalesOrders
        public IActionResult SalesOrders()
        {
            var salesOrders = _salesService.GetSalesOrders();
            IList<Dto.Sales.SalesOrder> salesOrdersDto = new List<Dto.Sales.SalesOrder>();


            try
            {
                foreach (var salesOrder in salesOrders)
                {
                    var salesOrderDto = new Dto.Sales.SalesOrder()
                    {
                        Id = salesOrder.Id,
                        PaymentTermId = salesOrder.PaymentTermId,
                        CustomerId = salesOrder.CustomerId!.Value,
                        CustomerNo = salesOrder.Customer.No,
                        CustomerName = salesOrder.Customer.Party.Name,
                        OrderDate = salesOrder.Date,
                        ReferenceNo = salesOrder.ReferenceNo,
                        StatusId = (int)salesOrder.Status.GetValueOrDefault(),
                        No = salesOrder.No
                    };

                    foreach (var line in salesOrder.SalesOrderLines)
                    {
                        var lineDto = new Dto.Sales.SalesOrderLine()
                        {
                            ItemId = line.ItemId,
                            MeasurementId = line.MeasurementId,
                            Quantity = line.Quantity,
                            Amount = line.Amount,
                            Discount = line.Discount,
                            RemainingQtyToInvoice = line.GetRemainingQtyToInvoice()
                        };
                        salesOrderDto.SalesOrderLines.Add(lineDto);
                    }

                    salesOrdersDto.Add(salesOrderDto);
                }

                return new ObjectResult(salesOrdersDto);
            }
            catch (Exception ex)
            {
                return new ObjectResult(ex);
            }
        }

        [HttpGet]
        [Route("SalesOrder")]
        public IActionResult SalesOrder(int id)
        {
            try
            {
                var salesOrder = _salesService.GetSalesOrderById(id);

                if (salesOrder == null)
                {
                    return new NotFoundObjectResult(new { error = "Sales order not found." });
                }

                var salesOrderDto = new Dto.Sales.SalesOrder()
                {
                    Id = salesOrder.Id,
                    CustomerId = salesOrder.CustomerId ?? 0,
                    CustomerNo = salesOrder.Customer?.No ?? "N/A",
                    CustomerName = salesOrder.Customer?.Party?.Name ?? "Unknown",
                    OrderDate = salesOrder.Date,
                    PaymentTermId = salesOrder.PaymentTermId ?? 0,
                    ReferenceNo = salesOrder.ReferenceNo,
                    StatusId = salesOrder.Status.HasValue ? (int)salesOrder.Status.Value : 0,
                    SalesOrderLines = new List<Dto.Sales.SalesOrderLine>()
                };

                if (salesOrder.SalesOrderLines != null)
                {
                    foreach (var line in salesOrder.SalesOrderLines)
                    {
                        var lineDto = new Dto.Sales.SalesOrderLine();
                        lineDto.Id = line.Id;
                        lineDto.Amount = line.Amount;
                        lineDto.Discount = line.Discount;
                        lineDto.Quantity = line.Quantity;
                        lineDto.ItemId = line.ItemId;
                        lineDto.ItemDescription = line.Item?.Description ?? "Unknown";
                        lineDto.MeasurementId = line.MeasurementId;
                        lineDto.MeasurementDescription = line.Measurement?.Description ?? "Unknown";
                        lineDto.RemainingQtyToInvoice = line.GetRemainingQtyToInvoice();

                        salesOrderDto.SalesOrderLines.Add(lineDto);
                    }
                }

                return new ObjectResult(salesOrderDto);
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex.ToString());
                return new BadRequestObjectResult(new { error = "An error occurred while loading the sales order.", details = ex.Message });
            }
        }

        [HttpGet]
        [Route("SalesInvoice")]
        public IActionResult SalesInvoice(int id)
        {
            try
            {
                var salesInvoice = _salesService.GetSalesInvoiceById(id);
                if (salesInvoice == null)
                    return NotFound(new { error = "The invoice no longer exists." });

                var salesInvoiceDto = new Dto.Sales.SalesInvoice()
                {
                    Id = salesInvoice.Id,
                    CustomerId = salesInvoice.CustomerId,
                    CustomerName = salesInvoice.Customer.Party.Name,
                    No = salesInvoice.No,
                    InvoiceDate = salesInvoice.Date,
                    SalesInvoiceLines = new List<Dto.Sales.SalesInvoiceLine>(),
                    PaymentTermId = salesInvoice.PaymentTermId,
                    ReferenceNo = salesInvoice.ReferenceNo,
                    Shipping = salesInvoice.ShippingHandlingCharge,
                    Posted = salesInvoice.GeneralLedgerHeaderId != null
                };

                foreach (var line in salesInvoice.SalesInvoiceLines)
                {
                    var lineDto = new Dto.Sales.SalesInvoiceLine();
                    lineDto.Id = line.Id;
                    lineDto.Amount = line.Amount;
                    lineDto.Discount = line.Discount;
                    lineDto.Quantity = line.Quantity;
                    lineDto.ItemId = line.ItemId;
                    lineDto.MeasurementId = line.MeasurementId;
                    lineDto.ItemDescription = line.Item.Description;
                    lineDto.MeasurementDescription = line.Measurement.Description;

                    salesInvoiceDto.SalesInvoiceLines.Add(lineDto);
                }

                salesInvoiceDto.TotalTax = _financialService.ComputeSalesInvoiceTaxes(salesInvoice).Sum(t => t.Value);
                salesInvoiceDto.TotalAmountAfterTax = salesInvoiceDto.TotalInvoice;

                // is this journal entry ready for posting?
                if (!salesInvoiceDto.Posted && salesInvoiceDto.SalesInvoiceLines.Count >= 1)
                {
                    salesInvoiceDto.ReadyForPosting = true;
                }

                return new ObjectResult(salesInvoiceDto);
            }
            catch (Exception ex)
            {
                return new ObjectResult(ex);
            }
        }

        [HttpPost]
        [Route("addsalesorder")]
        public IActionResult AddSalesOrder([FromBody] Dto.Sales.SalesOrder salesorderDto)
        {
            try
            {
                var salesOrder = new Core.Domain.Sales.SalesOrderHeader()
                {
                    CustomerId = salesorderDto.CustomerId,
                    Date = salesorderDto.OrderDate,
                    PaymentTermId = salesorderDto.PaymentTermId,
                    ReferenceNo = salesorderDto.ReferenceNo
                };

                foreach (var line in salesorderDto.SalesOrderLines)
                {
                    var salesOrderLine = new Core.Domain.Sales.SalesOrderLine();
                    salesOrderLine.Amount = line.Amount.GetValueOrDefault();
                    salesOrderLine.Discount = line.Discount.GetValueOrDefault();
                    salesOrderLine.Quantity = line.Quantity.GetValueOrDefault();
                    salesOrderLine.ItemId = line.ItemId.GetValueOrDefault();
                    salesOrderLine.MeasurementId = line.MeasurementId.GetValueOrDefault();

                    salesOrder.SalesOrderLines.Add(salesOrderLine);
                }

                _salesService.AddSalesOrder(salesOrder, true);

                salesorderDto.Id = salesOrder.Id;

                return new ObjectResult(salesorderDto);
            }
            catch (Exception ex)
            {
                return new ObjectResult(ex);
            }
        }

        [HttpGet]
        [Route("Quotations")] // api/Sales/Quotations
        public IActionResult Quotations()
        {
            var quotes = _salesService.GetSalesQuotes();

            var quoteDtos = new List<Dto.Sales.SalesQuotation>();

            foreach (var quote in quotes)
            {
                var quoteDto = new Dto.Sales.SalesQuotation()
                {
                    Id = quote.Id,
                    No = quote.No,
                    CustomerId = quote.CustomerId,
                    CustomerName = quote.Customer.Party.Name,
                    PaymentTermId = quote.PaymentTermId,
                    QuotationDate = quote.Date,
                    ReferenceNo = quote.ReferenceNo,
                    SalesQuoteStatus = quote.Status.ToString(),
                    StatusId = (int)quote.Status!
                };

                foreach (var line in quote.SalesQuoteLines)
                {
                    var lineDto = new Dto.Sales.SalesQuotationLine()
                    {
                        ItemId = line.ItemId,
                        MeasurementId = line.MeasurementId,
                        Quantity = line.Quantity,
                        Amount = line.Amount,
                        Discount = line.Discount
                    };
                    quoteDto.SalesQuotationLines.Add(lineDto);
                }

                quoteDtos.Add(quoteDto);
            }

            return new ObjectResult(quoteDtos.OrderBy(q => q.Id).Reverse());
        }

        [HttpGet]
        [Route("Quotation")]
        public IActionResult Quotation(int id)
        {
            var quote = _salesService.GetSalesQuotationById(id);

            var quoteDto = new Dto.Sales.SalesQuotation()
            {
                Id = quote.Id,
                CustomerId = quote.CustomerId,
                CustomerName = quote.Customer.Party.Name,
                QuotationDate = quote.Date,
                PaymentTermId = quote.PaymentTermId,
                ReferenceNo = quote.ReferenceNo,
                StatusId = (int)quote.Status!
            };

            foreach (var line in quote.SalesQuoteLines)
            {
                var lineDto = new Dto.Sales.SalesQuotationLine()
                {
                    Id = line.Id,
                    ItemId = line.ItemId,
                    MeasurementId = line.MeasurementId,
                    Quantity = line.Quantity,
                    Amount = line.Amount,
                    Discount = line.Discount,
                    ItemDescription = line.Item.Description,
                    MeasurementDescription = line.Measurement.Description
                };

                _logger.LogInformation("Quotation line: " + lineDto.ItemDescription);

                quoteDto.SalesQuotationLines.Add(lineDto);
            }

            return new ObjectResult(quoteDto);
        }

        [HttpGet]
        [Route("SalesInvoices")] // api/Sales/SalesInvoices
        public IActionResult SalesInvoices()
        {
            var salesInvoices = _salesService.GetSalesInvoices();
            IList<Dto.Sales.SalesInvoice> salesInvoicesDto = new List<Dto.Sales.SalesInvoice>();

            foreach (var salesInvoice in salesInvoices)
            {
                var salesInvoiceDto = new Dto.Sales.SalesInvoice()
                {
                    Id = salesInvoice.Id,
                    No = salesInvoice.No,
                    CustomerId = salesInvoice.CustomerId,
                    CustomerName = salesInvoice.Customer?.Party?.Name ?? "Unknown",
                    InvoiceDate = salesInvoice.Date,
                    ReferenceNo = salesInvoice.ReferenceNo,
                    TotalAllocatedAmount = (decimal)salesInvoice.CustomerAllocations.Sum(i => i.Amount),
                    Posted = salesInvoice.GeneralLedgerHeaderId != null
                };

                // Populate line items - Amount will be calculated automatically by the DTO
                foreach (var line in salesInvoice.SalesInvoiceLines)
                {
                    var lineDto = new Dto.Sales.SalesInvoiceLine()
                    {
                        ItemId = line.ItemId,
                        MeasurementId = line.MeasurementId,
                        Quantity = line.Quantity,
                        Amount = line.Amount,
                        Discount = line.Discount
                    };
                    salesInvoiceDto.SalesInvoiceLines!.Add(lineDto);
                }

                salesInvoicesDto.Add(salesInvoiceDto);
            }

            return new ObjectResult(salesInvoicesDto);
        }

        /// <summary>
        /// Gets invoices for all customers that still have an unpaid balance, oldest first.
        /// </summary>
        /// <returns>The outstanding invoices.</returns>
        [HttpGet("OutstandingInvoices")] // api/Sales/OutstandingInvoices
        public IActionResult OutstandingInvoices()
        {
            var outstanding = new List<Dto.Sales.OutstandingInvoice>();

            foreach (var salesInvoice in _salesService.GetSalesInvoices())
            {
                var invoice = ToOutstandingInvoice(salesInvoice);

                if (invoice.Balance > 0)
                    outstanding.Add(invoice);
            }

            return Ok(outstanding
                .OrderBy(invoice => invoice.InvoiceDate)
                .ThenBy(invoice => invoice.No)
                .ToList());
        }

        /// <summary>
        /// Builds the invoice totals and unpaid balance used by the outstanding invoice list and payment allocation.
        /// </summary>
        /// <param name="salesInvoice">The invoice with its lines, customer and allocations loaded.</param>
        /// <returns>The invoice summary including its remaining balance.</returns>
        private Dto.Sales.OutstandingInvoice ToOutstandingInvoice(Core.Domain.Sales.SalesInvoiceHeader salesInvoice) => new()
        {
            Id = salesInvoice.Id,
            No = salesInvoice.No,
            CustomerId = salesInvoice.CustomerId,
            CustomerName = salesInvoice.Customer?.Party?.Name ?? "Unknown",
            InvoiceDate = salesInvoice.Date,
            Subtotal = Math.Round(salesInvoice.SalesInvoiceLines.Sum(line =>
                line.Quantity * line.Amount * (1 - line.Discount / 100)), 2, MidpointRounding.ToEven),
            Shipping = salesInvoice.ShippingHandlingCharge,
            Tax = _financialService.ComputeSalesInvoiceTaxes(salesInvoice).Sum(tax => tax.Value),
            Paid = salesInvoice.CustomerAllocations.Sum(allocation => allocation.Amount),
            Posted = salesInvoice.GeneralLedgerHeaderId != null
        };

        [HttpGet]
        [Route("SalesReceipts")] // api/Sales/SalesReceipts
        public IActionResult SalesReceipts()
        {
            var salesReceipts = _salesService.GetSalesReceipts();
            var invoiceNumbers = _salesService.GetSalesInvoices().ToDictionary(i => i.Id, i => i.No);
            IList<Dto.Sales.SalesReceipt> salesReceiptsDto = new List<Dto.Sales.SalesReceipt>();

            foreach (var salesReceipt in salesReceipts)
            {
                var appliedTo = salesReceipt.CustomerAllocations
                    .Select(a => invoiceNumbers.TryGetValue(a.SalesInvoiceHeaderId, out var no) ? $"#{no}" : null)
                    .Where(no => no != null)
                    .Distinct();

                var salesReceiptDto = new Dto.Sales.SalesReceipt()
                {
                    Id = salesReceipt.Id,
                    ReceiptNo = salesReceipt.No,
                    CustomerId = salesReceipt.CustomerId,
                    CustomerName = salesReceipt.Customer?.Party.Name,
                    ReceiptDate = salesReceipt.Date,
                    Amount = salesReceipt.Amount,
                    RemainingAmountToAllocate = salesReceipt.AvailableAmountToAllocate,
                    AppliedToInvoices = string.Join(", ", appliedTo),
                    PaymentMethod = salesReceipt.PaymentMethod,
                    ReferenceNo = salesReceipt.ReferenceNo,
                    Memo = salesReceipt.Memo
                };

                salesReceiptsDto.Add(salesReceiptDto);
            }

            return new ObjectResult(salesReceiptsDto);
        }

        [HttpGet]
        [Route("SalesReceipt")]
        public IActionResult SalesReceipt(int id)
        {
            var salesReceipt = _salesService.GetSalesReceiptById(id);
            var creditAccountId = salesReceipt.SalesReceiptLines.FirstOrDefault()?.AccountToCreditId ?? 0;
            var debitCashBankId = _financialService.GetCashAndBanks().Where(cb => cb.AccountId == salesReceipt.AccountToDebitId).FirstOrDefault()?.Id ?? 0;

            var salesReceiptDto = new Dto.Sales.SalesReceipt()
            {
                Id = salesReceipt.Id,
                ReceiptNo = salesReceipt.No,
                CustomerId = salesReceipt.CustomerId,
                CustomerName = salesReceipt.Customer?.Party.Name,
                ReceiptDate = salesReceipt.Date,
                Amount = salesReceipt.Amount,
                RemainingAmountToAllocate = salesReceipt.AvailableAmountToAllocate,
                AccountToDebitId = debitCashBankId,
                AccountToCreditId = creditAccountId,
                PaymentMethod = salesReceipt.PaymentMethod,
                ReferenceNo = salesReceipt.ReferenceNo,
                Memo = salesReceipt.Memo
            };

            return new ObjectResult(salesReceiptDto);
        }

        [HttpGet]
        [Route("CustomerInvoices")] // api/Sales/CustomerInvoices
        public IActionResult CustomerInvoices(int id)
        {
            try
            {
                var invoices = _salesService.GetCustomerInvoices(id);

                var invoicesDto = new HashSet<Dto.Sales.SalesInvoice>();

                foreach (var invoice in invoices)
                {
                    var invoiceDto = new Dto.Sales.SalesInvoice()
                    {
                        Id = invoice.Id,
                        InvoiceDate = invoice.Date,
                        CustomerId = invoice.CustomerId,
                        TotalAllocatedAmount = (decimal)invoice.CustomerAllocations.Sum(i => i.Amount),
                        Posted = invoice.GeneralLedgerHeaderId.HasValue
                    };

                    foreach (var line in invoice.SalesInvoiceLines)
                    {
                        var lineDto = new Dto.Sales.SalesInvoiceLine();
                        lineDto.Id = line.Id;
                        lineDto.Amount = line.Amount;
                        lineDto.Discount = line.Discount;
                        lineDto.Quantity = line.Quantity;
                        lineDto.ItemId = line.ItemId;
                        lineDto.MeasurementId = line.MeasurementId;

                        invoiceDto.SalesInvoiceLines!.Add(lineDto);
                    }

                    invoicesDto.Add(invoiceDto);
                }

                return new ObjectResult(invoicesDto);
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex);
            }
        }

        [HttpPost]
        [Route("SaveSalesOrder")]
        public IActionResult SaveSalesOrder([FromBody] Dto.Sales.SalesOrder salesOrderDto)
        {
            string[]? errors = null;
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState.Values.SelectMany(value => value.Errors)
                        .Select(error => error.ErrorMessage).ToArray());
                }

                bool isNew = salesOrderDto.Id == 0;
                Core.Domain.Sales.SalesOrderHeader? salesOrder = null;

                if (isNew)
                {
                    salesOrder = new Core.Domain.Sales.SalesOrderHeader();
                    salesOrder.Status = SalesOrderStatus.Open;

                    if (salesOrderDto.QuotationId != null)
                    {
                        var quotation = _salesService.GetSalesQuotationById(salesOrderDto.QuotationId.Value);
                        quotation.Status = SalesQuoteStatus.ClosedOrderCreated;
                        _salesService.UpdateSalesQuote(quotation);
                    }

                }
                else
                {
                    salesOrder = _salesService.GetSalesOrderById(salesOrderDto.Id);
                    if (salesOrder == null)
                        return NotFound(new { error = "Sales order not found. Your changes were not saved." });
                }

                salesOrder.CustomerId = salesOrderDto.CustomerId;
                salesOrder.Date = salesOrderDto.OrderDate;
                salesOrder.PaymentTermId = salesOrderDto.PaymentTermId;
                salesOrder.ReferenceNo = salesOrderDto.ReferenceNo;

                foreach (var line in salesOrderDto.SalesOrderLines)
                {
                    if (!isNew)
                    {
                        var existingLine = line.Id > 0
                            ? salesOrder.SalesOrderLines.FirstOrDefault(existing => existing.Id == line.Id)
                            : null;
                        if (existingLine != null)
                        {
                            existingLine!.Amount = line.Amount.GetValueOrDefault();
                            existingLine.Discount = line.Discount.GetValueOrDefault();
                            existingLine.Quantity = line.Quantity.GetValueOrDefault();
                            existingLine.ItemId = line.ItemId.GetValueOrDefault();
                            existingLine.MeasurementId = line.MeasurementId.GetValueOrDefault();
                        }
                        else
                        {
                            var salesOrderLine = new Core.Domain.Sales.SalesOrderLine();
                            salesOrderLine.Amount = line.Amount.GetValueOrDefault();
                            salesOrderLine.Discount = line.Discount.GetValueOrDefault();
                            salesOrderLine.Quantity = line.Quantity.GetValueOrDefault();
                            salesOrderLine.ItemId = line.ItemId.GetValueOrDefault();
                            salesOrderLine.MeasurementId = line.MeasurementId.GetValueOrDefault();

                            salesOrder.SalesOrderLines.Add(salesOrderLine);
                        }
                    }
                    else
                    {
                        var salesOrderLine = new Core.Domain.Sales.SalesOrderLine();
                        salesOrderLine.Amount = line.Amount.GetValueOrDefault();
                        salesOrderLine.Discount = line.Discount.GetValueOrDefault();
                        salesOrderLine.Quantity = line.Quantity.GetValueOrDefault();
                        salesOrderLine.ItemId = line.ItemId.GetValueOrDefault();
                        salesOrderLine.MeasurementId = line.MeasurementId.GetValueOrDefault();

                        salesOrder.SalesOrderLines.Add(salesOrderLine);
                    }
                }

                if (isNew)
                {
                    _salesService.AddSalesOrder(salesOrder, true);
                }
                else
                {
                    var deleted = (from line in salesOrder.SalesOrderLines
                                   where line.Id > 0 && !salesOrderDto.SalesOrderLines.Any(x => x.Id == line.Id)
                                   select line).ToList();

                    foreach (var line in deleted)
                    {
                        if (line.SalesInvoiceLines.Count() > 0)
                            throw new Exception("The line cannot be deleted. An invoice line is created from the item.");
                    }

                    foreach (var line in deleted)
                    {
                        salesOrder.SalesOrderLines.Remove(line);
                    }

                    _salesService.UpdateSalesOrder(salesOrder);
                }


                return new OkObjectResult(Ok());
            }
            catch (Exception ex)
            {
                errors = new string[1] { ex.Message };
                return new BadRequestObjectResult(errors);
            }
        }

        [HttpPost]
        [Route("PostSalesInvoice")]
        public IActionResult PostSalesInvoice([FromBody] Dto.Sales.SalesInvoice salesInvoiceDto)
        {
            string[]? errors = null;

            _logger.LogInformation("PostSalesInvoice");

            try
            {
                if (!ModelState.IsValid)
                {
                    errors = new string[ModelState.ErrorCount];
                    foreach (var val in ModelState.Values)
                        for (int i = 0; i < ModelState.ErrorCount; i++)
                            errors[i] = val.Errors[i].ErrorMessage;

                    return new BadRequestObjectResult(errors);
                }

                _salesService.PostSalesInvoice(salesInvoiceDto.Id);

                return new ObjectResult(Ok());
            }
            catch (Exception ex)
            {
                errors = new string[1] { ex.InnerException != null ? ex.InnerException.Message : ex.Message };
                return new BadRequestObjectResult(errors);
            }
        }

        /// <summary>Suggests the next positive invoice number without reserving it.</summary>
        /// <returns>One for an empty invoice ledger, otherwise the next numeric invoice number.</returns>
        [HttpGet("NextInvoiceNumber")]
        public IActionResult NextInvoiceNumber()
        {
            var maximum = _salesService.GetSalesInvoices()
                .Select(invoice => long.TryParse(invoice.No, out var number) && number > 0 ? number : 0)
                .DefaultIfEmpty(0).Max();
            return Ok(checked(maximum + 1));
        }

        /// <summary>Calculates customer tax added to discounted invoice lines and shipping.</summary>
        /// <param name="request">The customer and invoice lines to evaluate.</param>
        /// <returns>The rounded tax amount and the customer's configured tax percentages.</returns>
        [HttpPost("CalculateInvoiceTax")]
        public IActionResult CalculateInvoiceTax([FromBody] Dto.Sales.InvoiceTaxRequest request)
        {
            if (request == null || request.CustomerId <= 0)
                return BadRequest(new[] { "Select a customer to calculate invoice tax." });

            if (request.Shipping < 0)
                return BadRequest(new[] { "Shipping cannot be negative." });
            var invoice = new Core.Domain.Sales.SalesInvoiceHeader
            {
                CustomerId = request.CustomerId,
                ShippingHandlingCharge = request.Shipping
            };
            foreach (var line in request.Lines
                .Where(line => line.ItemId > 0 && line.Quantity > 0 && line.Amount > 0))
                invoice.SalesInvoiceLines.Add(new Core.Domain.Sales.SalesInvoiceLine
                {
                    ItemId = line.ItemId, Quantity = line.Quantity, Amount = line.Amount,
                    Discount = Math.Clamp(line.Discount, 0, 100)
                });
            var tax = _financialService.ComputeSalesInvoiceTaxes(invoice).Sum(t => t.Value);
            return Ok(new Dto.Sales.InvoiceTaxResult
            {
                Amount = Math.Round(tax, 2, MidpointRounding.ToEven),
                Rates = _taxService.GetCustomerTaxes(request.CustomerId).Select(t => t.Rate).ToList()
            });
        }

        [HttpPost]
        [Route("SaveSalesInvoice")]
        public IActionResult SaveSalesInvoice([FromBody] Dto.Sales.SalesInvoice salesInvoiceDto)
        {
            string[]? errors = null;

            try
            {
                if (!ModelState.IsValid)
                {
                    errors = new string[ModelState.ErrorCount];
                    foreach (var val in ModelState.Values)
                        for (int i = 0; i < ModelState.ErrorCount; i++)
                            errors[i] = val.Errors[i].ErrorMessage;

                    return new BadRequestObjectResult(errors);
                }

                bool isNew = salesInvoiceDto.Id == 0;
                if (salesInvoiceDto.Shipping < 0)
                    return BadRequest(new[] { "Shipping cannot be negative." });
                string? invoiceNumber = null;
                if (!string.IsNullOrWhiteSpace(salesInvoiceDto.No))
                {
                    if (!long.TryParse(salesInvoiceDto.No, out var number) || number <= 0)
                        return BadRequest(new[] { "Invoice ID must be a positive whole number." });
                    invoiceNumber = number.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    if (_salesService.GetSalesInvoices().Any(invoice => invoice.Id != salesInvoiceDto.Id &&
                        (invoice.No == invoiceNumber ||
                         (long.TryParse(invoice.No, out var existingNumber) && existingNumber == number))))
                        return Conflict(new[] { "This invoice number already exists. Enter a different Invoice ID." });
                }
                Core.Domain.Sales.SalesInvoiceHeader? salesInvoice = null;
                Core.Domain.Sales.SalesOrderHeader? salesOrder = null;

                // Creating a new invoice
                if (isNew)
                {
                    if (!salesInvoiceDto.FromSalesOrderId.HasValue)
                    {
                        salesOrder = new Core.Domain.Sales.SalesOrderHeader();
                        salesOrder.Date = salesInvoiceDto.InvoiceDate;
                        salesOrder.PaymentTermId = salesInvoiceDto.PaymentTermId;
                        salesOrder.CustomerId = salesInvoiceDto.CustomerId;
                        salesOrder.ReferenceNo = salesInvoiceDto.ReferenceNo;
                        salesOrder.Status = SalesOrderStatus.FullyInvoiced;
                    }
                    else
                    {
                        // else,  your invoice is created from existing (open) sales order.
                        salesOrder = _salesService.GetSalesOrderById(salesInvoiceDto.FromSalesOrderId.GetValueOrDefault());
                        if (salesOrder == null)
                            return NotFound(new { error = "The source sales order no longer exists." });
                        if (salesInvoiceDto.CustomerId != salesOrder.CustomerId)
                            return BadRequest(new[] { "The invoice customer must match the source sales order." });
                        if (salesInvoiceDto.SalesInvoiceLines == null || salesInvoiceDto.SalesInvoiceLines.Count == 0)
                            return BadRequest(new[] { "Enter at least one invoice line." });

                        foreach (var group in salesInvoiceDto.SalesInvoiceLines.Where(line => line.Id > 0).GroupBy(line => line.Id))
                        {
                            var sourceLine = salesOrder.SalesOrderLines.FirstOrDefault(line => line.Id == group.Key);
                            if (sourceLine == null)
                                return BadRequest(new[] { "An invoice line does not belong to the selected sales order." });
                            if (group.Any(line => line.ItemId != sourceLine.ItemId || line.MeasurementId != sourceLine.MeasurementId))
                                return BadRequest(new[] { "Ordered invoice lines must retain their original item and unit." });
                            if (group.Any(line => line.Quantity.GetValueOrDefault() <= 0) ||
                                group.Sum(line => line.Quantity.GetValueOrDefault()) > sourceLine.GetRemainingQtyToInvoice())
                                return BadRequest(new[] { "Invoice quantity must be positive and cannot exceed the remaining order quantity. Reload the order before retrying." });
                        }
                    }

                    // populate invoice header
                    salesInvoice = new Core.Domain.Sales.SalesInvoiceHeader();
                    salesInvoice.No = invoiceNumber!;
                    salesInvoice.CustomerId = salesInvoiceDto.CustomerId;
                    salesInvoice.Date = salesInvoiceDto.InvoiceDate;
                    salesInvoice.PaymentTermId = salesInvoiceDto.PaymentTermId;
                    salesInvoice.ReferenceNo = salesInvoiceDto.ReferenceNo;
                    salesInvoice.ShippingHandlingCharge = salesInvoiceDto.Shipping;

                    foreach (var line in salesInvoiceDto.SalesInvoiceLines!)
                    {
                        var salesInvoiceLine = new Core.Domain.Sales.SalesInvoiceLine();

                        salesInvoiceLine.Amount = line.Amount.GetValueOrDefault();
                        salesInvoiceLine.Discount = line.Discount.GetValueOrDefault();
                        salesInvoiceLine.Quantity = line.Quantity.GetValueOrDefault();
                        salesInvoiceLine.ItemId = line.ItemId.GetValueOrDefault();
                        salesInvoiceLine.MeasurementId = line.MeasurementId.GetValueOrDefault();
                        salesInvoice.SalesInvoiceLines.Add(salesInvoiceLine);

                        // line.Id here is referring to SalesOrderLineId. It is pre-populated when you create a new sales invoice from sales order.
                        if (line.Id != 0)
                        {
                            salesInvoiceLine.SalesOrderLineId = line.Id;
                        }
                        else
                        {
                            // if you reach here, this line item is newly added to invoice which is not originally in sales order. create correspondin orderline and add to sales order.
                            var salesOrderLine = new Core.Domain.Sales.SalesOrderLine();
                            salesOrderLine.Amount = line.Amount.GetValueOrDefault();
                            salesOrderLine.Discount = line.Discount.GetValueOrDefault();
                            salesOrderLine.Quantity = line.Quantity.GetValueOrDefault();
                            salesOrderLine.ItemId = line.ItemId.GetValueOrDefault();
                            salesOrderLine.MeasurementId = line.MeasurementId.GetValueOrDefault();
                            salesInvoiceLine.SalesOrderLine = salesOrderLine;

                            salesOrder.SalesOrderLines.Add(salesOrderLine);

                            salesInvoiceLine.SalesOrderLine = salesOrderLine; // map invoice line to newly added orderline
                        }
                    }
                }
                else
                {
                    // if you reach here, you are updating existing invoice.
                    salesInvoice = _salesService.GetSalesInvoiceById(salesInvoiceDto.Id);
                    if (salesInvoice == null)
                        return NotFound(new[] { "The invoice no longer exists." });

                    if (salesInvoice.GeneralLedgerHeaderId.HasValue)
                        throw new Exception("Invoice is already posted. Update is not allowed.");
                    if (salesInvoiceDto.SalesInvoiceLines == null || salesInvoiceDto.SalesInvoiceLines.Count == 0)
                        return BadRequest(new[] { "Enter at least one invoice line." });

                    if (invoiceNumber != null)
                        salesInvoice.No = invoiceNumber;
                    salesInvoice.Date = salesInvoiceDto.InvoiceDate;
                    salesInvoice.PaymentTermId = salesInvoiceDto.PaymentTermId;
                    salesInvoice.ReferenceNo = salesInvoiceDto.ReferenceNo;
                    salesInvoice.CustomerId = salesInvoiceDto.CustomerId;
                    salesInvoice.ShippingHandlingCharge = salesInvoiceDto.Shipping;

                    foreach (var line in salesInvoiceDto.SalesInvoiceLines!)
                    {
                        var existingLine = salesInvoice.SalesInvoiceLines.Where(id => id.Id == line.Id).FirstOrDefault();
                        if (salesInvoice.SalesInvoiceLines.Where(id => id.Id == line.Id).FirstOrDefault() != null)
                        {
                            existingLine!.Amount = line.Amount.GetValueOrDefault();
                            existingLine.Discount = line.Discount.GetValueOrDefault();
                            existingLine.Quantity = line.Quantity.GetValueOrDefault();
                            existingLine.ItemId = line.ItemId.GetValueOrDefault();
                            existingLine.MeasurementId = line.MeasurementId.GetValueOrDefault();
                        }
                        else
                        {
                            //if you reach here, this line item is newly added to invoice. also, it has no SalesOrderLineId.
                            var salesInvoiceLine = new Core.Domain.Sales.SalesInvoiceLine();
                            salesInvoiceLine.Amount = line.Amount.GetValueOrDefault();
                            salesInvoiceLine.Discount = line.Discount.GetValueOrDefault();
                            salesInvoiceLine.Quantity = line.Quantity.GetValueOrDefault();
                            salesInvoiceLine.ItemId = line.ItemId.GetValueOrDefault();
                            salesInvoiceLine.MeasurementId = line.MeasurementId.GetValueOrDefault();
                            salesInvoice.SalesInvoiceLines.Add(salesInvoiceLine);

                            // add a new order line.
                            var salesOrderLine = new Core.Domain.Sales.SalesOrderLine();
                            salesOrderLine.Amount = line.Amount.GetValueOrDefault();
                            salesOrderLine.Discount = line.Discount.GetValueOrDefault();
                            salesOrderLine.Quantity = line.Quantity.GetValueOrDefault();
                            salesOrderLine.ItemId = line.ItemId.GetValueOrDefault();
                            salesOrderLine.MeasurementId = line.MeasurementId.GetValueOrDefault();

                            // Each invoice belongs to one sales order; attach the new line to that order.
                            if (salesOrder == null)
                            {
                                var linkedOrderLineId = salesInvoice.SalesInvoiceLines
                                    .Select(invoiceLine => invoiceLine.SalesOrderLineId)
                                    .FirstOrDefault(id => id.HasValue);
                                salesOrder = linkedOrderLineId.HasValue
                                    ? _salesService.GetSalesOrderLineById(linkedOrderLineId.Value)?.SalesOrderHeader
                                    : null;
                                salesOrder ??= new Core.Domain.Sales.SalesOrderHeader
                                {
                                    Date = salesInvoiceDto.InvoiceDate,
                                    PaymentTermId = salesInvoiceDto.PaymentTermId,
                                    CustomerId = salesInvoiceDto.CustomerId,
                                    ReferenceNo = salesInvoiceDto.ReferenceNo,
                                    Status = SalesOrderStatus.FullyInvoiced
                                };
                            }

                            salesOrder.SalesOrderLines.Add(salesOrderLine);

                            salesInvoiceLine.SalesOrderLine = salesOrderLine; // map invoice line to newly added orderline
                        }
                    }
                }

                if (!isNew)
                {
                    var deleted = (from line in salesInvoice.SalesInvoiceLines
                                   where !salesInvoiceDto.SalesInvoiceLines.Any(x => x.Id == line.Id)
                                   select line).ToList();

                    foreach (var line in deleted)
                    {
                        salesInvoice.SalesInvoiceLines.Remove(line);
                    }
                }

                _logger.LogInformation("SaveSalesInvoice API " + salesInvoice.CustomerId);

                _salesService.SaveSalesInvoice(salesInvoice, salesOrder);

                return new OkObjectResult(Ok());
            }
            catch (Exception ex)
            {
                errors = new string[1] { ex.InnerException != null ? ex.InnerException.Message : ex.Message };
                return new BadRequestObjectResult(errors);
            }
        }

        [HttpPost("CreateSalesInvoice")]
        [ServiceFilter(typeof(ValidationFilterAttribute))]
        public IActionResult CreateSalesInvoice([FromBody] Dto.Sales.SalesInvoice salesInvoiceDto)
        {
            var result = _salesService.CreateSalesInvoice(salesInvoiceDto);

            if (result.IsFailure)
                return BadRequest(result.Error.Message);

            var salesInvoiceToReturn = result.Value;

            return Ok(salesInvoiceToReturn);
        }

        [HttpPost("UpdateSalesInvoice")]
        [ServiceFilter(typeof(ValidationFilterAttribute))]
        public IActionResult UpdateSalesInvoice([FromBody] Dto.Sales.SalesInvoice salesInvoiceDto)
        {
            var result = _salesService.UpdateSalesInvoice(salesInvoiceDto);

            if (result.IsFailure)
                return BadRequest(result.Error.Message);

            var salesInvoiceToReturn = result.Value;

            return Ok(salesInvoiceToReturn);
        }

        [HttpDelete("DeleteSalesInvoice")]
        public async Task<IActionResult> DeleteSalesInvoice(int id)
        {
            var result = await _salesService.DeleteSalesInvoiceAsync(id);

            if (result.IsFailure)
                return BadRequest(result.Error.Message);

            return NoContent();
        }

        [HttpPost]
        [Route("SaveQuotation")]
        public IActionResult SaveQuotation([FromBody] Dto.Sales.SalesQuotation quotationDto)
        {
            string[]? errors = null;
            _logger.LogInformation("SaveQuotation");

            try
            {
                if (!ModelState.IsValid)
                {
                    errors = new string[ModelState.ErrorCount];
                    foreach (var val in ModelState.Values)
                        for (int i = 0; i < ModelState.ErrorCount; i++)
                            errors[i] = val.Errors[i].ErrorMessage;

                    return new BadRequestObjectResult(errors);
                }

                bool isNew = quotationDto.Id == 0;
                Core.Domain.Sales.SalesQuoteHeader? salesQuote = null;

                if (isNew)
                {
                    salesQuote = new Core.Domain.Sales.SalesQuoteHeader();
                    salesQuote.Status = SalesQuoteStatus.Draft;
                }
                else
                {
                    salesQuote = _salesService.GetSalesQuotationById(quotationDto.Id);
                    salesQuote.Status = (SalesQuoteStatus)quotationDto.StatusId;
                }

                salesQuote.CustomerId = quotationDto.CustomerId.GetValueOrDefault();
                salesQuote.Date = quotationDto.QuotationDate;

                salesQuote.ReferenceNo = quotationDto.ReferenceNo;
                salesQuote.PaymentTermId = quotationDto.PaymentTermId;

                foreach (var line in quotationDto.SalesQuotationLines)
                {
                    if (!isNew)
                    {
                        var existingLine = salesQuote.SalesQuoteLines.Where(id => id.Id == line.Id).FirstOrDefault();
                        if (salesQuote.SalesQuoteLines.Where(id => id.Id == line.Id).FirstOrDefault() != null)
                        {
                            existingLine!.Amount = line.Amount == null ? 0 : line.Amount.Value;
                            existingLine.Discount = line.Discount == null ? 0 : line.Discount.Value;
                            existingLine.Quantity = line.Quantity == null ? 0 : line.Quantity.Value;
                            existingLine.ItemId = line.ItemId.GetValueOrDefault();
                            existingLine.MeasurementId = line.MeasurementId.GetValueOrDefault();
                        }
                        else
                        {
                            var salesQuoteLine = new Core.Domain.Sales.SalesQuoteLine();
                            salesQuoteLine.Amount = line.Amount == null ? 0 : line.Amount.Value;
                            salesQuoteLine.Discount = line.Discount == null ? 0 : line.Discount.Value;
                            salesQuoteLine.Quantity = line.Quantity == null ? 0 : line.Quantity.Value;
                            salesQuoteLine.ItemId = line.ItemId.GetValueOrDefault();
                            salesQuoteLine.MeasurementId = line.MeasurementId.GetValueOrDefault();

                            salesQuote.SalesQuoteLines.Add(salesQuoteLine);
                        }
                    }
                    else
                    {
                        var salesQuoteLine = new Core.Domain.Sales.SalesQuoteLine();
                        salesQuoteLine.Amount = line.Amount == null ? 0 : line.Amount.Value;
                        salesQuoteLine.Discount = line.Discount == null ? 0 : line.Discount.Value;
                        salesQuoteLine.Quantity = line.Quantity == null ? 0 : line.Quantity.Value;
                        salesQuoteLine.ItemId = line.ItemId.GetValueOrDefault();
                        salesQuoteLine.MeasurementId = line.MeasurementId.GetValueOrDefault();

                        salesQuote.SalesQuoteLines.Add(salesQuoteLine);
                    }
                }

                if (isNew)
                {
                    _salesService.AddSalesQuote(salesQuote);
                }
                else
                {
                    var deleted = (from line in salesQuote.SalesQuoteLines
                                   where !quotationDto.SalesQuotationLines.Any(x => x.Id == line.Id)
                                   select line).ToList();

                    foreach (var line in deleted)
                    {
                        salesQuote.SalesQuoteLines.Remove(line);
                    }

                    _salesService.UpdateSalesQuote(salesQuote);
                }


                return new OkObjectResult(Ok());
            }
            catch (Exception ex)
            {
                errors = new string[1] { ex.InnerException != null ? ex.InnerException.Message : ex.Message };
                return new BadRequestObjectResult(errors);
            }
        }

        [HttpPost]
        [Route("SaveReceipt")]
        public IActionResult SaveReceipt([FromBody] dynamic receiptDto)
        {
            string[]? errors = null;

            try
            {
                if (!ModelState.IsValid)
                {
                    errors = new string[ModelState.ErrorCount];
                    foreach (var val in ModelState.Values)
                        for (int i = 0; i < ModelState.ErrorCount; i++)
                            errors[i] = val.Errors[i].ErrorMessage;

                    return new BadRequestObjectResult(errors);
                }

                var bank = _financialService.GetCashAndBanks().Where(id => id.Id == (int)receiptDto.AccountToDebitId).FirstOrDefault();

                var salesReceipt = new Core.Domain.Sales.SalesReceiptHeader();
                salesReceipt.Date = receiptDto.ReceiptDate;
                salesReceipt.CustomerId = receiptDto.CustomerId;
                salesReceipt.AccountToDebitId = bank!.AccountId;
                salesReceipt.Amount = receiptDto.Amount;
                salesReceipt.PaymentMethod = receiptDto.PaymentMethod != null ? (string)receiptDto.PaymentMethod : null;
                salesReceipt.ReferenceNo = receiptDto.ReferenceNo != null ? (string)receiptDto.ReferenceNo : null;
                salesReceipt.Memo = receiptDto.Memo != null ? (string)receiptDto.Memo : null;

                var customer = _salesService.GetCustomerById((int)receiptDto.CustomerId);
                if (customer.CustomerAdvancesAccountId != (int)receiptDto.AccountToCreditId)
                    throw new Exception("Invalid account.");

                // A payment started from an outstanding invoice is applied to that invoice.
                int? invoiceId = (int?)receiptDto.InvoiceId;
                Core.Domain.Sales.SalesInvoiceHeader? invoiceToPay = null;
                decimal invoiceBalance = 0;
                if (invoiceId is > 0)
                {
                    invoiceToPay = _salesService.GetSalesInvoiceById(invoiceId.Value);
                    if (invoiceToPay == null || invoiceToPay.CustomerId != (int)receiptDto.CustomerId)
                        return new BadRequestObjectResult(new[] { "The selected invoice does not belong to this customer." });

                    invoiceBalance = ToOutstandingInvoice(invoiceToPay).Balance;
                    if (invoiceBalance <= 0)
                        return new BadRequestObjectResult(new[] { "The selected invoice is already paid." });
                }

                var salesReceiptLine = new Core.Domain.Sales.SalesReceiptLine();
                salesReceiptLine.AccountToCreditId = receiptDto.AccountToCreditId;
                salesReceiptLine.AmountPaid = receiptDto.Amount;
                salesReceiptLine.Amount = receiptDto.Amount;
                salesReceiptLine.Quantity = 1;

                salesReceipt.SalesReceiptLines.Add(salesReceiptLine);

                _salesService.AddSalesReceiptNoInvoice(salesReceipt);

                if (invoiceToPay != null && salesReceipt.Id > 0)
                {
                    // Any amount above the invoice balance stays on the receipt for later allocation.
                    _salesService.SaveCustomerAllocation(new Core.Domain.Sales.CustomerAllocation
                    {
                        CustomerId = salesReceipt.CustomerId,
                        Date = salesReceipt.Date,
                        SalesInvoiceHeaderId = invoiceToPay.Id,
                        SalesReceiptHeaderId = salesReceipt.Id,
                        Amount = Math.Min(salesReceipt.Amount, invoiceBalance)
                    });
                }

                return new ObjectResult(Ok());
            }
            catch (Exception ex)
            {
                errors = new string[1] { ex.InnerException != null ? ex.InnerException.Message : ex.Message };
                return new BadRequestObjectResult(errors);
            }
        }

        [HttpPost]
        [Route("UpdateReceipt")]
        public IActionResult UpdateReceipt([FromBody] dynamic receiptDto)
        {
            string[]? errors = null;

            try
            {
                if (!ModelState.IsValid)
                {
                    errors = new string[ModelState.ErrorCount];
                    foreach (var val in ModelState.Values)
                        for (int i = 0; i < ModelState.ErrorCount; i++)
                            errors[i] = val.Errors[i].ErrorMessage;

                    return new BadRequestObjectResult(errors);
                }

                int receiptId = receiptDto.Id;
                var existingReceipt = _salesService.GetSalesReceiptById(receiptId);

                if (existingReceipt == null)
                {
                    return new BadRequestObjectResult(new string[] { "Sales receipt not found." });
                }

                var bank = _financialService.GetCashAndBanks().Where(id => id.Id == (int)receiptDto.AccountToDebitId).FirstOrDefault();

                existingReceipt.Date = receiptDto.ReceiptDate;
                existingReceipt.CustomerId = receiptDto.CustomerId;
                existingReceipt.AccountToDebitId = bank!.AccountId;
                existingReceipt.Amount = receiptDto.Amount;
                existingReceipt.PaymentMethod = receiptDto.PaymentMethod != null ? (string)receiptDto.PaymentMethod : null;
                existingReceipt.ReferenceNo = receiptDto.ReferenceNo != null ? (string)receiptDto.ReferenceNo : null;
                existingReceipt.Memo = receiptDto.Memo != null ? (string)receiptDto.Memo : null;

                var customer = _salesService.GetCustomerById((int)receiptDto.CustomerId);
                if (customer.CustomerAdvancesAccountId != (int)receiptDto.AccountToCreditId)
                    throw new Exception("Invalid account.");

                // Update the first receipt line
                var receiptLine = existingReceipt.SalesReceiptLines.FirstOrDefault();
                if (receiptLine != null)
                {
                    receiptLine.AccountToCreditId = receiptDto.AccountToCreditId;
                    receiptLine.AmountPaid = receiptDto.Amount;
                    receiptLine.Amount = receiptDto.Amount;
                }

                _salesService.UpdateSalesReceipt(existingReceipt);

                return new ObjectResult(Ok());
            }
            catch (Exception ex)
            {
                errors = new string[1] { ex.InnerException != null ? ex.InnerException.Message : ex.Message };
                return new BadRequestObjectResult(errors);
            }
        }

        [HttpPost]
        [Route("SaveAllocation")]
        public IActionResult SaveAllocation([FromBody] dynamic allocationDto)
        {
            string[]? errors = null;

            try
            {
                if (!ModelState.IsValid)
                {
                    errors = new string[ModelState.ErrorCount];
                    foreach (var val in ModelState.Values)
                        for (int i = 0; i < ModelState.ErrorCount; i++)
                            errors[i] = val.Errors[i].ErrorMessage;
                    return new BadRequestObjectResult(errors);
                }

                foreach (var line in allocationDto.AllocationLines)
                {
                    decimal? amount = (decimal?)line.AmountToAllocate;
                    if (amount.HasValue)
                    {
                        var allocation = new Core.Domain.Sales.CustomerAllocation();
                        allocation.CustomerId = allocationDto.CustomerId;
                        allocation.Date = allocationDto.Date;
                        allocation.SalesInvoiceHeaderId = line.InvoiceId;
                        allocation.SalesReceiptHeaderId = allocationDto.ReceiptId;
                        allocation.Amount = amount.GetValueOrDefault();

                        _salesService.SaveCustomerAllocation(allocation);
                    }
                }

                return new ObjectResult(Ok());
            }
            catch (Exception ex)
            {
                errors = new string[1] { ex.InnerException != null ? ex.InnerException.Message : ex.Message };
                return new BadRequestObjectResult(errors);
            }
        }

        [HttpGet]
        [Route("GetMonthlySales")] // api/Sales/GetMonthlySales
        public IActionResult GetMonthlySales()
        {
            var salesOrders = _salesService.GetSalesInvoices().Where(a => a.GeneralLedgerHeaderId != null);

            IList<Dto.Sales.MonthlySales> monthlySalesDto = new List<Dto.Sales.MonthlySales>();

            IList<Dto.Sales.MonthlySales> finalmonthlySalesDto = new List<Dto.Sales.MonthlySales>();

            foreach (var item in salesOrders)
            {
                foreach (var line in item.SalesInvoiceLines)
                {
                    var dtoSales = new MonthlySales();
                    dtoSales.Month = item.Date.Month.ToString();
                    dtoSales.Amount = line.Amount * line.Quantity;
                    monthlySalesDto.Add(dtoSales);
                }
            }

            var totalSales = monthlySalesDto.ToList().GroupBy(a => a.Month)
            .Select(ms => new MonthlySales
            {
                Month = ms.First().Month,
                Amount = ms.Sum(x => x.Amount),
            }).ToList();

            for (int i = 1; i <= DateTime.Now.Month; i++)
            {
                var sales = new MonthlySales();
                var month = i + "/1/" + DateTime.Now.Year;
                sales.Month = Convert.ToDateTime(month).ToString("MMMMM");
                sales.Amount = totalSales.Where(a => a.Month == i.ToString()).Select(x => x.Amount).FirstOrDefault();
                finalmonthlySalesDto.Add(sales);
            }

            return Json(finalmonthlySalesDto);
        }

        [HttpGet]
        [Route("SalesInvoiceForPrinting")]
        public IActionResult SalesInvoiceForPrinting(int id)
        {
            try
            {
                var salesInvoice = _salesService.GetSalesInvoiceById(id);

                //var items = _salesService.Ge
                var salesInvoiceDto = new Dto.Sales.SalesInvoice()
                {
                    Id = salesInvoice.Id,
                    CustomerId = salesInvoice.CustomerId,
                    CustomerName = salesInvoice.Customer.Party.Name,
                    CustomerEmail = salesInvoice.Customer.Party.Email,
                    InvoiceDate = salesInvoice.Date,
                    SalesInvoiceLines = new List<Dto.Sales.SalesInvoiceLine>(),
                    PaymentTermId = salesInvoice.PaymentTermId,
                    ReferenceNo = salesInvoice.ReferenceNo,
                    Posted = salesInvoice.GeneralLedgerHeaderId != null,
                    CompanyName = _adminService.GetDefaultCompany().Name

                };

                foreach (var line in salesInvoice.SalesInvoiceLines)
                {
                    var lineDto = new Dto.Sales.SalesInvoiceLine();

                    lineDto.Id = line.Id;
                    lineDto.Amount = line.Amount;
                    lineDto.Discount = line.Discount;
                    lineDto.Quantity = line.Quantity;
                    lineDto.ItemId = line.ItemId;
                    lineDto.MeasurementId = line.MeasurementId;

                    lineDto.ItemDescription = _inventoryService.GetItemById(line.ItemId).Description;
                    lineDto.MeasurementDescription = _inventoryService.GetMeasurementById(line.MeasurementId).Description;

                    salesInvoiceDto.SalesInvoiceLines.Add(lineDto);
                }
                salesInvoiceDto.Shipping = salesInvoice.ShippingHandlingCharge;
                salesInvoiceDto.TotalTax = _financialService.ComputeSalesInvoiceTaxes(salesInvoice).Sum(t => t.Value);
                salesInvoiceDto.TotalAmountAfterTax = salesInvoiceDto.TotalInvoice;


                return new ObjectResult(salesInvoiceDto);
            }
            catch (Exception ex)
            {
                return new ObjectResult(ex);
            }
        }

        [HttpPost]
        [Route("BookQuotation")]
        public IActionResult BookQuotation(int id)
        {
            try
            {
                _salesService.BookQuotation(id);
                return new ObjectResult(Ok());
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }
    }
}