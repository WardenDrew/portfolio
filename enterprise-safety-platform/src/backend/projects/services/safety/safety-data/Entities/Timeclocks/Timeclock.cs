using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Legacy.Data.Entities.Companies;
using Platform.Legacy.Data.Entities.Users;

// ReSharper disable InconsistentNaming
// ReSharper disable EntityFramework.ModelValidation.CircularDependency

namespace Platform.Legacy.Data.Entities.Timeclocks;

/// <summary>
/// Timeclock Entry
/// </summary>
public class Timeclock : IEntityTypeConfiguration<Timeclock>
{
	/// <summary>
	/// Errors
	/// </summary>
	public class Errors : EntityErrorCodeProvider<Timeclock> { }

	/// <summary>
	/// Record Id
	/// </summary>
	public int Id { get; set; }

	/// <summary>
	/// Company
	/// </summary>
	public int CompanyId { get; set; }

	/// <summary>
	/// Optional Job
	/// </summary>
	public int? JobId { get; set; }

	/// <summary>
	/// User
	/// </summary>
	public int UserId { get; set; }

	/// <summary>
	/// Optional Cost Code
	/// </summary>
	public int? CostCodeId { get; set; }

	/// <summary>
	/// Optional Time Category
	/// </summary>
	public int? CostCategoryId { get; set; }

	/// <summary>
	/// Status
	/// </summary>
	public required string Status { get; set; }

	/// <summary>
	/// Clock Start At
	/// </summary>
	public DateTime ClockStart { get; set; }

	/// <summary>
	/// Clock Start Reason
	/// </summary>
	public int? ClockStartReasonId { get; set; }

	/// <summary>
	/// Clock Start Position
	/// </summary>
	public string? ClockStartLatLng { get; set; }

	/// <summary>
	/// Clock Start Position Determination Method
	/// </summary>
	public string? ClockStartLatLngMethod { get; set; }

	/// <summary>
	/// Clock End At
	/// </summary>
	public DateTime? ClockEnd { get; set; }

	/// <summary>
	/// Clock End Reason
	/// </summary>
	public int? ClockEndReasonId { get; set; }

	/// <summary>
	/// Clock End Position
	/// </summary>
	public string? ClockEndLatLng { get; set; }

	/// <summary>
	/// Clock End Position Determination Method
	/// </summary>
	public string? ClockEndLatLngMethod { get; set; }

	/// <summary>
	/// Entry Created On
	/// </summary>
	public DateTime CreatedOn { get; set; }

	/// <summary>
	/// Craeted By User
	/// </summary>
	public int CreatedById { get; set; }

	/// <summary>
	/// Updated On
	/// </summary>
	public DateTime? UpdatedOn { get; set; }

	/// <summary>
	/// Updated By User
	/// </summary>
	public int? UpdatedById { get; set; }

	/// <summary>
	/// Approved On
	/// </summary>
	public DateTime? ApprovedOn { get; set; }

	/// <summary>
	/// Approved By User
	/// </summary>
	public int? ApprovedById { get; set; }

	/// <summary>
	/// Synced On
	/// </summary>
	public DateTime? SyncedOn { get; set; }

	/// <summary>
	/// Friendly Duration
	/// </summary>
	[NotMapped]
	public string? FriendlyDuration
	{
		get
		{
			if (!ClockEnd.HasValue)
			{
				return null;
			}

			TimeSpan ts = ClockEnd.Value.Subtract(ClockStart);

			return $"{Math.Floor(ts.TotalHours)}:{ts.Minutes:D2}:{ts.Seconds:D2}";
		}
	}

