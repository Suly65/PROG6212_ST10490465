using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

/// <summary>
/// EF Core context. The configuration below makes the generated schema match the
/// Part 1 ERD and docs/RaceDay_Schema.sql (keys, unique constraints, check
/// constraints, defaults and delete behaviour).
/// </summary>
public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options)
    {
    }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<RaceRoute> Routes => Set<RaceRoute>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ---------------- Users ----------------
        modelBuilder.Entity<AppUser>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Role).HasDefaultValue(Roles.Participant);
            e.Property(u => u.CreatedAt).HasDefaultValueSql("GETDATE()");
            e.ToTable(t => t.HasCheckConstraint("CK_Users_Role", "[Role] IN ('Organiser','Participant')"));
        });

        // ---------------- Events ----------------
        modelBuilder.Entity<Event>(e =>
        {
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
            e.ToTable(t => t.HasCheckConstraint("CK_Events_EventType", "[EventType] IN ('Run','Walk','Cycle')"));

            // One Organiser -> many Events (no cascade: an Organiser with events cannot be removed by accident)
            e.HasOne(x => x.Organiser)
                .WithMany(u => u.OrganisedEvents)
                .HasForeignKey(x => x.OrganiserID)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------------- Categories ----------------
        modelBuilder.Entity<Category>(e =>
        {
            e.Property(c => c.Price).HasDefaultValue(0m);

            // One Event -> many Categories. Deleting an Event removes its Categories.
            e.HasOne(c => c.Event)
                .WithMany(ev => ev.Categories)
                .HasForeignKey(c => c.EventID)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------------- Routes ----------------
        modelBuilder.Entity<RaceRoute>(e =>
        {
            e.Property(r => r.ElevationGainM).HasDefaultValue(0);
            e.HasIndex(r => r.CategoryID).IsUnique();

            // One Category -> one Route (1:1)
            e.HasOne(r => r.Category)
                .WithOne(c => c.Route)
                .HasForeignKey<RaceRoute>(r => r.CategoryID)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------------- Enrolments ----------------
        modelBuilder.Entity<Enrolment>(e =>
        {
            e.Property(x => x.Status).HasDefaultValue(EnrolmentStatuses.Confirmed);
            e.Property(x => x.EnrolmentDate).HasDefaultValueSql("GETDATE()");
            e.ToTable(t => t.HasCheckConstraint("CK_Enrolments_Status", "[Status] IN ('Confirmed','Cancelled')"));

            // One Participant -> many Enrolments (Restrict avoids SQL Server multiple cascade paths)
            e.HasOne(x => x.Participant)
                .WithMany(u => u.Enrolments)
                .HasForeignKey(x => x.ParticipantID)
                .OnDelete(DeleteBehavior.Restrict);

            // One Category -> many Enrolments
            e.HasOne(x => x.Category)
                .WithMany(c => c.Enrolments)
                .HasForeignKey(x => x.CategoryID)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------------- Results ----------------
        modelBuilder.Entity<Result>(e =>
        {
            e.Property(r => r.FinishTime).HasColumnType("time");
            e.Property(r => r.RecordedAt).HasDefaultValueSql("GETDATE()");
            e.HasIndex(r => r.EnrolmentID).IsUnique();

            // One Enrolment -> zero or one Result
            e.HasOne(r => r.Enrolment)
                .WithOne(en => en.Result)
                .HasForeignKey<Result>(r => r.EnrolmentID)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
