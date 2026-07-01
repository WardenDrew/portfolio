using System.Text.RegularExpressions;
using FirebaseAdmin.Messaging;
using Platform.Legacy.Core.Extensions.ServiceScanning;
using Platform.Legacy.Core.Models.Push;
using Platform.Legacy.Data.Entities.Users;

namespace Platform.Legacy.Core.Services;

public interface IPushService : IServiceScanningServiceInterface
{
	bool Ready();
	
	Task<IResponse> SendNotification(
		List<int> userIds,
		int notificationId,
		string title,
		string body,
		CancellationToken cancellationToken = default
	);
}

public partial class PushService(
	LegacyDbContext db,
	IPushLifetimeService pushLifetimeService)
	: IPushService, IServiceScanningScopedImplementation
{
	private const int FCM_MAX_MULTICAST_CHUNK_SIZE = 500;

	[GeneratedRegex(pattern: "<[^>]*>", options: RegexOptions.Compiled)]
	private static partial Regex HtmlTagRegex();

	private readonly FirebaseMessaging? firebaseMessaging =
		pushLifetimeService.Firebase != null
			? FirebaseMessaging.GetMessaging(pushLifetimeService.Firebase)
			: null;
			
	public bool Ready()
	{
		return firebaseMessaging != null;
	}

	private static string StripHtml(string html)
	{
		if (string.IsNullOrEmpty(html))
		{
			return html;
		}

		// First replace common block elements with newlines
		string withLineBreaks = html.Replace(oldValue: "</p>", newValue: "</p>\n")
			.Replace(oldValue: "<br>", newValue: "\n")
			.Replace(oldValue: "<br/>", newValue: "\n")
			.Replace(oldValue: "<br />", newValue: "\n")
			.Replace(oldValue: "</div>", newValue: "</div>\n")
			.Replace(oldValue: "</h1>", newValue: "</h1>\n")
			.Replace(oldValue: "</h2>", newValue: "</h2>\n")
			.Replace(oldValue: "</h3>", newValue: "</h3>\n")
			.Replace(oldValue: "</tr>", newValue: "</tr>\n");

		// Remove all HTML tags
		string stripped = PushService.HtmlTagRegex().Replace(input: withLineBreaks, replacement: string.Empty);

		// Clean up excessive whitespace/newlines but preserve intentional line breaks
#pragma warning disable SYSLIB1045
		return Regex
#pragma warning restore SYSLIB1045
			.Replace(input: stripped, pattern: @"\n{3,}", replacement: "\n\n") // Limit to max double newlines
			.Replace(oldValue: "\t", newValue: " ") // Replace tabs with spaces
			.Replace(oldValue: "  ", newValue: " ") // Replace double spaces with single
			.Trim(); // Remove leading/trailing whitespace
	}

	public async Task<IResponse> SendNotification(
		List<int> userIds,
		int notificationId,
		string title,
		string body,
		CancellationToken cancellationToken = default
	)
	{
		if (this.firebaseMessaging is null)
		{
			throw new NotSupportedException("Push not configured");
		}

		Stack<PushToken> pushTokens = new();
		HashSet<int> userIdSet = [.. userIds,];

		// TODO evaluate if this is still the best method once we have a large dataset to compare against
		// Expectations:
		// userIdSet is a large set of unique user id's (500-1000 likely)

		// Foreach streams, processing one row at a time from the SQL server for all rows that match
		foreach (
			UserSession session in db.Set<UserSession>()
				.Where(x => x.SessionExpiresOn > DateTime.UtcNow)
				.Where(x => x.PushRegistrationToken != null)
				.Where(x => x.PushRegistrationExpiresOn > DateTime.UtcNow)
		)
		{
			// This will match all rows that have a valid push token, now compare on the client side with a fast hashset compare if the ID matches
			// Keep this loop VERY tight
			if (userIdSet.Contains(session.UserId))
			{
				pushTokens.Push(
					new PushToken
					{
						DeviceToken = session.PushRegistrationToken,
						Expiration = session.PushRegistrationExpiresOn,
						UserId = session.UserId,
					}
				);
			}
		}

		// Done getting list of push tokens

		while (pushTokens.Count > 0)
		{
			// Grab up to 500 tokens at a time to send to
			List<string> nextChunkTokens = [];
			int nextChunkLength = Math.Min(val1: pushTokens.Count, val2: PushService.FCM_MAX_MULTICAST_CHUNK_SIZE);
			for (int i = 0; i < nextChunkLength; i++)
			{
				string? token = pushTokens.Pop().DeviceToken;

				if (token is null)
				{
					continue;
				}

				nextChunkTokens.Add(token);
			}

			// Build the message structure
			MulticastMessage multicastMessage = new()
			{
				Tokens = nextChunkTokens.AsReadOnly(),
				Notification = new Notification { Title = title, Body = PushService.StripHtml(body), },
				Data = new Dictionary<string, string>()
				{
					{ MessageMetadata.Keys.TYPE, MessageMetadata.Types.NOTIFICATION },
					{ MessageMetadata.Keys.NOTIFICATION_ID, notificationId.ToString() },
					{ MessageMetadata.Keys.HTML_BODY, body }, // Include original HTML in data payload
				},
			};

			// Done building message structure

			// Send the messages!
			BatchResponse batchResponse = await firebaseMessaging.SendEachForMulticastAsync(
				message: multicastMessage,
				cancellationToken: cancellationToken
			);

			// Clean up bad tokens from the database
			if (batchResponse.FailureCount > 0)
			{
				List<string> badTokens = [];

				// The order of responses is the same as the order of tokens, we have to use this to associate which tokens actually failed
				for (int i = 0; i < batchResponse.Responses.Count; i++)
				{
					if (batchResponse.Responses[i].IsSuccess)
					{
						continue;
					}

					try
					{
						badTokens.Add(nextChunkTokens[i]);
					}
					catch (IndexOutOfRangeException)
					{
						// This should never happen, if it does Something went wrong and google sent us not the same number of responses as we sent them tokens
						// Log this somehow in the future?
					}
				}

				// Get our actual usersession entities
				List<UserSession> badTokenSessions = [];
				foreach (string badToken in badTokens)
				{
					badTokenSessions.AddRange(
						await db.Set<UserSession>()
							.Where(x => x.PushRegistrationToken == badToken)
							.ToListAsync(cancellationToken)
					);
				}

				// Clear the token information on each usersession entity
				foreach (UserSession badTokenSession in badTokenSessions)
				{
					badTokenSession.PushRegistrationToken = null;
					badTokenSession.PushRegistrationExpiresOn = null;

					_ = db.Update(badTokenSession);
				}

				_ = await db.SaveChangesAsync(cancellationToken);
			}

			// Done cleaning up bad tokens

			// Done sending this batch of pushes
		}

		// Done sending pushes to all tokens

		return Response.FromSuccess();
	}
}
