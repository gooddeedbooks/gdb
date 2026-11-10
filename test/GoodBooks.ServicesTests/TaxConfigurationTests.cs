using AutoMapper;
using Core.Data;
using Core.Domain;
using Core.Domain.Auditing;
using Core.Domain.Financials;
using Core.Domain.TaxSystem;
using Moq;
using Services.Administration;
using Services.Financial;
using Services.TaxSystem;

namespace GoodBooks.ServicesTests
{
    public class TaxConfigurationTests
    {
        private readonly Tax tax = new Tax { Id = 1, Rate = 5, TaxCode = "CA-GST", TaxName = "GST" };
        private readonly Mock<IRepository<Tax>> taxRepository = new Mock<IRepository<Tax>>();
        private readonly Mock<IRepository<TaxGroup>> groups = new Mock<IRepository<TaxGroup>>();
        private readonly Mock<ITaxService> taxes = new Mock<ITaxService>();
        private readonly Mock<IFinancialService> financial = new Mock<IFinancialService>();

        public TaxConfigurationTests()
        {
            tax.TaxGroupTaxes.Add(new TaxGroupTax { TaxGroupId = 10 });
            tax.TaxGroupTaxes.Add(new TaxGroupTax { TaxGroupId = 11 });
            tax.ItemTaxGroupTaxes.Add(new ItemTaxGroupTax { ItemTaxGroupId = 20 });
            taxes.Setup(s => s.GetTaxById(1)).Returns(tax);
            financial.Setup(s => s.GetAccountByAccountCode("20300")).Returns(new Account { Id = 2 });
            financial.Setup(s => s.GetAccountByAccountCode("50700")).Returns(new Account { Id = 3 });
            taxRepository.Setup(r => r.UpdateAsync(tax)).Returns(Task.CompletedTask);
        }

        [Fact]
        public async Task EditTax_SharedGst_PreservesAllProvincialAndItemGroupMemberships()
        {
            var input = Update(5);
            input.TaxGroup = new global::Dto.TaxSystem.TaxGroup();
            input.ItemTaxGroup = new global::Dto.TaxSystem.ItemTaxGroup();

            var result = await Service().EditTaxAsync(input);

            Assert.False(result.IsFailure);
            Assert.Equal(new[] { 10, 11 }, tax.TaxGroupTaxes.Select(t => t.TaxGroupId));
            Assert.Equal(20, Assert.Single(tax.ItemTaxGroupTaxes).ItemTaxGroupId);
        }

        [Fact]
        public async Task EditTax_ExistingGroup_DoesNotRemoveOtherGroupsOrDuplicateLink()
        {
            groups.Setup(r => r.GetById(10)).Returns(new TaxGroup { Id = 10, Description = "CA-AB - Alberta" });
            var input = Update(5);
            input.TaxGroup = new global::Dto.TaxSystem.TaxGroup { Id = 10 };

            await Service().EditTaxAsync(input);

            Assert.Equal(2, tax.TaxGroupTaxes.Count);
        }

        [Fact]
        public async Task EditTax_QstRate_PreservesThreeDecimalPlaces()
        {
            var result = await Service().EditTaxAsync(Update(9.975m));

            Assert.False(result.IsFailure);
            Assert.Equal(9.975m, tax.Rate);
        }

        [Theory]
        [InlineData("-1")]
        [InlineData("101")]
        [InlineData("9.9755")]
        public async Task EditTax_InvalidRate_RejectsRatherThanRounding(string value)
        {
            var result = await Service().EditTaxAsync(Update(decimal.Parse(value,
                System.Globalization.CultureInfo.InvariantCulture)));

            Assert.True(result.IsFailure);
            taxRepository.Verify(r => r.UpdateAsync(It.IsAny<Tax>()), Times.Never);
        }

        private static global::Dto.TaxSystem.TaxForUpdate Update(decimal rate)
        {
            return new global::Dto.TaxSystem.TaxForUpdate
            {
                SalesAccountId = 20300,
                PurchaseAccountId = 50700,
                Tax = new global::Dto.TaxSystem.Tax
                {
                    Id = 1, TaxCode = "CA-GST", TaxName = "GST", Rate = rate, IsActive = true
                }
            };
        }

        private AdministrationService Service()
        {
            var mapper = new MapperConfiguration(c => c.AddProfile(new Api.MappingProfile())).CreateMapper();
            return new AdministrationService(Mock.Of<IRepository<FinancialYear>>(), groups.Object,
                Mock.Of<IRepository<ItemTaxGroup>>(), Mock.Of<IRepository<PaymentTerm>>(),
                Mock.Of<IRepository<Bank>>(), taxRepository.Object,
                Mock.Of<IRepository<GeneralLedgerSetting>>(), Mock.Of<IRepository<Account>>(),
                Mock.Of<IRepository<AuditLog>>(), Mock.Of<ISecurityRepository>(),
                financial.Object, taxes.Object, mapper);
        }
    }
}