	/// <summary>
	/// Duration String in decimal format
	/// </summary>
	[NotMapped]
	public string? Duration
	{
		get
		{
			if (!ClockEnd.HasValue)
			{
				return null;
			}
			/*
			 * DOL 29 CFR Section 785.48(b)
			 * Rounding must never cause an employee to "lose" any time worked no matter how small, so we round up.
			 *
			 * [12:54] Andrew Haskell
			 * AN FYI on decimal rounding of hours for the purposes of payroll:
			 * https://www.jdsupra.com/legalnews/dol-issues-guidance-on-payroll-rounding-96004/
			 * https://www.ecfr.gov/current/title-29/subtitle-B/chapter-V/subchapter-B/part-785/subpart-D/section-785.48
			 *
			 * Any rounding of employee time must fully compensate the employee all time actually worked.
			 * IE round all time up, and never down.
			 *
			 * Rounding decimal time to 2 decimal places, yields 36 seconds of error
			 * 3 decimal places, 3.6 seconds of error
			 * 4 decimal places 0.36 seconds of error.
			 *
			 * I'm exposing all decimal times in the API to 4 decimal places for reporting to minimize the additional time being paid out
			 *
			 * For an illustration of this, lets say Big Box Store of Big Boxes has 100 employees who all make $15.00 per hour.
			 * Each employee works a full shift (the length of shift doesn't matter) and has 2 seperate clock in / clock out periods.
			 * Each employee works 260 days a year.
			 * So in total there are 52,000 timecard entries for the year in total across all employees.
			 *
			 * Rounding to 2 decimal places, the company would overpay its employees $7,800 in payroll over the year for time not actually worked, simply due to rounding error.
			 * Rounding to 3 decimal places, it is $780 in extra payroll.
			 * Rounding to 4 decimal places, it is $78 in extra payroll.
			 *
			 * [12:57] Andrew Haskell
			 * Now, internally, if a report to sum up the hours worked over a given time range is run, we use perfect datetime math with no error,
			 * the error is only introduced when collapsing hours:minutes:seconds time into decimal hours.
			 *
			 * So in practice, so long as companies are using these summed reports as the source for payroll,
			 * instead of exporting all time entries and summing each entry's decimal hours together,
			 * then the error is highly mitigated.
			 *
			 * But if they are taking an export of all individual timeclock entries, and throwing that in clockify, or paychex, or whatever,
			 * then the error will stack up depending on how many decimal places of precision are used.
			*/

			return decimal.Round(
					d: Convert.ToDecimal(ClockEnd.Value.Subtract(ClockStart).TotalHours),
					decimals: 4,
					mode: MidpointRounding.AwayFromZero
				)
				.ToString("F");
		}
	}

	private Company? _company;

	/// <summary>
	/// Company
	/// </summary>
	/// <exception cref="NavigationUnloadedException"></exception>
	public Company Company
	{
		get => _company ?? throw new NavigationUnloadedException();
		set => _company = value;
	}

	/// <summary>
	/// Job
	/// </summary>
	public Job? Job;

	private User? _user;

	/// <summary>
	/// User
	/// </summary>
	/// <exception cref="NavigationUnloadedException"></exception>
	public User User
	{
		get => _user ?? throw new NavigationUnloadedException();
		set => _user = value;
	}

	private User? _createdBy;

	/// <summary>
	/// Created By User
	/// </summary>
	/// <exception cref="NavigationUnloadedException"></exception>
	public User CreatedBy
	{
		get => _createdBy ?? throw new NavigationUnloadedException();
		set => _createdBy = value;
	}

	/// <summary>
	/// Clock End Reason
	/// </summary>
	public TimeclockReason? ClockEndReason { get; set; }

	/// <summary>
	/// Clock Start Reason
	/// </summary>
	public TimeclockReason? ClockStartReason { get; set; }

	/// <summary>
	/// Time Category
	/// </summary>
	public TimeCategory? CostCategory { get; set; }

	/// <summary>
	/// Cost Code
	/// </summary>
	public CostCode? CostCode { get; set; }

	/// <summary>
	/// Updated By User
	/// </summary>
	public User? UpdatedBy { get; set; }

	/// <summary>
	/// Approved By user
	/// </summary>
	public User? ApprovedBy { get; set; }

	/// <summary>
	/// Notes
	/// </summary>
	public ICollection<TimeclockNote> TimeclockNotes { get; set; } = [];

	/// <summary>
	/// Friendly Status
	/// </summary>
	[NotMapped]
	public string? FriendlyStatus => Platform.Legacy.Common.Models.Status.GetFriendlyStatus(Status);

