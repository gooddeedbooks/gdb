using Api.Controllers;
using Core.Domain;
using Core.Domain.Sales;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Services.Administration;
using Services.Financial;
using Services.Inventory;
using Services.Sales;
using Services.TaxSystem;
using InvoiceDto = global::Dto.Sales.SalesInvoice;
using InvoiceLineDto = global::Dto.Sales.SalesInvoiceLine;
using OutstandingInvoiceDto = global::Dto.Sales.OutstandingInvoice;

namespace GoodBooks.ServicesTests
{
    public class OutstandingInvoicesTests
    {
        private readonly Mock<ISalesService> sales = new Mock<ISalesService>();
        private readonly Mock<IFinancialService> financial = new Mock<IFinancialService>();

        [Fact]
        public void OutstandingInvoices_MixedBalances_ReturnsOnlyUnpaidInvoicesWithTotals()
        {
            var partlyPaid = Invoice(1, "10", 9, "Northwind Books", 350, shipping: 15, paid: 100);
            var paid = Invoice(2, "11", 9, "Northwind Books", 100, shipping: 0, paid: 114);
            var unpaid = Invoice(3, "12", 8, "ABC Customer", 50, shipping: 0, paid: 0);
            unpaid.Date = new DateTime(2026, 1, 1);
            sales.Setup(service => service.GetSalesInvoices()).Returns(new[] { partlyPaid, paid, unpaid });
            financial.Setup(service => service.ComputeSalesInvoiceTaxes(It.IsAny<SalesInvoiceHeader>()))
                .Returns((SalesInvoiceHeader invoice) => new List<KeyValuePair<int, decimal>>
                {
                    new(1, Math.Round((invoice.SalesInvoiceLines.Sum(line => line.Amount * line.Quantity) +
                        invoice.ShippingHandlingCharge) * 0.14m, 2))
                });

            var result = Assert.IsType<OkObjectResult>(Controller().OutstandingInvoices());

            var invoices = Assert.IsAssignableFrom<IList<OutstandingInvoiceDto>>(result.Value);
            Assert.Equal(new[] { 3, 1 }, invoices.Select(invoice => invoice.Id));
            var first = invoices.Single(invoice => invoice.Id == 1);
            Assert.Equal(350m, first.Subtotal);
            Assert.Equal(51.10m, first.Tax);
            Assert.Equal(416.10m, first.Total);
            Assert.Equal(316.10m, first.Balance);
            Assert.Equal("Northwind Books", first.CustomerName);
        }

        [Fact]
        public void SaveSalesInvoice_ExistingInvoiceWithNewLine_AddsLineToLinkedOrder()
        {
            var order = new SalesOrderHeader { Id = 42, CustomerId = 9 };
            var orderLine = new SalesOrderLine { Id = 7, SalesOrderHeader = order };
            order.SalesOrderLines.Add(orderLine);
            var invoice = Invoice(5, "20", 9, "Northwind Books", 25, shipping: 0, paid: 0);
            invoice.SalesInvoiceLines.First().Id = 70;
            invoice.SalesInvoiceLines.First().SalesOrderLineId = 7;
            sales.Setup(service => service.GetSalesInvoiceById(5)).Returns(invoice);
            sales.Setup(service => service.GetSalesOrderLineById(7)).Returns(orderLine);
            sales.Setup(service => service.GetSalesInvoices()).Returns(new[] { invoice });

            var result = Controller().SaveSalesInvoice(EditInput(5));

            Assert.IsType<OkObjectResult>(result);
            Assert.Equal(2, invoice.SalesInvoiceLines.Count);
            Assert.Equal(12.5m, invoice.ShippingHandlingCharge);
            Assert.Contains(order.SalesOrderLines, line => line.Id == 0 && line.ItemId == 3 && line.Quantity == 2);
            sales.Verify(service => service.SaveSalesInvoice(invoice, order), Times.Once);
        }

        [Fact]
        public void SaveSalesInvoice_PostedInvoice_RejectsEdit()
        {
            var invoice = Invoice(5, "20", 9, "Northwind Books", 25, shipping: 0, paid: 0);
            invoice.GeneralLedgerHeaderId = 1;
            sales.Setup(service => service.GetSalesInvoiceById(5)).Returns(invoice);
            sales.Setup(service => service.GetSalesInvoices()).Returns(new[] { invoice });

            Assert.IsType<BadRequestObjectResult>(Controller().SaveSalesInvoice(EditInput(5)));
            sales.Verify(service => service.SaveSalesInvoice(It.IsAny<SalesInvoiceHeader>(),
                It.IsAny<SalesOrderHeader>()), Times.Never);
        }

        [Fact]
        public void SalesInvoice_MissingInvoice_ReturnsNotFound()
        {
            Assert.IsType<NotFoundObjectResult>(Controller().SalesInvoice(404));
        }

        private static InvoiceDto EditInput(int id)
        {
            return new InvoiceDto
            {
                Id = id, No = "20", CustomerId = 9, PaymentTermId = 3, Shipping = 12.5m,
                SalesInvoiceLines = new List<InvoiceLineDto>
                {
                    new InvoiceLineDto { Id = 70, ItemId = 1, MeasurementId = 2, Quantity = 1, Amount = 25, Discount = 0 },
                    new InvoiceLineDto { Id = 0, ItemId = 3, MeasurementId = 2, Quantity = 2, Amount = 10, Discount = 0 }
                }
            };
        }

        private static SalesInvoiceHeader Invoice(int id, string no, int customerId, string customerName,
            decimal amount, decimal shipping, decimal paid)
        {
            var invoice = new SalesInvoiceHeader
            {
                Id = id, No = no, CustomerId = customerId, Date = new DateTime(2026, 2, id),
                ShippingHandlingCharge = shipping,
                Customer = new Customer { Party = new Party { Name = customerName } }
            };
            invoice.SalesInvoiceLines.Add(new SalesInvoiceLine { ItemId = 1, MeasurementId = 2, Quantity = 1, Amount = amount });
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
