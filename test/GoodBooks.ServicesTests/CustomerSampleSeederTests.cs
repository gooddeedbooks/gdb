using Api.Data;
using Api.Data.Seed;
using Core.Domain;
using Core.Domain.Financials;
using Core.Domain.Sales;
using Core.Domain.TaxSystem;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace GoodBooks.ServicesTests
{
    public class CustomerSampleSeederTests
    {
        private readonly List<Customer> customers = new List<Customer>();
        private readonly Mock<ApiDbContext> context;

        public CustomerSampleSeederTests()
        {
            context = new Mock<ApiDbContext>(new DbContextOptionsBuilder<ApiDbContext>().Options);
            context.Setup(c => c.Customers).Returns(Set(customers));
            context.Setup(c => c.Accounts).Returns(Set(new List<Account>
            {
                new Account { Id = 1, AccountCode = "10120" },
                new Account { Id = 2, AccountCode = "40100" },
                new Account { Id = 3, AccountCode = "40400" },
                new Account { Id = 4, AccountCode = "20120" }
            }));
            context.Setup(c => c.TaxGroups).Returns(Set(new List<TaxGroup>
            {
                new TaxGroup { Id = 10, Description = "CA-ON - Ontario", IsActive = true },
                new TaxGroup { Id = 11, Description = "CA-BC - British Columbia", IsActive = true }
            }));
            context.Setup(c => c.PaymentTerms).Returns(Set(new List<PaymentTerm>
            {
                new PaymentTerm { Id = 20, IsActive = true }
            }));
        }

        [Fact]
        public void Seed_FreshDatabase_CompletesBothSamples()
        {
            CustomerSampleSeeder.Seed(context.Object);

            Assert.Equal(2, customers.Count);
            foreach (var customer in customers)
            {
                var party = customer.Party;
                Assert.All(new[] { customer.No, party.Name, party.Phone, party.Fax, party.Email, party.Website,
                    party.CompanyStreet, party.CompanyProvince, party.CompanyPostalCode, party.CompanyCountry,
                    party.ShippingStreet, party.ShippingProvince, party.ShippingPostalCode, party.ShippingCountry,
                    customer.PrimaryContact.FirstName, customer.PrimaryContact.LastName,
                    customer.PrimaryContact.Party.Name, customer.PrimaryContact.Party.Phone,
                    customer.PrimaryContact.Party.Email, customer.PrimaryContact.Party.Website },
                    value => Assert.False(string.IsNullOrWhiteSpace(value)));
                Assert.Contains("example.com", party.Email);
                Assert.Contains("555-01", party.Phone);
                Assert.Equal(20, customer.PaymentTermId);
                Assert.Equal(1, customer.AccountsReceivableAccountId);
                Assert.Equal(2, customer.SalesAccountId);
                Assert.Equal(3, customer.SalesDiscountAccountId);
                Assert.Equal(4, customer.CustomerAdvancesAccountId);
                Assert.Equal(party.CompanyProvince == "ON" ? 10 : 11, customer.TaxGroupId);
            }
        }

        [Fact]
        public void Seed_ExistingSamples_FillsBlanksAndPreservesEdits()
        {
            var sample = new Customer
            {
                Id = 7, No = "50", TaxGroupId = 99, PaymentTermId = 88,
                Party = new Party { Name = "ABC Customer", Phone = "CUSTOM-PHONE", CompanyProvince = "AB" },
                PrimaryContact = new Contact
                {
                    FirstName = "Custom", LastName = "Contact",
                    Party = new Party { Email = "custom@example.com" }
                }
            };
            var unrelated = new Customer { No = "100", Party = new Party { Name = "User customer" } };
            customers.Add(sample);
            customers.Add(unrelated);

            CustomerSampleSeeder.Seed(context.Object);
            CustomerSampleSeeder.Seed(context.Object);

            Assert.Equal(3, customers.Count);
            Assert.Equal("CUSTOM-PHONE", sample.Party.Phone);
            Assert.Equal("AB", sample.Party.CompanyProvince);
            Assert.Equal("custom@example.com", sample.PrimaryContact.Party.Email);
            Assert.Equal(99, sample.TaxGroupId);
            Assert.Equal(88, sample.PaymentTermId);
            Assert.False(string.IsNullOrWhiteSpace(sample.Party.Website));
            Assert.Null(unrelated.Party.Phone);
            Assert.Equal("101", customers.Single(c => c.Party.Name == "Northwind Books").No);
        }

        private static DbSet<T> Set<T>(List<T> values) where T : BaseEntity
        {
            var query = values.AsQueryable();
            var set = new Mock<DbSet<T>>();
            set.As<IQueryable<T>>().Setup(s => s.Provider).Returns(query.Provider);
            set.As<IQueryable<T>>().Setup(s => s.Expression).Returns(query.Expression);
            set.As<IQueryable<T>>().Setup(s => s.ElementType).Returns(query.ElementType);
            set.As<IQueryable<T>>().Setup(s => s.GetEnumerator()).Returns(() => values.GetEnumerator());
            set.Setup(s => s.Add(It.IsAny<T>())).Callback<T>(values.Add);
            return set.Object;
        }
    }
}
