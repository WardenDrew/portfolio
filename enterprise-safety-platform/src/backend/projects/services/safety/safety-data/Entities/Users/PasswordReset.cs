using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength
// ReSharper disable InconsistentNaming
// ReSharper disable EntityFramework.ModelValidation.CircularDependency

namespace Platform.Legacy.Data.Entities.Users;

/// <summary>
/// Password Reset
/// </summary>
public class PasswordReset : IEntityTypeConfiguration<PasswordReset>
{
	/// <summary>
	/// Errors
	/// </summary>
	public class Errors : EntityErrorCodeProvider<PasswordReset> { }

	/// <summary>
	/// Record Id
	/// </summary>
	public Guid Id { get; set; }

	/// <summary>
	/// Internal Token
	/// </summary>
	public required string InternalToken { get; set; }

	/// <summary>
	/// Created On
	/// </summary>
	public DateTime CreatedOn { get; set; }

	/// <summary>
	/// Expires On
	/// </summary>
	public DateTime ExpiresOn { get; set; }

	/// <summary>
	/// User
	/// </summary>
	public int UserId { get; set; }

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

	/// <inheritdoc />
	public void Configure(EntityTypeBuilder<PasswordReset> builder)
	{
		_ = builder.ToTable("PasswordResets");

		_ = builder.HasKey(e => e.Id);

		_ = builder
			.Property(e => e.Id)
			.HasCharSet(CharSet.Ascii)
			.UseCollation("ascii_general_ci")
			.HasColumnType("char(36)")
			.IsRequired()
			.ValueGeneratedOnAdd();

		_ = builder.Property(e => e.InternalToken).HasColumnType("longtext");

		_ = builder.Property(e => e.CreatedOn).HasColumnType("datetime");
		_ = builder.Property(e => e.ExpiresOn).HasColumnType("datetime");

		_ = builder.Property(e => e.UserId).HasColumnType("int(11)");

		_ = builder
			.HasOne(d => d.User)
			.WithMany(p => p.PasswordResets)
			.HasForeignKey(d => d.UserId)
			.OnDelete(DeleteBehavior.Cascade)
			.HasConstraintName("FK_PasswordResets_User_UserId");
	}
}
