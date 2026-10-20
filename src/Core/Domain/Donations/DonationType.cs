using System.ComponentModel.DataAnnotations;

namespace Core.Domain.Donations
{
    public class DonationType : BaseEntity
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        public int? GlAccountId { get; set; }

        public virtual Financials.Account GlAccount { get; set; }
    }
}
