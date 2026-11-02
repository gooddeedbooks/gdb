using System.ComponentModel.DataAnnotations;

namespace Dto.Inventory
{
    /// <summary>Describes a unit of measure stored in the existing Measurement lookup.</summary>
    public class Unit
    {
        /// <summary>Gets or sets the lookup identifier.</summary>
        public int Id { get; set; }

        /// <summary>Gets or sets the unit's unique code.</summary>
        [Required(ErrorMessage = "Code is required. Enter the unit code.")]
        [StringLength(20, ErrorMessage = "Code cannot exceed 20 characters.")]
        public string Code { get; set; } = string.Empty;

        /// <summary>Gets or sets the name displayed in unit dropdowns.</summary>
        [Required(ErrorMessage = "Description is required. Enter the unit description.")]
        [StringLength(100, ErrorMessage = "Description cannot exceed 100 characters.")]
        public string Description { get; set; } = string.Empty;
    }
}
