using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models;

[Table("Events")]
public class Event
{
    [Key]
    public int EventID { get; set; }

    public int OrganiserID { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime EventDate { get; set; }

    [Required, MaxLength(150)]
    public string Location { get; set; } = string.Empty;

    /// <summary>Overall event distance (added in Part 2 - see README "Deviations").</summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal DistanceKM { get; set; }

    /// <summary>Run, Walk or Cycle (added in Part 2 - see README "Deviations").</summary>
    [Required, MaxLength(20)]
    public string EventType { get; set; } = EventTypes.Run;

    [Column(TypeName = "datetime")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public AppUser? Organiser { get; set; }
    public ICollection<Category> Categories { get; set; } = new List<Category>();
}

public static class EventTypes
{
    public const string Run = "Run";
    public const string Walk = "Walk";
    public const string Cycle = "Cycle";

    public static readonly string[] All = { Run, Walk, Cycle };
}
