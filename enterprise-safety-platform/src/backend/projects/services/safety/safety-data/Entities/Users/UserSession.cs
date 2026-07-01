using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength
// ReSharper disable InconsistentNaming
// ReSharper disable EntityFramework.ModelValidation.CircularDependency

namespace Platform.Legacy.Data.Entities.Users;

/// <summary>
/// User Session
/// </summary>
public class UserSession : IEntityTypeConfiguration<UserSession>
{
	/// <summary>
	/// Errors
	/// </summary>
	public class Errors : EntityErrorCodeProvider<UserSession> { }

	/// <summary>
	/// Id
	/// </summary>
	public int Id { get; set; }

	/// <summary>
	/// User Id
	/// </summary>
	public int UserId { get; set; }

	/// <summary>
	/// Access Token Guid
	/// </summary>
	public Guid AccessTokenGuid { get; set; }

	/// <summary>
	/// Refresh Token Guid
	/// </summary>
	public Guid RefreshTokenGuid { get; set; }

	/// <summary>
	/// Psuh Registration Token
	/// </summary>
	public string? PushRegistrationToken { get; set; }

	/// <summary>
	/// Push Registration Token Expires On
	/// </summary>
	public DateTime? PushRegistrationExpiresOn { get; set; }

	/// <summary>
	/// Session Created On
	/// </summary>
	public DateTime SessionCreatedOn { get; set; }

	/// <summary>
	/// Access Token Expires On
	/// </summary>
	public DateTime AccessTokenExpiresOn { get; set; }

	/// <summary>
	/// Session Expires On
	/// </summary>
	public DateTime SessionExpiresOn { get; set; }

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
	public void Configure(EntityTypeBuilder<UserSession> builder)
	{
		_ = builder.ToTable("UserSession");

		_ = builder.HasIndex(indexExpression: e => e.UserId, name: "Fk_UserSession_User");

		_ = builder.Property(e => e.Id).HasColumnType("int(11)");

		_ = builder.Property(e => e.UserId).HasColumnType("int(11)");

		_ = builder
			.Property(e => e.AccessTokenGuid)
			.HasCharSet(CharSet.Ascii)
			.UseCollation("ascii_general_ci")
			.HasColumnType("char(36)")
			.IsRequired();

		_ = builder
			.Property(e => e.RefreshTokenGuid)
			.HasCharSet(CharSet.Ascii)
			.UseCollation("ascii_general_ci")
			.HasColumnType("char(36)")
			.IsRequired();

		_ = builder
			.HasOne(d => d.User)
			.WithMany(p => p.UserSessions)
			.HasForeignKey(d => d.UserId)
			.OnDelete(DeleteBehavior.ClientSetNull)
			.HasConstraintName("Fk_UserSession_User");

		_ = builder.Property(e => e.PushRegistrationToken).HasColumnType("varchar(4096)");

		_ = builder.Property(e => e.PushRegistrationExpiresOn).IsRequired(false).HasColumnType("datetime");

		_ = builder.Property(e => e.SessionCreatedOn).IsRequired().HasColumnType("datetime");

		_ = builder.Property(e => e.AccessTokenExpiresOn).IsRequired().HasColumnType("datetime");

		_ = builder.Property(e => e.SessionExpiresOn).IsRequired().HasColumnType("datetime");
	}
}
