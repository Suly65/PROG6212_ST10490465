using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models;

/// <summary>
/// Route information for a Category (one-to-one). The class is called RaceRoute
/// (not Route) to avoid clashing with Microsoft.AspNetCore.Routing.Route.
/// It maps to the "Routes" table from the Part 1 ERD.
/// </summary>
[Table("Routes")]
public class RaceRoute
{
    [Key]
    public int RouteID { get; set; }

    public int CategoryID { get; set; }

    [Required, MaxLength(100)]
    public string RouteName { get; set; } = string.Empty;

    public int ElevationGainM { get; set; }

    [MaxLength(255)]
    public string? MapURL { get; set; }

    public Category? Category { get; set; }
}
