using System.ComponentModel.DataAnnotations;

namespace Dto.Administration
{
  public class Company : BaseDto
  {
    [Required]
    [Display(Name = "Company Code")]
    public string? CompanyCode { get; set; }

    [Required]
    [Display(Name = "Name")]
    public string? Name { get; set; }

    [Required]
    [Display(Name = "Short Name")]
    public string? ShortName { get; set; }

    [Required]
    [Display(Name = "CRA")]
    public string? CRA { get; set; }

    [Display(Name = "Street")]
    public string? Street { get; set; }

    [Display(Name = "City")]
    public string? City { get; set; }

    [Display(Name = "Province")]
    public string? Province { get; set; }

    [Display(Name = "Postal Code")]
    public string? PostalCode { get; set; }

    [Display(Name = "Logo")]
    public byte[]? Logo { get; set; }
  }
}
