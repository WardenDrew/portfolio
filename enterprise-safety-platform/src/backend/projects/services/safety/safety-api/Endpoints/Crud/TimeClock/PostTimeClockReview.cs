using System.Runtime.CompilerServices;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Platform.Legacy.Core.Extensions;
using Platform.Legacy.Core.Models.API;
using Platform.Legacy.Core.Models.Auth;
using Platform.Legacy.Core.Services;
using Platform.Legacy.Data;
using Platform.Legacy.Data.Entities.Timeclocks;

namespace Platform.Legacy.Api.Endpoints.Crud.TimeClock;

/// <summary>
/// Post TimeClock Review Endpoint
/// </summary>
/// <param name="authorizationService"></param>
/// <param name="db"></param>
internal class PostTimeClockReview(IAuthorizationService authorizationService, LegacyDbContext db)
	: Endpoint<PostTimeClockReview.RequestData, IResponse>
{
	/// <summary>
	/// Request Model
	/// </summary>
	public class RequestData
	{
		public int Id { get; set; }
		public bool? Accept { get; set; }
		public bool? Reject { get; set; }
		public bool? Unreview { get; set; }
		public string? Note { get; set; }
	}

	/// <inheritdoc/>
	public override void Configure()
	{
		Verbs(Http.POST);
		Routes("/admin/timeclock/review", "/timeclock/review");
		Description(
			builder: x =>
				x.WithTags("Timeclock")
					.Accepts<RequestData>("application/json")
					.Produces(201)
					.ProducesProblemFE()
					.ProducesProblemFE()
					.ProducesProblemFE(401)
					.ProducesProblemFE(403)
					.ProducesProblemFE(404),
			clearDefaults: true
		);
	}

	/// <summary>
	/// Fluent Validation
	/// </summary>
	public class Validator : Validator<RequestData>
	{
		public Validator()
		{
			_ = RuleFor(x => x.Note).NotWhiteSpace().MaximumLength(65535);
		}
	}

	/// <inheritdoc/>
	public override async Task HandleAsync(RequestData request, CancellationToken cancellationToken)
	{
		AccessToken? accessToken = authorizationService.ParseCurrentAccessToken();
		if (accessToken is null)
		{
			await Send.ResponseAsync(
				response: Core.Models.API.Response.FromError(Core.Enums.ErrorCodes.Authentication.INVALID_ACCESS_TOKEN),
				statusCode: 401,
				cancellation: cancellationToken
			);
			return;
		}
		if (!accessToken.IsSuperAdmin)
		{
			await Send.ResponseAsync(
				response: Core.Models.API.Response.FromError(Core.Enums.ErrorCodes.Authorization.ADMIN_ONLY),
				statusCode: 403,
				cancellation: cancellationToken
			);
			return;
		}

		// Only one review action flag should be set
		if (
			request.Accept.HasTrueValue() && request.Reject.HasTrueValue()
			|| request.Reject.HasTrueValue() && request.Unreview.HasTrueValue()
			|| request.Unreview.HasTrueValue() && request.Accept.HasTrueValue()
		)
		{
			await Send.ResponseAsync(
				response: Core.Models.API.Response.FromError(PostReviewErrors.BAD_FLAGS),
				statusCode: 400,
				cancellation: cancellationToken
			);
			return;
		}

		IQueryable<Timeclock> query = db.Set<Timeclock>().Where(x => x.CompanyId == accessToken.CompanyId);

		Timeclock? exists = await query
			.Where(x => x.Id == request.Id)
			.Where(x => x.Status != Status.DELETED)
			.FirstOrDefaultAsync(cancellationToken);

		if (exists is null)
		{
			await Send.ResponseAsync(
				response: Core.Models.API.Response.FromError(EntityErrorCodeProvider<Timeclock>.NOT_FOUND),
				statusCode: 404,
				cancellation: cancellationToken
			);
			return;
		}

		if (exists.ClockEnd is null)
		{
			await Send.ResponseAsync(
				response: Core.Models.API.Response.FromError(PostReviewErrors.NOT_CLOSED),
				statusCode: 400,
				cancellation: cancellationToken
			);
			return;
		}

		if (request.Accept.HasTrueValue())
		{
			exists.Status = Status.ACTIVE;
			exists.ApprovedById = accessToken.UserId;
			exists.ApprovedOn = DateTime.UtcNow;
		}
		else if (request.Reject.HasTrueValue())
		{
			exists.Status = Status.INACTIVE;
			exists.ApprovedById = accessToken.UserId;
			exists.ApprovedOn = DateTime.UtcNow;
		}
		else if (request.Unreview.HasTrueValue())
		{
			exists.Status = Status.PENDING;
			exists.ApprovedById = null;
			exists.ApprovedOn = null;
		}
		else
		{
			await Send.ResponseAsync(
				response: Core.Models.API.Response.FromError(PostReviewErrors.BAD_FLAGS),
				statusCode: 400,
				cancellation: cancellationToken
			);
			return;
		}

		_ = db.Update(exists);

		if (request.Note.HasValue())
		{
			TimeclockNote newNote = new()
			{
				TimeclockId = exists.Id,
				Note = request.Note,
				Status = Status.ACTIVE,
				CreatedById = accessToken.UserId,
				CreatedOn = DateTime.UtcNow,
			};

			_ = db.Add(newNote);
		}

		_ = await db.SaveChangesAsync(cancellationToken);

		await Send.ResponseAsync(response: Core.Models.API.Response.FromSuccess(), statusCode: 201, cancellation: cancellationToken);
	}
}

/// <summary>
/// Errors for updating the time clock
/// </summary>
public class PostReviewErrors : IErrorCodeProvider
{
	private static ErrorCode Error(string message, [CallerMemberName] string name = "UNKNOWN")
	{
		return new ErrorCode(name: name, section: nameof(PostTimeClockReview.RequestData), englishTranslation: message, httpStatusCode: 400);
	}

	/// <summary>
	/// Error message for bad flags.
	/// </summary>
	public static readonly ErrorCode BAD_FLAGS = PostReviewErrors.Error(
		"Please select one of the following: Accept, Reject, or Unreview."
	);

	/// <summary>
	/// Error messgae for not closed timeclock entry.
	/// </summary>
	public static readonly ErrorCode NOT_CLOSED = PostReviewErrors.Error(
		"The timeclock entry is still running and cannot be reviewed until clocked out!"
	);
}
