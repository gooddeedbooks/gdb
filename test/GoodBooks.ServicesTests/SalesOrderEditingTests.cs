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
using SalesOrderDto = global::Dto.Sales.SalesOrder;
using SalesOrderLineDto = global::Dto.Sales.SalesOrderLine;

namespace GoodBooks.ServicesTests
{
    public class SalesOrderEditingTests
    {
        private readonly Mock<ISalesService> sales = new Mock<ISalesService>();

        [Fact]
        public void SaveSalesOrder_ExistingOrder_UpdatesOriginalAndRetainsDistinctNewLines()
        {
            var order = new SalesOrderHeader { Id = 42 };
            order.SalesOrderLines.Add(new SalesOrderLine { Id = 7, ItemId = 1, Quantity = 1 });
            order.SalesOrderLines.Add(new SalesOrderLine { Id = 8, ItemId = 2, Quantity = 1 });
            sales.Setup(service => service.GetSalesOrderById(42)).Returns(order);
            var input = new SalesOrderDto
            {
                Id = 42, CustomerId = 9, PaymentTermId = 3, OrderDate = new DateTime(2026, 11, 2),
                SalesOrderLines = new List<SalesOrderLineDto>
                {
                    new SalesOrderLineDto { Id = 7, ItemId = 1, MeasurementId = 1, Quantity = 4, Amount = 25, Discount = 10 },
                    new SalesOrderLineDto { ItemId = 3, MeasurementId = 1, Quantity = 2, Amount = 30 },
                    new SalesOrderLineDto { ItemId = 4, MeasurementId = 2, Quantity = 3, Amount = 40 }
                }
            };

            var result = Controller().SaveSalesOrder(input);

            Assert.IsType<OkObjectResult>(result);
            Assert.Equal(9, order.CustomerId);
            Assert.Equal(3, order.PaymentTermId);
            Assert.Equal(input.OrderDate, order.Date);
            Assert.Equal(3, order.SalesOrderLines.Count);
            Assert.DoesNotContain(order.SalesOrderLines, line => line.Id == 8);
            Assert.Contains(order.SalesOrderLines, line => line.Id == 7 && line.Quantity == 4 && line.Amount == 25 && line.Discount == 10);
            Assert.Contains(order.SalesOrderLines, line => line.ItemId == 3 && line.Quantity == 2);
            Assert.Contains(order.SalesOrderLines, line => line.ItemId == 4 && line.Quantity == 3);
            sales.Verify(service => service.UpdateSalesOrder(order), Times.Once);
            sales.Verify(service => service.AddSalesOrder(It.IsAny<SalesOrderHeader>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public void SaveSalesOrder_MissingOrder_ReturnsNotFoundWithoutCreatingOrder()
        {
            var result = Controller().SaveSalesOrder(new SalesOrderDto { Id = 42 });

            Assert.IsType<NotFoundObjectResult>(result);
            sales.Verify(service => service.UpdateSalesOrder(It.IsAny<SalesOrderHeader>()), Times.Never);
            sales.Verify(service => service.AddSalesOrder(It.IsAny<SalesOrderHeader>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public void SaveSalesOrder_RemovingInvoicedLine_RejectsSave()
        {
            var order = new SalesOrderHeader { Id = 42 };
            var line = new SalesOrderLine { Id = 7 };
            line.SalesInvoiceLines.Add(new SalesInvoiceLine());
            order.SalesOrderLines.Add(line);
            sales.Setup(service => service.GetSalesOrderById(42)).Returns(order);

            var result = Controller().SaveSalesOrder(new SalesOrderDto
            {
                Id = 42, SalesOrderLines = new List<SalesOrderLineDto>()
            });

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains(line, order.SalesOrderLines);
            sales.Verify(service => service.UpdateSalesOrder(It.IsAny<SalesOrderHeader>()), Times.Never);
        }

        private SalesController Controller()
        {
            return new SalesController(Mock.Of<IAdministrationService>(), sales.Object,
                Mock.Of<IFinancialService>(), Mock.Of<IInventoryService>(), Mock.Of<ITaxService>(),
                NullLogger<SalesController>.Instance);
        }
    }
}
