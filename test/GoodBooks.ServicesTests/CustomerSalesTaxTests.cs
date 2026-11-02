using System.Linq.Expressions;
using Core.Data;
using Core.Domain;
using Core.Domain.Items;
using Core.Domain.Purchases;
using Core.Domain.Sales;
using Core.Domain.TaxSystem;
using Moq;
using Services.TaxSystem;

namespace GoodBooks.ServicesTests
{
    public class CustomerSalesTaxTests
    {
        private readonly Mock<IRepository<Customer>> customers = new Mock<IRepository<Customer>>();
        private readonly Mock<IRepository<Tax>> taxes = new Mock<IRepository<Tax>>();
        private readonly Mock<IRepository<Item>> items = new Mock<IRepository<Item>>(MockBehavior.Strict);

        [Theory]
        [InlineData(0, 10)]
        [InlineData(10, 9)]
        public void GetSalesTaxes_ItemWithoutTaxGroup_UsesCustomerGroup(decimal discount, decimal expected)
        {
            var group = new TaxGroup { IsActive = true };
            group.TaxGroupTax.Add(new TaxGroupTax { TaxId = 1 });
            group.TaxGroupTax.Add(new TaxGroupTax { TaxId = 2 });
            SetCustomers(new Customer { Id = 9, TaxGroup = group });
            SetTaxes(
                new Tax { Id = 1, Rate = 10, IsActive = true },
                new Tax { Id = 2, Rate = 50, IsActive = false },
                new Tax { Id = 3, Rate = 20, IsActive = true });

            var result = Service().GetSalesTaxes(9, 123, 2, 55, discount);

            var tax = Assert.Single(result);
            Assert.Equal(1, tax.Key);
            Assert.Equal(expected, tax.Value);
            items.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GetSalesTaxes_MissingOrInactiveGroup_ReturnsNoTax(bool hasGroup)
        {
            SetCustomers(new Customer { Id = 9, TaxGroup = hasGroup ? new TaxGroup { IsActive = false } : null });

            Assert.Empty(Service().GetSalesTaxes(9, 1, 1, 100, 0));
        }

        [Fact]
        public void GetSalesTaxes_DifferentCustomers_UsesEachAssignedGroup()
        {
            var first = new TaxGroup { IsActive = true };
            first.TaxGroupTax.Add(new TaxGroupTax { TaxId = 1 });
            var second = new TaxGroup { IsActive = true };
            second.TaxGroupTax.Add(new TaxGroupTax { TaxId = 2 });
            SetCustomers(new Customer { Id = 9, TaxGroup = first }, new Customer { Id = 10, TaxGroup = second });
            SetTaxes(new Tax { Id = 1, Rate = 10, IsActive = true }, new Tax { Id = 2, Rate = 20, IsActive = true });
            var service = Service();

            Assert.Equal(10m, Assert.Single(service.GetSalesTaxes(9, 1, 1, 110, 0)).Value);
            Assert.Equal(20m, Assert.Single(service.GetSalesTaxes(10, 1, 1, 120, 0)).Value);
        }

        [Fact]
        public void GetSalesTaxes_UnknownCustomer_Throws()
        {
            SetCustomers();

            Assert.Throws<KeyNotFoundException>(() => Service().GetSalesTaxes(99, 1, 1, 100, 0));
        }

        private void SetCustomers(params Customer[] values)
        {
            customers.Setup(repo => repo.GetAllIncluding(It.IsAny<Expression<Func<Customer, object>>[]>()))
                .Returns(values.AsQueryable());
        }

        private void SetTaxes(params Tax[] values)
        {
            taxes.Setup(repo => repo.GetAllIncluding(It.IsAny<Expression<Func<Tax, object>>[]>()))
                .Returns(values.AsQueryable());
        }

        private TaxService Service()
        {
            return new TaxService(Mock.Of<IRepository<Vendor>>(), customers.Object, items.Object, taxes.Object,
                Mock.Of<IRepository<TaxGroup>>(), Mock.Of<IRepository<TaxGroupTax>>(),
                Mock.Of<IRepository<ItemTaxGroupTax>>(), Mock.Of<IRepository<ItemTaxGroup>>(),
                Mock.Of<IRepository<Party>>());
        }
    }
}
