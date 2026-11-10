using Api.Controllers;
using Core.Domain;
using Core.Domain.Financials;
using Core.Domain.Sales;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Newtonsoft.Json.Linq;
using Services.Administration;
using Services.Financial;
using Services.Inventory;
using Services.Sales;
using Services.TaxSystem;

namespace GoodBooks.ServicesTests
{
    public class CustomerPaymentInvoiceTests
    {
        private const int CustomerId = 9;
        private const int AdvancesAccountId = 55;
        private readonly Mock<ISalesService> sales = new Mock<ISalesService>();
        private readonly Mock<IFinancialService> financial = new Mock<IFinancialService>();

        public CustomerPaymentInvoiceTests()
        {
            financial.Setup(service => service.GetCashAndBanks())
                .Returns(new[] { new Bank { Id = 1, AccountId = 100 } });
            financial.Setup(service => service.ComputeSalesInvoiceTaxes(It.IsAny<SalesInvoiceHeader>()))
                .Returns(new List<KeyValuePair<int, decimal>> { new(1, 14m) });
            sales.Setup(service => service.GetCustomerById(CustomerId))
                .Returns(new Customer { Id = CustomerId, CustomerAdvancesAccountId = AdvancesAccountId });
            sales.Setup(service => service.AddSalesReceiptNoInvoice(It.IsAny<SalesReceiptHeader>()))
                .Callback((SalesReceiptHeader receipt) => receipt.Id = 77);
        }

        [Fact]
        public void SaveReceipt_WithInvoice_AllocatesUpToInvoiceBalance()
        {
            // Invoice total 100 + 14 tax = 114, already paid 14, so balance is 100.
            sales.Setup(service => service.GetSalesInvoiceById(5)).Returns(Invoice(5, CustomerId, paid: 14));
            CustomerAllocation? saved = null;
            sales.Setup(service => service.SaveCustomerAllocation(It.IsAny<CustomerAllocation>()))
                .Callback((CustomerAllocation allocation) => saved = allocation);

            var result = Controller().SaveReceipt(Receipt(amount: 150, invoiceId: 5));

            Assert.IsType<ObjectResult>(result);
            Assert.NotNull(saved);
            Assert.Equal(5, saved!.SalesInvoiceHeaderId);
            Assert.Equal(77, saved.SalesReceiptHeaderId);
            Assert.Equal(100m, saved.Amount);
        }

        [Fact]
        public void SaveReceipt_WithoutInvoice_DoesNotAllocate()
        {
            var result = Controller().SaveReceipt(Receipt(amount: 50, invoiceId: null));

            Assert.IsType<ObjectResult>(result);
            sales.Verify(service => service.AddSalesReceiptNoInvoice(It.IsAny<SalesReceiptHeader>()), Times.Once);
            sales.Verify(service => service.SaveCustomerAllocation(It.IsAny<CustomerAllocation>()), Times.Never);
        }

        [Fact]
        public void SaveReceipt_InvoiceOfAnotherCustomer_IsRejectedWithoutSaving()
        {
            sales.Setup(service => service.GetSalesInvoiceById(5)).Returns(Invoice(5, customerId: 8, paid: 0));

            var result = Controller().SaveReceipt(Receipt(amount: 50, invoiceId: 5));

            Assert.IsType<BadRequestObjectResult>(result);
            sales.Verify(service => service.AddSalesReceiptNoInvoice(It.IsAny<SalesReceiptHeader>()), Times.Never);
        }

        [Fact]
        public void SaveReceipt_PaidInvoice_IsRejectedWithoutSaving()
        {
            sales.Setup(service => service.GetSalesInvoiceById(5)).Returns(Invoice(5, CustomerId, paid: 114));

            var result = Controller().SaveReceipt(Receipt(amount: 50, invoiceId: 5));

            Assert.IsType<BadRequestObjectResult>(result);
            sales.Verify(service => service.AddSalesReceiptNoInvoice(It.IsAny<SalesReceiptHeader>()), Times.Never);
        }

        private static JObject Receipt(decimal amount, int? invoiceId)
        {
            return JObject.FromObject(new
            {
                CustomerId,
                ReceiptDate = new DateTime(2026, 11, 9),
                AccountToDebitId = 1,
                AccountToCreditId = AdvancesAccountId,
                Amount = amount,
                PaymentMethod = "Cheque",
                ReferenceNo = "123",
                Memo = (string?)null,
                InvoiceId = invoiceId
            });
        }

        private static SalesInvoiceHeader Invoice(int id, int customerId, decimal paid)
        {
            var invoice = new SalesInvoiceHeader
            {
                Id = id, No = id.ToString(), CustomerId = customerId, Date = new DateTime(2026, 11, 1),
                Customer = new Customer { Party = new Party { Name = "Northwind Books" } }
            };
            invoice.SalesInvoiceLines.Add(new SalesInvoiceLine { ItemId = 1, MeasurementId = 2, Quantity = 1, Amount = 100 });
            if (paid > 0)
                invoice.CustomerAllocations.Add(new CustomerAllocation { Amount = paid });
            return invoice;
        }

        private SalesController Controller()
        {
            return new SalesController(Mock.Of<IAdministrationService>(), sales.Object,
                financial.Object, Mock.Of<IInventoryService>(), Mock.Of<ITaxService>(),
                NullLogger<SalesController>.Instance);
        }
    }
}
