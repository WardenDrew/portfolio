using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Platform.Legacy.Data.Entities.Companies;
using Platform.Legacy.Data.Entities.Forms;
using Platform.Legacy.Data.Entities.Training;
using Platform.Legacy.Data.Entities.Users;

// ReSharper disable InconsistentNaming
// ReSharper disable EntityFramework.ModelValidation.CircularDependency

namespace Platform.Legacy.Data.Entities.Assets;

/// <summary>
/// An asset representing a large object storage file in AWS S3
/// </summary>
public class Asset : IEntityTypeConfiguration<Asset>
{
	/// <inheritdoc />
	public class Errors : EntityErrorCodeProvider<Asset>
	{
		/// <summary>
		/// The asset has already been marked as completed upload
		/// </summary>
		public static IErrorCode UPLOAD_COMPLETED_ALREADY =>
			ErrorCodeProvider<Asset>.Error(name: nameof(Errors.UPLOAD_COMPLETED_ALREADY), message: "This Asset has already been marked as Completed Upload!");

		/// <summary>
		/// The asset has not finished uploading and cannot be used until it is flagged as complete
		/// </summary>
		public static IErrorCode UPLOAD_INCOMPLETE =>
			ErrorCodeProvider<Asset>.Error(
				name: nameof(Errors.UPLOAD_INCOMPLETE),
				message: "This Asset has not finished uploading and cannot be used until it is flagged as complete!"
			);

		/// <summary>
		/// The asset must be marked as viewable by all users in this company to use for this purpose
		/// </summary>
		public static IErrorCode NOT_ALL_COMPANY =>
			ErrorCodeProvider<Asset>.Error(
				name: nameof(Errors.NOT_ALL_COMPANY),
				message: "This Asset must be marked as viewable by all users in this company to use for this purpose!"
			);

		/// <summary>
		/// The asset must be marked as cacheable to use for this purpose
		/// </summary>
		public static IErrorCode NOT_CACHEABLE =>
			ErrorCodeProvider<Asset>.Error(name: nameof(Errors.NOT_CACHEABLE), message: "This Asset must be marked as cacheable to use for this purpose!");
	}

	/// <summary>
	/// The database ID of the Asset
	/// </summary>
	public int Id { get; set; }

	/// <summary>
	/// The AWS bucket filename and public ID of the asset. Used for caching on clients.
	/// </summary>
	public Guid PublicId { get; set; }

	/// <summary>
	/// The company this asset belongs to
	/// </summary>
	public int CompanyId { get; set; }

	/// <summary>
	/// The company this asset belongs to
	/// </summary>
	public Company Company
	{
		get => _company ?? throw new NavigationUnloadedException();
		set => _company = value;
	}
	private Company? _company;

	/// <summary>
	/// The user who uploaded this asset
	/// </summary>
	public int UploadedByUserId { get; set; }

	/// <summary>
	/// The Name of the asset
	/// </summary>
	[MaxLength(255)]
	public required string Name { get; set; }

	/// <summary>
	/// The description of the asset
	/// </summary>
	// ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
	public string? Description { get; set; }

	/// <summary>
	/// The Original File Name of the asset. If null, when downloading, use the PublicId for the filename to store as
	/// </summary>
	[MaxLength(255)]
	public string? OriginalFileName { get; set; }

	/// <summary>
	/// The MimeType of the asset.
	/// </summary>
	[MaxLength(255)]
	public required string MimeType { get; set; }

	/// <summary>
	/// Whether this asset should be cached by the client.
	/// </summary>
	public bool Cacheable { get; set; }

	/// <summary>
	/// Allow users in this company to access this asset without a Token
	/// </summary>
	public bool PermitCompany { get; set; }

	/// <summary>
	/// Allow any user in the system to access this asset without a Token
	/// </summary>
	public bool PermitPublic { get; set; }

	/// <summary>
	/// When the asset was created on
	/// </summary>
	public DateTime CreatedOn { get; set; }

	/// <summary>
	/// When the upload of the actual asset binary to storage was completed
	/// </summary>
	public DateTime? UploadCompletedOn { get; set; }

	/// <summary>
	/// When the asset was scanned.
	/// </summary>
	public DateTime? ScannedOn { get; set; }

	/// <summary>
	/// The user who uploaded this asset
	/// </summary>
	public User UploadedByUser
	{
		get => _uploadedByUser ?? throw new NavigationUnloadedException();
		set => _uploadedByUser = value;
	}
	private User? _uploadedByUser;

