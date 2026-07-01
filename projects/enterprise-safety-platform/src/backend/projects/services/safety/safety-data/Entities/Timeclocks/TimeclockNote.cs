using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Legacy.Data.Entities.Users;

// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength
// ReSharper disable InconsistentNaming
// ReSharper disable EntityFramework.ModelValidation.CircularDependency

namespace Platform.Legacy.Data.Entities.Timeclocks;

/// <summary>
/// Timeclock Note
/// </summary>
public class TimeclockNote : IEntityTypeConfiguration<TimeclockNote>
{
	/// <summary>
	/// Errors
	/// </summary>
	public class Errors : EntityErrorCodeProvider<TimeclockNote> { }

	/// <summary>
	/// Record Id
	/// </summary>
	public int Id { get; set; }

	/// <summary>
	/// Timeclock id
	/// </summary>
	public int TimeclockId { get; set; }

	/// <summary>
	/// Note Text
	/// </summary>
	public required string Note { get; set; }

	/// <summary>
	/// Status
	/// </summary>
	public required string Status { get; set; }

	/// <summary>
	/// Is Fit For Duty
	/// </summary>
	public bool? FitForDuty { get; set; }

	/// <summary>
	/// Is Incident Report
	/// </summary>
	public bool? IncidentReport { get; set; }

	/// <summary>
	/// Created On
	/// </summary>
	public DateTime CreatedOn { get; set; }

	/// <summary>
	/// Created By User
	/// </summary>
	public int CreatedById { get; set; }

	/// <summary>
	/// Parent Note
	/// </summary>
	public int? ParentTimeClockNoteId { get; set; }

	private Timeclock? _timeclock;

	/// <summary>
	/// Timeclock
	/// </summary>
	/// <exception cref="NavigationUnloadedException"></exception>
	public Timeclock Timeclock
	{
		get => _timeclock ?? throw new NavigationUnloadedException();
		set => _timeclock = value;
	}

	private User? _createdBy;

	/// <summary>
	/// Created By
	/// </summary>
	/// <exception cref="NavigationUnloadedException"></exception>
	public User CreatedBy
	{
		get => _createdBy ?? throw new NavigationUnloadedException();
		set => _createdBy = value;
	}

	/// <summary>
	/// Parent Note
	/// </summary>
	public TimeclockNote? ParentTimeclockNote { get; set; }

	/// <summary>
	/// Child Note
	/// </summary>
	public TimeclockNote? ChildTimeclockNote { get; set; }

	/// <inheritdoc />
	public void Configure(EntityTypeBuilder<TimeclockNote> builder)
	{
		_ = builder.ToTable("TimeclockNote");

		_ = builder.HasIndex(indexExpression: e => e.TimeclockId, name: "Fk_TimeclockNote_Timeclock");

		_ = builder.HasIndex(indexExpression: e => e.ParentTimeClockNoteId, name: "FK_ParentTimeclockNote_TimeclockNote").IsUnique();

		_ = builder.Property(e => e.Id).HasColumnType("int(11)");

		_ = builder.Property(e => e.ParentTimeClockNoteId).HasColumnType("int(11)");

		_ = builder.Property(e => e.Status).IsRequired().HasColumnType("char(1)");

		_ = builder.Property(e => e.CreatedById).HasColumnType("int(11)");

		_ = builder.Property(e => e.CreatedOn).HasColumnType("datetime").HasDefaultValueSql("current_timestamp()");

		_ = builder.Property(e => e.Note).IsRequired().HasColumnType("text");

		_ = builder.Property(e => e.TimeclockId).HasColumnType("int(11)");

		_ = builder
			.HasOne(d => d.Timeclock)
			.WithMany(p => p.TimeclockNotes)
			.HasForeignKey(d => d.TimeclockId)
			.OnDelete(DeleteBehavior.ClientSetNull)
			.HasConstraintName("Fk_TimeclockNote_Timeclock");

		_ = builder
			.HasOne(d => d.CreatedBy)
			.WithMany(p => p.TimeclockNotes)
			.HasForeignKey(d => d.CreatedById)
			.OnDelete(DeleteBehavior.ClientSetNull)
			.HasConstraintName("Fk_TimeclockNote_User");

		_ = builder
			.HasOne(d => d.ParentTimeclockNote)
			.WithOne(p => p.ChildTimeclockNote)
			.HasForeignKey<TimeclockNote>(d => d.ParentTimeClockNoteId)
			.HasConstraintName("FK_ParentTimeclockNote_TimeclockNote");
	}
}
