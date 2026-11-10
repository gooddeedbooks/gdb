using System.Linq.Expressions;
using AutoMapper;
using Core.Data;
using Core.Domain;
using Core.Domain.Financials;
using Core.Domain.Items;
using Core.Domain.Sales;
using Core.Domain.TaxSystem;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Services.Financial;
using Services.Inventory;
using Services.Sales;
using Services.TaxSystem;

namespace GoodBooks.ServicesTests
{
    public class SalesInvoiceTaxTests
    {
        private readonly Mock<ITaxService> taxes = new Mock<ITaxService>();

        [Theory]
        [InlineData(350, 15, 0, 51.10, 416.10)]
        [InlineData(350, 0, 0, 49, 399)]
        [InlineData(350, 15, 10, 46.20, 376.20)]
        [InlineData(0, 15, 0, 2.10, 17.10)]
        public void ComputeSalesInvoiceTaxes_ExclusivePrices_TaxesDiscountedItemsAndShipping(
            decimal amount, decimal shipping, decimal discount, decimal expectedTax, decimal expectedTotal)
        {
            taxes.Setup(s => s.GetCustomerTaxes(9)).Returns(new[] { new Tax { Id = 1, Rate = 14 } });
            var invoice = Invoice(amount, shipping, discount);

            var tax = Financial().ComputeSalesInvoiceTaxes(invoice).Sum(t => t.Value);

            Assert.Equal(expectedTax, tax);
            var dto = new global::Dto.Sales.SalesInvoice
            {
                Shipping = shipping, TotalTax = tax,
                SalesInvoiceLines = new List<global::Dto.Sales.SalesInvoiceLine>
                {
                    new() { Quantity = 1, Amount = amount, Discount = discount }
                }
            };
            Assert.Equal(expectedTotal, dto.TotalInvoice);
        }

        [Fact]
        public void ComputeSalesInvoiceTaxes_Quebec_AddsGstAndQstWithoutCompounding()
        {
            taxes.Setup(s => s.GetCustomerTaxes(9)).Returns(new[]
            {
                new Tax { Id = 1, Rate = 5 }, new Tax { Id = 2, Rate = 9.975m }
            });

            var result = Financial().ComputeSalesInvoiceTaxes(Invoice(100, 15, 0));

            Assert.Equal(5.75m, result.Single(t => t.Key == 1).Value);
            Assert.Equal(11.47m, result.Single(t => t.Key == 2).Value);
        }

