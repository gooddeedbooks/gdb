using Core.Domain.TaxSystem;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace Api.Data.Seed
{
    /// <summary>Seeds standard Canadian sales-tax components and place-of-supply groups.</summary>
    public static class CanadianTaxSeeder
    {
        /// <summary>Adds missing Canadian defaults without overwriting existing tax configuration or assignments.</summary>
        /// <param name="context">The application database context.</param>
        public static void Seed(ApiDbContext context)
        {
            var salesAccount = context.Accounts.FirstOrDefault(a => a.AccountCode == "20300")
                ?? throw new InvalidOperationException("Canadian tax setup requires sales-tax GL account 20300.");
            var purchaseAccount = context.Accounts.FirstOrDefault(a => a.AccountCode == "50700")
                ?? throw new InvalidOperationException("Canadian tax setup requires purchase-tax GL account 50700.");
            var taxes = context.Taxes.ToList();
            var groups = context.TaxGroups.Include(g => g.TaxGroupTax).ToList();
            var itemGroups = context.ItemTaxGroups.Include(g => g.ItemTaxGroupTax).ToList();

            var components = new[]
            {
                new { Code = "CA-GST", Name = "Goods and Services Tax", Rate = 5m },
                new { Code = "CA-HST-ON", Name = "Ontario Harmonized Sales Tax", Rate = 13m },
                new { Code = "CA-HST-NS", Name = "Nova Scotia Harmonized Sales Tax", Rate = 14m },
                new { Code = "CA-HST-15", Name = "Harmonized Sales Tax (NB, NL, PE)", Rate = 15m },
                new { Code = "CA-PST-BC", Name = "British Columbia Provincial Sales Tax", Rate = 7m },
                new { Code = "CA-RST-MB", Name = "Manitoba Retail Sales Tax", Rate = 7m },
                new { Code = "CA-PST-SK", Name = "Saskatchewan Provincial Sales Tax", Rate = 6m },
                new { Code = "CA-QST", Name = "Quebec Sales Tax", Rate = 9.975m }
            };

            foreach (var component in components)
            {
                if (taxes.Any(t => t.TaxCode == component.Code))
                    continue;

                var tax = new Tax
                {
                    TaxCode = component.Code,
                    TaxName = component.Name,
                    Rate = component.Rate,
                    IsActive = true,
                    SalesAccountId = salesAccount.Id,
                    PurchasingAccountId = purchaseAccount.Id
                };
                context.Taxes.Add(tax);
                taxes.Add(tax);
            }

            var provinces = new[]
            {
                new { Code = "AB", Name = "Alberta", Taxes = new[] { "CA-GST" } },
                new { Code = "BC", Name = "British Columbia", Taxes = new[] { "CA-GST", "CA-PST-BC" } },
                new { Code = "MB", Name = "Manitoba", Taxes = new[] { "CA-GST", "CA-RST-MB" } },
                new { Code = "NB", Name = "New Brunswick", Taxes = new[] { "CA-HST-15" } },
                new { Code = "NL", Name = "Newfoundland and Labrador", Taxes = new[] { "CA-HST-15" } },
                new { Code = "NT", Name = "Northwest Territories", Taxes = new[] { "CA-GST" } },
                new { Code = "NS", Name = "Nova Scotia", Taxes = new[] { "CA-HST-NS" } },
                new { Code = "NU", Name = "Nunavut", Taxes = new[] { "CA-GST" } },
                new { Code = "ON", Name = "Ontario", Taxes = new[] { "CA-HST-ON" } },
                new { Code = "PE", Name = "Prince Edward Island", Taxes = new[] { "CA-HST-15" } },
                new { Code = "QC", Name = "Quebec", Taxes = new[] { "CA-GST", "CA-QST" } },
                new { Code = "SK", Name = "Saskatchewan", Taxes = new[] { "CA-GST", "CA-PST-SK" } },
                new { Code = "YT", Name = "Yukon", Taxes = new[] { "CA-GST" } }
            };

            foreach (var province in provinces)
            {
                var description = $"CA-{province.Code} - {province.Name}";
                if (groups.Any(g => g.Description == description))
                    continue;

                var group = new TaxGroup { Description = description, IsActive = true };
                foreach (var code in province.Taxes)
                    group.TaxGroupTax.Add(new TaxGroupTax { Tax = taxes.Single(t => t.TaxCode == code) });
                context.TaxGroups.Add(group);
            }

            foreach (var description in new[] { "Canada - Zero-rated (0%)", "Canada - Exempt" })
            {
                if (!groups.Any(g => g.Description == description))
                    context.TaxGroups.Add(new TaxGroup { Description = description, IsActive = true });
            }

            var regular = itemGroups.FirstOrDefault(g => g.Name == "Regular");
            if (regular == null)
            {
                regular = new ItemTaxGroup { Name = "Regular", IsFullyExempt = false };
                context.ItemTaxGroups.Add(regular);
            }

            foreach (var component in components)
            {
                var tax = taxes.Single(t => t.TaxCode == component.Code);
                if (!regular.ItemTaxGroupTax.Any(t => t.TaxId == tax.Id && tax.Id != 0))
                    regular.ItemTaxGroupTax.Add(new ItemTaxGroupTax { Tax = tax, IsExempt = false });
            }

            if (!itemGroups.Any(g => g.Name == "Exempt"))
                context.ItemTaxGroups.Add(new ItemTaxGroup { Name = "Exempt", IsFullyExempt = true });

            context.SaveChanges();
        }
    }
}