	/// <summary>
	/// Instructor profile images using this asset
	/// </summary>
	public ICollection<Instructor> InstructorProfileImages { get; set; } = new HashSet<Instructor>();

	/// <summary>
	/// Course header images using this asset
	/// </summary>
	public ICollection<Course> CourseHeaderImages { get; set; } = new HashSet<Course>();

	/// <summary>
	/// Course Asset references to this asset
	/// </summary>
	public ICollection<CourseContentAsset> CourseAssets { get; set; } = new HashSet<CourseContentAsset>();

	/// <summary>
	/// Certificate Templates referencing this asset SHOULD BE IMAGES ONLY
	/// </summary>
	public ICollection<CertificateTemplateAsset> CertificateTemplateAssets { get; set; } =
		new HashSet<CertificateTemplateAsset>();

	/// <summary>
	/// Form Content this asset is used in
	/// </summary>
	public ICollection<FormContentAsset> FormContentAssets { get; set; } = new HashSet<FormContentAsset>();

	/// <summary>
	/// Form submissions this asset is used in
	/// </summary>
	public ICollection<FormSubmissionAsset> FormSubmissionAssets { get; set; } = new HashSet<FormSubmissionAsset>();

	/// <summary>
	/// Profile images this asset is used as
	/// </summary>
	public ICollection<User> ProfileImageAssets { get; set; } = new HashSet<User>();

	/// <summary>
	/// Planner Company Header Image Assets
	/// </summary>
	public HashSet<Company> PlannerCoverHeaderImageAssetCompanies { get; set; } = [];

	/// <summary>
	/// Determine the locations this asset is in use at
	/// </summary>
	/// <param name="db"></param>
	/// <param name="id"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public static async Task<Dictionary<string, int>> GetInUseLocations(
		DbContext db,
		int id,
		CancellationToken cancellationToken = default
	)
	{
		Dictionary<string, int> inUseLocations = new();

		Asset? asset = await db.Set<Asset>()
			.Where(x => x.Id == id)
			.Include(x => x.CertificateTemplateAssets)
			.Include(x => x.CourseAssets)
			.Include(x => x.CourseHeaderImages)
			.Include(x => x.FormContentAssets)
			.Include(x => x.FormSubmissionAssets)
			.Include(x => x.InstructorProfileImages)
			.Include(x => x.ProfileImageAssets)
			.FirstOrDefaultAsync(cancellationToken);
		if (asset is null)
		{
			return inUseLocations;
		}

		foreach (CertificateTemplateAsset use in asset.CertificateTemplateAssets)
		{
			inUseLocations.Add(key: nameof(Asset.CertificateTemplateAssets), value: use.Id);
		}

		foreach (CourseContentAsset use in asset.CourseAssets)
		{
			inUseLocations.Add(key: nameof(Asset.CourseAssets), value: use.Id);
		}

		foreach (Course use in asset.CourseHeaderImages)
		{
			inUseLocations.Add(key: nameof(Asset.CourseHeaderImages), value: use.Id);
		}

		foreach (FormContentAsset use in asset.FormContentAssets)
		{
			inUseLocations.Add(key: nameof(Asset.FormContentAssets), value: use.Id);
		}

		foreach (FormSubmissionAsset use in asset.FormSubmissionAssets)
		{
			inUseLocations.Add(key: nameof(Asset.FormSubmissionAssets), value: use.Id);
		}

		foreach (Instructor use in asset.InstructorProfileImages)
		{
			inUseLocations.Add(key: nameof(Asset.InstructorProfileImages), value: use.Id);
		}

		foreach (User use in asset.ProfileImageAssets)
		{
			inUseLocations.Add(key: nameof(Asset.ProfileImageAssets), value: use.Id);
		}

		foreach (Company use in asset.PlannerCoverHeaderImageAssetCompanies)
		{
			inUseLocations.Add(key: nameof(Asset.PlannerCoverHeaderImageAssetCompanies), value: use.Id);
		}

		return inUseLocations;
	}

	/// <inheritdoc />
	public void Configure(EntityTypeBuilder<Asset> builder)
	{
		_ = builder
			.Property(e => e.PublicId)
			.HasCharSet(CharSet.Ascii)
			.UseCollation("ascii_general_ci")
			.HasColumnType("char(36)")
			.IsRequired();

		_ = builder
			.HasOne(x => x.Company)
			.WithMany(x => x.Assets)
			.HasForeignKey(x => x.CompanyId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}
