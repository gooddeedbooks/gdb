using Api.Controllers;
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

namespace GoodBooks.ServicesTests
{
    public class SalesOrderInvoicingTests
    {
        private readonly Mock<ISalesService> sales = new Mock<ISalesService>();

        [Fact]
        public void SaveSalesInvoice_FromOrder_PreservesSourceLinkAndInvoiceValues()
        {
            var order = SourceOrder();
            var input = Invoice(3);

            var result = Controller().SaveSalesInvoice(input);

            Assert.IsType<OkObjectResult>(result);
            sales.Verify(service => service.SaveSalesInvoice(
                It.Is<SalesInvoiceHeader>(invoice => invoice.Id == 0 && invoice.CustomerId == 9 &&
                    invoice.PaymentTermId == 3 && invoice.SalesInvoiceLines.Count == 1 &&
                    invoice.SalesInvoiceLines.First().SalesOrderLineId == 7 &&
                    invoice.SalesInvoiceLines.First().Quantity == 3 &&
                    invoice.SalesInvoiceLines.First().Amount == 25 &&
                    invoice.SalesInvoiceLines.First().Discount == 10), order), Times.Once);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(4)]
        public void SaveSalesInvoice_InvalidRemainingQuantity_RejectsInvoice(decimal quantity)
        {
            SourceOrder();

            Assert.IsType<BadRequestObjectResult>(Controller().SaveSalesInvoice(Invoice(quantity)));
            sales.Verify(service => service.SaveSalesInvoice(It.IsAny<SalesInvoiceHeader>(),
                It.IsAny<SalesOrderHeader>()), Times.Never);
        }

        [Fact]
        public void SaveSalesInvoice_MissingSourceOrder_ReturnsNotFound()
        {
            Assert.IsType<NotFoundObjectResult>(Controller().SaveSalesInvoice(Invoice(1)));
        }

        [Fact]
        public void SaveSalesInvoice_ForeignOrderLine_RejectsInvoice()
        {
            SourceOrder();
            var input = Invoice(1);
            input.SalesInvoiceLines![0].Id = 99;

            Assert.IsType<BadRequestObjectResult>(Controller().SaveSalesInvoice(input));
        }

        [Fact]
        public void NextInvoiceNumber_EmptyLedger_StartsAtOne()
        {
            sales.Setup(service => service.GetSalesInvoices()).Returns(Array.Empty<SalesInvoiceHeader>());

            var result = Assert.IsType<OkObjectResult>(Controller().NextInvoiceNumber());

            Assert.Equal(1L, result.Value);
        }

        [Fact]
        public void NextInvoiceNumber_ExistingNumbers_UsesNextNumericValue()
        {
            sales.Setup(service => service.GetSalesInvoices()).Returns(new[]
            {
                new SalesInvoiceHeader { No = "1" },
                new SalesInvoiceHeader { No = "20" },
                new SalesInvoiceHeader { No = "legacy" }
            });

            var result = Assert.IsType<OkObjectResult>(Controller().NextInvoiceNumber());

            Assert.Equal(21L, result.Value);
        }

        [Fact]
        public void SaveSalesInvoice_OverriddenNumber_PreservesNumber()
        {
            var order = SourceOrder();
            sales.Setup(service => service.GetSalesInvoices()).Returns(Array.Empty<SalesInvoiceHeader>());
            var input = Invoice(1);
            input.No = "25";

            Assert.IsType<OkObjectResult>(Controller().SaveSalesInvoice(input));

            sales.Verify(service => service.SaveSalesInvoice(
                It.Is<SalesInvoiceHeader>(invoice => invoice.No == "25" && invoice.Id == 0), order), Times.Once);
        }

        [Fact]
        public void SaveSalesInvoice_Shipping_PersistsShippingCharge()
        {
            var order = SourceOrder();
            var input = Invoice(1);
            input.Shipping = 12.5m;

            Assert.IsType<OkObjectResult>(Controller().SaveSalesInvoice(input));
            Assert.Equal(35m, input.TotalInvoice);
            sales.Verify(service => service.SaveSalesInvoice(
                It.Is<SalesInvoiceHeader>(invoice => invoice.ShippingHandlingCharge == 12.5m), order), Times.Once);
        }

        [Fact]
        public void CalculateInvoiceTax_CustomerTaxProfile_SumsIncludedTax()
        {
            var financial = new Mock<IFinancialService>();
            financial.Setup(service => service.ComputeOutputTax(9, 1, 10, 110, 0))
                .Returns(new List<KeyValuePair<int, decimal>> { new(1, 100m), new(2, 0.555m) });
            var taxes = new Mock<ITaxService>();
            taxes.Setup(service => service.GetCustomerTaxes(9)).Returns(new[]
            {
                new Core.Domain.TaxSystem.Tax { Rate = 10 },
                new Core.Domain.TaxSystem.Tax { Rate = 0.0555m }
            });
            var controller = new SalesController(Mock.Of<IAdministrationService>(), sales.Object,
                financial.Object, Mock.Of<IInventoryService>(), taxes.Object,
                NullLogger<SalesController>.Instance);
            var request = new global::Dto.Sales.InvoiceTaxRequest
            {
                CustomerId = 9,
                Lines = new List<global::Dto.Sales.InvoiceTaxLine>
                {
                    new() { ItemId = 1, Quantity = 10, Amount = 110 },
                    new() { ItemId = 0, Quantity = 1, Amount = 5 }
                }
            };

            var result = Assert.IsType<OkObjectResult>(controller.CalculateInvoiceTax(request));

            var tax = Assert.IsType<global::Dto.Sales.InvoiceTaxResult>(result.Value);
            Assert.Equal(100.56m, tax.Amount);
            Assert.Equal(new[] { 10m, 0.0555m }, tax.Rates);
        }

        [Fact]
        public void SaveSalesInvoice_DuplicateNumber_RejectsSave()
        {
            sales.Setup(service => service.GetSalesInvoices()).Returns(new[]
            {
                new SalesInvoiceHeader { Id = 99, No = "25" }
            });
            var input = Invoice(1);
            input.No = "25";

            Assert.IsType<ConflictObjectResult>(Controller().SaveSalesInvoice(input));
            sales.Verify(service => service.SaveSalesInvoice(It.IsAny<SalesInvoiceHeader>(),
                It.IsAny<SalesOrderHeader>()), Times.Never);
        }

        private SalesOrderHeader SourceOrder()
        {
            var order = new SalesOrderHeader { Id = 42, CustomerId = 9, PaymentTermId = 3 };
            var line = new SalesOrderLine { Id = 7, ItemId = 1, MeasurementId = 2, Quantity = 5 };
            line.SalesInvoiceLines.Add(new SalesInvoiceLine { Quantity = 2 });
            order.SalesOrderLines.Add(line);
            sales.Setup(service => service.GetSalesOrderById(42)).Returns(order);
            return order;
        }

        private static InvoiceDto Invoice(decimal quantity)
        {
            return new InvoiceDto
            {
                FromSalesOrderId = 42, CustomerId = 9, PaymentTermId = 3,
                SalesInvoiceLines = new List<InvoiceLineDto>
                {
                    new InvoiceLineDto { Id = 7, ItemId = 1, MeasurementId = 2, Quantity = quantity, Amount = 25, Discount = 10 }
                }
            };
        }

        private SalesController Controller()
        {
            return new SalesController(Mock.Of<IAdministrationService>(), sales.Object,
                Mock.Of<IFinancialService>(), Mock.Of<IInventoryService>(), Mock.Of<ITaxService>(),
                NullLogger<SalesController>.Instance);
        }
    }
}