        [Theory]
        [InlineData(false, 0)]
        [InlineData(false, 10)]
        [InlineData(true, 0)]
        [InlineData(true, 10)]
        public void Posting_ExclusiveTaxAndShipping_BalancesReceivableRevenueAndTax(bool directAdd, decimal discount)
        {
            taxes.Setup(s => s.GetCustomerTaxes(9)).Returns(new[] { new Tax { Id = 1, Rate = 14, SalesAccountId = 30 } });
            var invoice = Invoice(350, 15, discount);
            var ledger = new GeneralLedgerHeader();
            var financial = new Mock<IFinancialService>();
            financial.Setup(s => s.ComputeSalesInvoiceTaxes(invoice)).Returns(() => Financial().ComputeSalesInvoiceTaxes(invoice));
            financial.Setup(s => s.GetTaxes()).Returns(taxes.Object.GetCustomerTaxes(9));
            financial.Setup(s => s.CreateGeneralLedgerHeader(It.IsAny<DocumentTypes>(), It.IsAny<DateTime>(), It.IsAny<string>()))
                .Returns(ledger);
            financial.Setup(s => s.CreateGeneralLedgerLine(It.IsAny<DrOrCrSide>(), It.IsAny<int>(), It.IsAny<decimal>()))
                .Returns((DrOrCrSide side, int account, decimal amount) =>
                    new GeneralLedgerLine { DrCr = side, AccountId = account, Amount = amount });
            financial.Setup(s => s.ValidateGeneralLedgerEntry(ledger)).Returns(() =>
                ledger.GeneralLedgerLines.Where(l => l.DrCr == DrOrCrSide.Dr).Sum(l => l.Amount) ==
                ledger.GeneralLedgerLines.Where(l => l.DrCr == DrOrCrSide.Cr).Sum(l => l.Amount));

            var item = new Item
            {
                Id = 1, SalesAccountId = 20, SalesAccount = new Account { Id = 20 },
                InventoryAccount = new Account(), InventoryAdjustmentAccount = new Account(),
                CostOfGoodsSoldAccount = new Account(), ItemCategory = new ItemCategory { ItemType = ItemTypes.Charge }
            };
            var inventory = new Mock<IInventoryService>();
            inventory.Setup(s => s.GetItemById(1)).Returns(item);
            var invoices = new Mock<IRepository<SalesInvoiceHeader>>();
            invoices.Setup(r => r.GetAllIncluding(It.IsAny<Expression<Func<SalesInvoiceHeader, object>>[]>()))
                .Returns(new[] { invoice }.AsQueryable());
            var customers = new Mock<IRepository<Customer>>();
            customers.Setup(r => r.GetAllIncluding(It.IsAny<Expression<Func<Customer, object>>[]>()))
                .Returns(new[] { new Customer { Id = 9, AccountsReceivableAccount = new Account { Id = 10 },
                    CustomerContact = new List<CustomerContact>() } }.AsQueryable());
            var settings = new Mock<IRepository<GeneralLedgerSetting>>();
            settings.Setup(r => r.Table).Returns(new[]
            {
                new GeneralLedgerSetting { ShippingChargeAccount = new Account { Id = 40 } }
            }.AsQueryable());
            var sequences = new Mock<IRepository<SequenceNumber>>();
            sequences.Setup(r => r.Table).Returns(Array.Empty<SequenceNumber>().AsQueryable());
            var items = new Mock<IRepository<Item>>();
            items.Setup(r => r.GetById(1)).Returns(item);
            var service = new SalesService(financial.Object, inventory.Object,
                Mock.Of<IRepository<SalesOrderHeader>>(), Mock.Of<IRepository<SalesOrderLine>>(),
                invoices.Object, Mock.Of<IRepository<SalesReceiptHeader>>(), customers.Object,
                Mock.Of<IRepository<Account>>(), items.Object, Mock.Of<IRepository<Measurement>>(),
                sequences.Object, Mock.Of<IRepository<PaymentTerm>>(), Mock.Of<IRepository<SalesDeliveryHeader>>(),
                Mock.Of<IRepository<Bank>>(), settings.Object, Mock.Of<IRepository<Contact>>(),
                Mock.Of<IRepository<TaxGroup>>(), Mock.Of<IRepository<SalesQuoteHeader>>(),
                Mock.Of<ISalesOrderRepository>(), Mock.Of<IRepository<CustomerContact>>(),
                Mock.Of<IRepository<VendorContact>>(), NullLogger<SalesService>.Instance, Mock.Of<IMapper>());

            if (directAdd)
                service.AddSalesInvoice(invoice, 1, null);
            else
                service.PostSalesInvoice(1);

            Assert.Same(ledger, invoice.GeneralLedgerHeader);
            Assert.Equal(discount == 0 ? 416.10m : 376.20m,
                Assert.Single(ledger.GeneralLedgerLines, l => l.AccountId == 10).Amount);
            Assert.Equal(discount == 0 ? 51.10m : 46.20m,
                Assert.Single(ledger.GeneralLedgerLines, l => l.AccountId == 30).Amount);
            Assert.Equal(15, Assert.Single(ledger.GeneralLedgerLines, l => l.AccountId == 40).Amount);
            Assert.Equal(350 * (1 - discount / 100),
                Assert.Single(ledger.GeneralLedgerLines, l => l.AccountId == 20).Amount);
        }

        private static SalesInvoiceHeader Invoice(decimal amount, decimal shipping, decimal discount)
        {
            var invoice = new SalesInvoiceHeader { Id = 1, CustomerId = 9, ShippingHandlingCharge = shipping };
            invoice.SalesInvoiceLines.Add(new SalesInvoiceLine { ItemId = 1, Quantity = 1, Amount = amount, Discount = discount });
            return invoice;
        }

        private FinancialService Financial()
        {
            return new FinancialService(Mock.Of<IInventoryService>(), taxes.Object,
                Mock.Of<IRepository<GeneralLedgerHeader>>(), Mock.Of<IRepository<GeneralLedgerLine>>(),
                Mock.Of<IRepository<Account>>(), Mock.Of<IRepository<AccountClass>>(),
                Mock.Of<IRepository<Tax>>(), Mock.Of<IRepository<JournalEntryHeader>>(),
                Mock.Of<IRepository<JournalEntryLine>>(), Mock.Of<IRepository<FinancialYear>>(),
                Mock.Of<IRepository<TaxGroup>>(), Mock.Of<IRepository<ItemTaxGroup>>(),
                Mock.Of<IRepository<PaymentTerm>>(), Mock.Of<IRepository<Bank>>(),
                Mock.Of<IRepository<Item>>(), Mock.Of<IRepository<GeneralLedgerSetting>>());
        }
    }
}
