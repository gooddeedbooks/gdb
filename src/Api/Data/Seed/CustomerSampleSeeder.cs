using Core.Domain;
using Core.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using System;
using System.Globalization;
using System.Linq;

namespace Api.Data.Seed
{
    /// <summary>Creates complete fictional customer samples and fills missing sample fields.</summary>
    public static class CustomerSampleSeeder
    {
        /// <summary>Completes known sample customers without replacing populated values or changing other customers.</summary>
        /// <param name="context">The application database context.</param>
        public static void Seed(ApiDbContext context)
        {
            var samples = new[]
            {
                new
                {
                    Name = "ABC Customer", Phone = "416-555-0101", Fax = "416-555-0102",
                    Email = "accounts@abc-customer.example.com", Website = "https://abc-customer.example.com",
                    Street = "100 Sample Street", Province = "ON", PostalCode = "M5V 2T6",
                    ShippingStreet = "120 Sample Street", ShippingPostalCode = "M5V 2T6",
                    FirstName = "John", LastName = "Doe", ContactEmail = "john.doe@abc-customer.example.com",
                    ContactPhone = "416-555-0103"
                },
                new
                {
                    Name = "Northwind Books", Phone = "604-555-0111", Fax = "604-555-0112",
                    Email = "accounts@northwind-books.example.com", Website = "https://northwind-books.example.com",
                    Street = "200 Example Avenue", Province = "BC", PostalCode = "V6B 1A1",
                    ShippingStreet = "220 Example Avenue", ShippingPostalCode = "V6B 1A1",
                    FirstName = "Alex", LastName = "Morgan", ContactEmail = "alex.morgan@northwind-books.example.com",
                    ContactPhone = "604-555-0113"
                }
            };
            var customers = context.Customers.Include(c => c.Party)
                .Include(c => c.PrimaryContact).ThenInclude(c => c.Party).ToList();
            var accounts = context.Accounts.ToList();
            var groups = context.TaxGroups.ToList();
            var paymentTermId = context.PaymentTerms.Where(p => p.IsActive).OrderBy(p => p.Id)
                .Select(p => (int?)p.Id).FirstOrDefault();

            foreach (var sample in samples)
            {
                var customer = customers.FirstOrDefault(c => c.Party != null && c.Party.Name == sample.Name);
                if (customer == null)
                {
                    customer = new Customer { Party = new Party { Name = sample.Name, PartyType = PartyTypes.Customer, IsActive = true } };
                    context.Customers.Add(customer);
                    customers.Add(customer);
                }

                if (string.IsNullOrWhiteSpace(customer.No))
                {
                    var maximum = customers.Select(c => long.TryParse(c.No, out var number) ? number : 0)
                        .DefaultIfEmpty(0).Max();
                    customer.No = checked(maximum + 1).ToString(CultureInfo.InvariantCulture);
                }

                var party = customer.Party;
                party.Phone = Fill(party.Phone, sample.Phone);
                party.Fax = Fill(party.Fax, sample.Fax);
                party.Email = Fill(party.Email, sample.Email);
                party.Website = Fill(party.Website, sample.Website);
                party.CompanyStreet = Fill(party.CompanyStreet, sample.Street);
                party.CompanyProvince = Fill(party.CompanyProvince, sample.Province);
                party.CompanyPostalCode = Fill(party.CompanyPostalCode, sample.PostalCode);
                party.CompanyCountry = Fill(party.CompanyCountry, "Canada");
                party.ShippingStreet = Fill(party.ShippingStreet, sample.ShippingStreet);
                party.ShippingProvince = Fill(party.ShippingProvince, party.CompanyProvince);
                party.ShippingPostalCode = Fill(party.ShippingPostalCode, sample.ShippingPostalCode);
                party.ShippingCountry = Fill(party.ShippingCountry, party.CompanyCountry);

                if (customer.PrimaryContact == null)
                    customer.PrimaryContact = new Contact { ContactType = ContactTypes.Customer };
                var contact = customer.PrimaryContact;
                contact.FirstName = Fill(contact.FirstName, sample.FirstName);
                contact.LastName = Fill(contact.LastName, sample.LastName);
                if (contact.Party == null)
                    contact.Party = new Party { PartyType = PartyTypes.Contact, IsActive = true };
                contact.Party.Name = Fill(contact.Party.Name, $"{contact.FirstName} {contact.LastName}");
                contact.Party.Phone = Fill(contact.Party.Phone, sample.ContactPhone);
                contact.Party.Email = Fill(contact.Party.Email, sample.ContactEmail);
                contact.Party.Website = Fill(contact.Party.Website, sample.Website);
                contact.Party.Fax = Fill(contact.Party.Fax, sample.Fax);

                customer.PaymentTermId = customer.PaymentTermId ?? paymentTermId;
                customer.AccountsReceivableAccountId = customer.AccountsReceivableAccountId ??
                    accounts.FirstOrDefault(a => a.AccountCode == "10120")?.Id;
                customer.SalesAccountId = customer.SalesAccountId ?? accounts.FirstOrDefault(a => a.AccountCode == "40100")?.Id;
                customer.SalesDiscountAccountId = customer.SalesDiscountAccountId ?? accounts.FirstOrDefault(a => a.AccountCode == "40400")?.Id;
                customer.CustomerAdvancesAccountId = customer.CustomerAdvancesAccountId ?? accounts.FirstOrDefault(a => a.AccountCode == "20120")?.Id;
                customer.TaxGroupId = customer.TaxGroupId ?? groups.FirstOrDefault(g => g.IsActive &&
                    g.Description.StartsWith($"CA-{party.CompanyProvince} - ", StringComparison.OrdinalIgnoreCase))?.Id;
            }

            context.SaveChanges();
        }

        private static string Fill(string value, string sample)
        {
            return string.IsNullOrWhiteSpace(value) ? sample : value;
        }
    }
}
