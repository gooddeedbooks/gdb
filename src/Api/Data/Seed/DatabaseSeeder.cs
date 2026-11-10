using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Services.Administration;
using Services.Financial;
using Services.Inventory;
using Services.Purchasing;
using Services.Sales;
using Services.Security;

namespace Api.Data.Seed
{
    public class DatabaseSeeder
    {
        private readonly IServiceProvider _serviceProvider;

        public DatabaseSeeder(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void Seed()
        {
            using var scope = _serviceProvider.CreateScope();
            var adminService = scope.ServiceProvider.GetRequiredService<IAdministrationService>();
            var financialService = scope.ServiceProvider.GetRequiredService<IFinancialService>();
            var salesService = scope.ServiceProvider.GetRequiredService<ISalesService>();
            var purchasingService = scope.ServiceProvider.GetRequiredService<IPurchasingService>();
            var inventoryService = scope.ServiceProvider.GetRequiredService<IInventoryService>();
            var securityService = scope.ServiceProvider.GetRequiredService<ISecurityService>();

            var initializer = new Initializer(
                adminService,
                financialService,
                salesService,
                purchasingService,
                inventoryService,
                securityService,
                scope.ServiceProvider.GetRequiredService<ApiDbContext>()
            );

            var success = initializer.Setup();

            if (success)
            {
                Console.WriteLine("Database seeding completed successfully.");
            }
            else
            {
                Console.WriteLine("Database seeding failed. Check logs for details.");
            }

            var apiContext = scope.ServiceProvider.GetRequiredService<ApiDbContext>();
            SeedDonations(apiContext);
            SeedDonationTypes(apiContext);
            SeedSalesOrders(apiContext);
        }

        private static void SeedSalesOrders(ApiDbContext context)
        {
            var customers = context.Customers
                .Where(customer => customer.Party.IsActive)
                .OrderBy(customer => customer.Id)
                .ToList();
            var items = context.Items
                .Where(item => item.Price.HasValue && item.Price.Value > 0)
                .OrderBy(item => item.Id)
                .ToList();
            var paymentTermId = context.PaymentTerms
                .Where(term => term.IsActive)
                .OrderBy(term => term.Id)
                .Select(term => (int?)term.Id)
                .FirstOrDefault();
            var defaultMeasurementId = context.Measurements
                .OrderBy(measurement => measurement.Id)
                .Select(measurement => (int?)measurement.Id)
                .FirstOrDefault();

            if (customers.Count == 0 || items.Count == 0 || !defaultMeasurementId.HasValue)
            {
                Console.WriteLine("Skipped sample sales orders: an active customer, priced item, or unit of measure is missing.");
                return;
            }

            for (var index = 0; index < 5; index++)
            {
                var orderNumber = $"SO-SEED-{index + 1:D3}";
                if (context.SalesOrderHeaders.Any(order => order.No == orderNumber))
                    continue;

                var item = items[index % items.Count];
                var measurementId = item.SellMeasurementId ?? item.SmallestMeasurementId ?? defaultMeasurementId.Value;
                context.SalesOrderHeaders.Add(new Core.Domain.Sales.SalesOrderHeader
                {
                    No = orderNumber,
                    ReferenceNo = $"SAMPLE-{index + 1:D3}",
                    CustomerId = customers[index % customers.Count].Id,
                    PaymentTermId = paymentTermId,
                    Date = DateTime.Today.AddDays(-index * 3),
                    Status = Core.Domain.SalesOrderStatus.Open,
                    SalesOrderLines =
                    {
                        new Core.Domain.Sales.SalesOrderLine
                        {
                            ItemId = item.Id,
                            MeasurementId = measurementId,
                            Quantity = index + 1,
                            Amount = item.Price.GetValueOrDefault(),
                            Discount = index % 2 == 0 ? 0 : 5
                        }
                    }
                });
            }

            context.SaveChanges();
        }

        private static void SeedDonationTypes(ApiDbContext context)
        {
            var samples = new[]
            {
                new { Name = "Project reach", Description = "To build low housing cost", AccountCode = "41001" },
                new { Name = "Youth field trip", Description = "Trip to Kelowna", AccountCode = "41200" }
            };

            foreach (var sample in samples)
            {
                if (context.DonationTypes.Any(type => type.Name == sample.Name))
                    continue;

                var account = context.Accounts.FirstOrDefault(entity => entity.CompanyId == 1 && entity.AccountCode == sample.AccountCode);
                if (account == null)
                {
                    Console.WriteLine($"Skipped sample donation type {sample.Name}: GL account {sample.AccountCode} is missing.");
                    continue;
                }

                context.DonationTypes.Add(new Core.Domain.Donations.DonationType
                {
                    Name = sample.Name,
                    Description = sample.Description,
                    GlAccountId = account.Id
                });
            }

            context.SaveChanges();
        }

        private static void SeedDonations(ApiDbContext context)
        {
            var samples = new[]
            {
                new Dto.Donations.DonationInvoice
                {
                    No = "DON-SEED-001", DonorId = 1, GlAccountCode = "41001",
                    DonationDate = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
                    PaymentType = "Cash", Amount = 150.00m, Purpose = "General-purpose support",
                    IsTaxReceiptIssued = true, TaxReceiptNo = "TR-SEED-001"
                },
                new Dto.Donations.DonationInvoice
                {
                    No = "DON-SEED-002", DonorId = 1, GlAccountCode = "41200",
                    DonationDate = new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc),
                    PaymentType = "Credit Card", Amount = 75.50m, Purpose = "Sunday School materials",
                    IsTaxReceiptIssued = false, TaxReceiptNo = string.Empty
                },
                new Dto.Donations.DonationInvoice
                {
                    No = "DON-SEED-003", DonorId = 2, GlAccountCode = "41250",
                    DonationDate = new DateTime(2026, 10, 3, 0, 0, 0, DateTimeKind.Utc),
                    PaymentType = "E-Transfer", Amount = 5000.00m, Purpose = "Brothers In Christ outreach",
                    IsTaxReceiptIssued = true, TaxReceiptNo = "TR-SEED-003"
                },
                new Dto.Donations.DonationInvoice
                {
                    No = "DON-SEED-004", DonorId = 2, GlAccountCode = "41001",
                    DonationDate = new DateTime(2026, 10, 4, 0, 0, 0, DateTimeKind.Utc),
                    PaymentType = "Cheque", Amount = 1200.00m, Purpose = "Community care fund",
                    IsTaxReceiptIssued = false, TaxReceiptNo = string.Empty
                }
            };

            foreach (var sample in samples)
            {
                if (context.DonationInvoiceHeaders.Any(donation => donation.No == sample.No))
                    continue;

                var donor = context.Donors.FirstOrDefault(entity => entity.Id == sample.DonorId);
                var account = context.Accounts.FirstOrDefault(entity => entity.CompanyId == 1 && entity.AccountCode == sample.GlAccountCode);
                if (donor == null || account == null)
                {
                    Console.WriteLine($"Skipped sample donation {sample.No}: its donor or GL account is missing.");
                    continue;
                }

                context.DonationInvoiceHeaders.Add(new Core.Domain.Donations.DonationInvoiceHeader
                {
                    No = sample.No,
                    DonorId = donor.Id,
                    GlAccountId = account.Id,
                    Date = sample.DonationDate,
                    PaymentType = sample.PaymentType,
                    Amount = sample.Amount,
                    Purpose = sample.Purpose,
                    ReferenceNo = string.Empty,
                    IsTaxReceiptIssued = sample.IsTaxReceiptIssued,
                    TaxReceiptNo = sample.TaxReceiptNo,
                    Posted = false,
                    ModifiedBy = "seed"
                });
            }

            context.SaveChanges();
        }
    }
}