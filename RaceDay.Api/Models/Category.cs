using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models;

[Table("Categories")]
public class Category
{
    [Key]
    public int CategoryID { get; set; }

    public int EventID { get; set; }

    [Required, MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [Column(TypeName = "decimal(5,2)")]
    public decimal DistanceKM { get; set; }

    [Column(TypeName = "decimal(8,2)")]
    public decimal Price { get; set; }

    public Event? Event { get; set; }
    public RaceRoute? Route { get; set; }
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
