using Api.Data;
using Api.Data.Seed;
using Core.Domain;
using Core.Domain.Financials;
using Core.Domain.TaxSystem;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Collections;

namespace GoodBooks.ServicesTests
{
    public class CanadianTaxSeederTests
    {
        private readonly List<Tax> taxes = new List<Tax>();
        private readonly List<TaxGroup> groups = new List<TaxGroup>();
        private readonly List<ItemTaxGroup> itemGroups = new List<ItemTaxGroup>();
        private readonly Mock<ApiDbContext> context;

        public CanadianTaxSeederTests()
        {
            context = new Mock<ApiDbContext>(new DbContextOptionsBuilder<ApiDbContext>().Options);
            context.Setup(c => c.Taxes).Returns(Set(taxes));
            context.Setup(c => c.TaxGroups).Returns(Set(groups));
            context.Setup(c => c.ItemTaxGroups).Returns(Set(itemGroups));
            context.Setup(c => c.Accounts).Returns(Set(new List<Account>
            {
                new Account { Id = 1, AccountCode = "20300" },
                new Account { Id = 2, AccountCode = "50700" }
            }));
            context.Setup(c => c.SaveChanges()).Callback(() =>
            {
                foreach (var group in groups)
                    foreach (var link in group.TaxGroupTax)
                        link.TaxId = link.Tax.Id;
                foreach (var group in itemGroups)
                    foreach (var link in group.ItemTaxGroupTax)
                        link.TaxId = link.Tax.Id;
            }).Returns(1);
        }

        [Theory]
        [InlineData("AB", "5")]
        [InlineData("BC", "12")]
        [InlineData("MB", "12")]
        [InlineData("NB", "15")]
        [InlineData("NL", "15")]
        [InlineData("NT", "5")]
        [InlineData("NS", "14")]
        [InlineData("NU", "5")]
        [InlineData("ON", "13")]
        [InlineData("PE", "15")]
        [InlineData("QC", "14.975")]
        [InlineData("SK", "11")]
        [InlineData("YT", "5")]
        public void Seed_Province_HasExpectedStandardRate(string province, string rate)
        {
            CanadianTaxSeeder.Seed(context.Object);

            var group = Assert.Single(groups, g => g.Description.StartsWith($"CA-{province} - "));
            Assert.Equal(decimal.Parse(rate, System.Globalization.CultureInfo.InvariantCulture),
                group.TaxGroupTax.Sum(t => t.Tax.Rate));
            Assert.True(group.IsActive);
            if (group.TaxGroupTax.Any(t => t.Tax.TaxCode.StartsWith("CA-HST")))
                Assert.Single(group.TaxGroupTax);
        }

        [Fact]
        public void Seed_RepeatedRun_PreservesConfigurationWithoutDuplicates()
        {
            CanadianTaxSeeder.Seed(context.Object);
            var gst = taxes.Single(t => t.TaxCode == "CA-GST");
            gst.SalesAccountId = 99;
            var bc = groups.Single(g => g.Description.StartsWith("CA-BC"));
            bc.IsActive = false;
            bc.TaxAppliedToShipping = true;
            var customer = new Core.Domain.Sales.Customer { TaxGroup = bc, TaxGroupId = bc.Id };
            groups.Add(new TaxGroup { Id = 100, Description = "Legacy custom group", IsActive = true });

            CanadianTaxSeeder.Seed(context.Object);

            Assert.Equal(8, taxes.Count);
            Assert.Equal(16, groups.Count);
            Assert.Equal(8, itemGroups.Single(g => g.Name == "Regular").ItemTaxGroupTax.Count);
            Assert.Equal(99, gst.SalesAccountId);
            Assert.Same(bc, customer.TaxGroup);
            Assert.False(bc.IsActive);
            Assert.True(bc.TaxAppliedToShipping);
        }

        [Fact]
        public void Seed_Quebec_PreservesExactQstRate()
        {
            CanadianTaxSeeder.Seed(context.Object);

            Assert.Equal(9.975m, taxes.Single(t => t.TaxCode == "CA-QST").Rate);
        }

        [Fact]
        public void Seed_NoTaxGroups_AreSeparateZeroRatedAndExempt()
        {
            CanadianTaxSeeder.Seed(context.Object);

            Assert.Empty(groups.Single(g => g.Description == "Canada - Zero-rated (0%)").TaxGroupTax);
            Assert.Empty(groups.Single(g => g.Description == "Canada - Exempt").TaxGroupTax);
            Assert.True(itemGroups.Single(g => g.Name == "Exempt").IsFullyExempt);
        }

        [Fact]
        public void Seed_MissingGlAccounts_ReportsConfigurationError()
        {
            context.Setup(c => c.Accounts).Returns(Set(new List<Account>()));

            Assert.Throws<InvalidOperationException>(() => CanadianTaxSeeder.Seed(context.Object));
        }

        [Fact]
        public void TaxModel_RateColumn_SupportsThreeDecimalPlaces()
        {
            using var db = new ApiDbContext(new DbContextOptionsBuilder<ApiDbContext>()
                .UseSqlServer("Server=localhost;Database=TaxModelOnly;Integrated Security=true").Options);

            Assert.Equal("decimal(18,3)", db.Model.FindEntityType(typeof(Tax))!
                .FindProperty(nameof(Tax.Rate))!.GetColumnType());
        }

        private static DbSet<T> Set<T>(List<T> values) where T : BaseEntity
        {
            var query = values.AsQueryable();
            var set = new Mock<DbSet<T>>();
            set.As<IQueryable<T>>().Setup(s => s.Provider).Returns(query.Provider);
            set.As<IQueryable<T>>().Setup(s => s.Expression).Returns(query.Expression);
            set.As<IQueryable<T>>().Setup(s => s.ElementType).Returns(query.ElementType);
            set.As<IQueryable<T>>().Setup(s => s.GetEnumerator()).Returns(() => values.GetEnumerator());
            set.As<IEnumerable>().Setup(s => s.GetEnumerator()).Returns(() => values.GetEnumerator());
            set.Setup(s => s.Add(It.IsAny<T>())).Callback<T>(value =>
            {
                value.Id = values.Count + 1;
                values.Add(value);
            });
            return set.Object;
        }
    }
}
