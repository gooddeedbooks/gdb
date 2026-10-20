using System.ComponentModel.DataAnnotations;

namespace Dto.DonationTypes
{
    public class DonationType
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required. Enter the donation type name."),
         StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string Description { get; set; } = string.Empty;

        public int? GlAccountId { get; set; }

        public string GlAccountCode { get; set; } = string.Empty;

        public string GlAccountName { get; set; } = string.Empty;
    }
}
