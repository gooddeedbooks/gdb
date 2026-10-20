using System;

namespace Dto.Donations
{
    public class DonationReceipt
    {
        public string ReceiptNo { get; set; } = string.Empty;
        public string CharityName { get; set; } = string.Empty;
        public string CharityStreet { get; set; } = string.Empty;
        public string CharityCity { get; set; } = string.Empty;
        public string CharityProvince { get; set; } = string.Empty;
        public string CharityPostalCode { get; set; } = string.Empty;
        public string RegistrationNumber { get; set; } = string.Empty;
        public DateTime IssuedDate { get; set; }
        public string LocationIssued { get; set; } = string.Empty;
        public string DonorName { get; set; } = string.Empty;
        public string DonorStreet { get; set; } = string.Empty;
        public string DonorCity { get; set; } = string.Empty;
        public string DonorProvince { get; set; } = string.Empty;
        public string DonorPostalCode { get; set; } = string.Empty;
        public DateTime DonationDate { get; set; }
        public decimal Amount { get; set; }
        public decimal AdvantageAmount { get; set; }
        public decimal EligibleAmount { get; set; }
    }
}