	/// <inheritdoc />
	public void Configure(EntityTypeBuilder<Timeclock> builder)
	{
		_ = builder.ToTable("Timeclock");

		_ = builder.Ignore(e => e.Duration);
		_ = builder.Ignore(e => e.FriendlyDuration);

		_ = builder.HasIndex(indexExpression: e => e.CompanyId, name: "Fk_Timeclock_Company");
		_ = builder.HasIndex(indexExpression: e => e.CostCategoryId, name: "Fk_Timeclock_CostCategory");
		_ = builder.HasIndex(indexExpression: e => e.CostCodeId, name: "Fk_Timeclock_CostCode");
		_ = builder.HasIndex(indexExpression: e => e.JobId, name: "Fk_Timeclock_Project");
		_ = builder.HasIndex(indexExpression: e => e.ClockStartReasonId, name: "Fk_Timeclock_TimeclockReason");
		_ = builder.HasIndex(indexExpression: e => e.ClockEndReasonId, name: "Fk_Timeclock_TimeclockReason_0");
		_ = builder.HasIndex(indexExpression: e => e.UserId, name: "Fk_Timeclock_User");
		_ = builder.HasIndex(indexExpression: e => e.CreatedById, name: "Fk_Timeclock_User_0");
		_ = builder.HasIndex(indexExpression: e => e.UpdatedById, name: "Fk_Timeclock_User_1");
		_ = builder.HasIndex(indexExpression: e => e.ApprovedById, name: "Fk_Timeclock_User_2");

		_ = builder.Property(e => e.Id).HasColumnType("int(11)");

		_ = builder.Property(e => e.ApprovedById).HasColumnType("int(11)");

		_ = builder.Property(e => e.ApprovedOn).HasColumnType("datetime");

		_ = builder.Property(e => e.ClockEnd).HasColumnType("datetime");

		_ = builder.Property(e => e.ClockEndLatLng).HasColumnType("varchar(100)");

		_ = builder.Property(e => e.ClockEndLatLngMethod).HasColumnType("varchar(50)");

		_ = builder.Property(e => e.ClockEndReasonId).HasColumnType("int(11)");

		_ = builder.Property(e => e.ClockStart).HasColumnType("datetime");

		_ = builder.Property(e => e.ClockStartLatLng).HasColumnType("varchar(100)");

		_ = builder.Property(e => e.ClockStartLatLngMethod).HasColumnType("varchar(50)");

		_ = builder.Property(e => e.ClockStartReasonId).HasColumnType("int(11)");

		_ = builder.Property(e => e.CompanyId).HasColumnType("int(11)");

		_ = builder.Property(e => e.CostCategoryId).HasColumnType("int(11)");

		_ = builder.Property(e => e.CostCodeId).HasColumnType("int(11)");

		_ = builder.Property(e => e.CreatedById).HasColumnType("int(11)");

		_ = builder.Property(e => e.CreatedOn).HasColumnType("datetime").HasDefaultValueSql("current_timestamp()");

		_ = builder.Property(e => e.JobId).HasColumnType("int(11)");

		_ = builder.Property(e => e.Status).IsRequired().HasColumnType("char(1)");

		_ = builder.Property(e => e.SyncedOn).HasColumnType("datetime");

		_ = builder.Property(e => e.UpdatedById).HasColumnType("int(11)");

		_ = builder.Property(e => e.UpdatedOn).HasColumnType("datetime");

		_ = builder.Property(e => e.UserId).HasColumnType("int(11)");

		_ = builder
			.HasOne(d => d.ApprovedBy)
			.WithMany(p => p.TimeclockApprovals)
			.HasForeignKey(d => d.ApprovedById)
			.HasConstraintName("Fk_Timeclock_User_2");

		_ = builder
			.HasOne(d => d.ClockEndReason)
			.WithMany(p => p.TimeclockClockEndReasons)
			.HasForeignKey(d => d.ClockEndReasonId)
			.HasConstraintName("Fk_Timeclock_TimeclockReason_0");

		_ = builder
			.HasOne(d => d.ClockStartReason)
			.WithMany(p => p.TimeclockClockStartReasons)
			.HasForeignKey(d => d.ClockStartReasonId)
			.HasConstraintName("Fk_Timeclock_TimeclockReason");

		_ = builder
			.HasOne(d => d.Company)
			.WithMany(p => p.Timeclocks)
			.HasForeignKey(d => d.CompanyId)
			.OnDelete(DeleteBehavior.ClientSetNull)
			.HasConstraintName("Fk_Timeclock_Company");

		_ = builder
			.HasOne(d => d.CostCategory)
			.WithMany(p => p.Timeclocks)
			.HasForeignKey(d => d.CostCategoryId)
			.HasConstraintName("Fk_Timeclock_CostCategory");

		_ = builder
			.HasOne(d => d.CostCode)
			.WithMany(p => p.Timeclocks)
			.HasForeignKey(d => d.CostCodeId)
			.HasConstraintName("Fk_Timeclock_CostCode");

		_ = builder
			.HasOne(d => d.CreatedBy)
			.WithMany(p => p.TimeclockCreates)
			.HasForeignKey(d => d.CreatedById)
			.OnDelete(DeleteBehavior.ClientSetNull)
			.HasConstraintName("Fk_Timeclock_User_0");

		_ = builder
			.HasOne(d => d.Job)
			.WithMany(p => p.Timeclocks)
			.HasForeignKey(d => d.JobId)
			.OnDelete(DeleteBehavior.ClientSetNull)
			.HasConstraintName("Fk_Timeclock_Project");

		_ = builder
			.HasOne(d => d.UpdatedBy)
			.WithMany(p => p.TimeclockUpdates)
			.HasForeignKey(d => d.UpdatedById)
			.HasConstraintName("Fk_Timeclock_User_1");

		_ = builder
			.HasOne(d => d.User)
			.WithMany(p => p.TimeclockUsers)
			.HasForeignKey(d => d.UserId)
			.OnDelete(DeleteBehavior.ClientSetNull)
			.HasConstraintName("Fk_Timeclock_User");
	}
}
