using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models;

/// <summary>
/// Links a Participant to a Category (and therefore to an Event).
/// This is the junction table that resolves the many-to-many between
/// Participants and Categories.
/// </summary>
[Table("Enrolments")]
public class Enrolment
{
    [Key]
    public int EnrolmentID { get; set; }

    public int ParticipantID { get; set; }

    public int CategoryID { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;

    [Required, MaxLength(20)]
    public string Status { get; set; } = EnrolmentStatuses.Confirmed;

    public AppUser? Participant { get; set; }
    public Category? Category { get; set; }
    public Result? Result { get; set; }
}

public static class EnrolmentStatuses
{
    public const string Confirmed = "Confirmed";
    public const string Cancelled = "Cancelled";
}
