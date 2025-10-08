using Microsoft.EntityFrameworkCore;
using Serilog;
using SESAggregator.Data;
using SESAggregator.Data.Entities;
using SESAggregator.Models;

namespace SESAggregator.Services;

public class Orchestrator : BackgroundService
{
	private const int BATCH_SIZE = 100;
	private const int POLLING_INTERVAL_SECONDS = 5;
	private const int MAX_TRIES_PER_MESSAGE = 3;
	private const int BACKOFF_DELAY_MINUTES = 10;
	
	private bool backoffTriggered = false;
	private DateTime? backoffTriggeredExpiresAt = null;

	private readonly PeriodicTimer periodicTimer = new(TimeSpan.FromSeconds(POLLING_INTERVAL_SECONDS));

	private readonly SESDispatchService sesDispatchService;
	private readonly AppDbContext db;

	public Orchestrator(SESDispatchService sesDispatchService, AppDbContext db)
	{
		this.sesDispatchService = sesDispatchService;
		this.db = db;
	}
	
	protected override async Task ExecuteAsync(CancellationToken cancellationToken = default)
	{
		Log.Logger.Information("Orchestrator background service starting");

		while (await this.periodicTimer.WaitForNextTickAsync(cancellationToken))
		{
			try
			{

				if (this.backoffTriggered)
				{
					Log.Logger.Debug("Checking for backoff expiration");

					if (this.backoffTriggeredExpiresAt is not null &&
						DateTime.UtcNow < this.backoffTriggeredExpiresAt)
					{
						continue;
					}

					Log.Logger.Debug("Backoff expired. Resuming sending loop");

					this.backoffTriggered = false;
					this.backoffTriggeredExpiresAt = null;
				}

				Log.Logger.Debug("Checking for unsent messages");

				List<Message> unsentBatch = await this.db.Set<Message>()
					.Where(x => x.PermanentlyFailed == false)
					.Where(x => x.SuccessfullySent == false)
					.OrderBy(x => x.Id)
					.Take(BATCH_SIZE)
					.ToListAsync(cancellationToken);

				foreach (Message message in unsentBatch)
				{
					if (this.backoffTriggered)
					{
						break;
					}

					Log.Logger.Information("Sending Message ID: {MessageId} To: {Recipient}", message.Id, message.ToAddress);
					SendMessageResponse response = await this.sesDispatchService.SendMessage(message, cancellationToken);

					message.LastSendingAttemptAt = DateTime.UtcNow;
					message.Tries++;

					if (response.Success)
					{
						message.SuccessfullySent = true;
					}
					else
					{
						message.LastFailureMessage = response.FailureMessage;

						if (response.Backoff)
						{
							this.backoffTriggered = true;
							this.backoffTriggeredExpiresAt = DateTime.UtcNow.AddMinutes(BACKOFF_DELAY_MINUTES);

							message.Tries--; // Decrement tries as we don't want a backoff to hurt the sending attempts counter

						}

						if (!response.Retryable || message.Tries >= MAX_TRIES_PER_MESSAGE)
						{
							message.PermanentlyFailed = true;
						}
					}

					_ = this.db.Update(message);
					_ = await this.db.SaveChangesAsync(cancellationToken);
				}
			}
			catch (Exception ex)
			{
				Log.Logger.Error(ex, "An unhandled exception occured inside the Orchestrator loop! The loop will continue.");
			}
		}
	}
}
