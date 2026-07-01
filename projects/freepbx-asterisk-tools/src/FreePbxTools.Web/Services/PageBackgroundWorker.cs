namespace FreePbxTools.Web.Services;

public class PageBackgroundWorker(
	ILogger<PageBackgroundWorker> logger,
	SettingsService settings,
	PagingService paging) : BackgroundService
{
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		logger.LogInformation("PageBackgroundWorker starting.");

		DateTime? lastFiredAt = null;
		
		while (!stoppingToken.IsCancellationRequested)
		{
			await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
			DateTime now = DateTime.Now;
			if (lastFiredAt is not null && 
				lastFiredAt.Value.Hour == now.Hour && 
				lastFiredAt.Value.Minute == now.Minute)
			{
				continue;
			}
			
			Guid? scheduleId = null;

			SettingsService.OverrideModel? overrideToday = settings.Running.Overrides
				.Where(
					x =>
						x.Date != null
						&& x.Date.Value.Year == now.Year
						&& x.Date.Value.Month == now.Month
						&& x.Date.Value.Day == now.Day
				)
				.FirstOrDefault();

			if (overrideToday is not null)
			{
				scheduleId = overrideToday.Schedule;
			}
			else
			{
				scheduleId = now.DayOfWeek switch
				{
					DayOfWeek.Monday => settings.Running.Plan.Monday,
					DayOfWeek.Tuesday => settings.Running.Plan.Tuesday,
					DayOfWeek.Wednesday => settings.Running.Plan.Wednesday,
					DayOfWeek.Thursday => settings.Running.Plan.Thursday,
					DayOfWeek.Friday => settings.Running.Plan.Friday,
					DayOfWeek.Saturday => settings.Running.Plan.Saturday,
					DayOfWeek.Sunday => settings.Running.Plan.Sunday,
					_ => null,
				};
			}
			
			if (scheduleId is null || scheduleId == Guid.Empty) continue;
			
			SettingsService.ScheduleModel? schedule = settings.Running.Schedules
				.FirstOrDefault(s => s.Id == scheduleId);
			if (schedule is null) continue;

			
			SettingsService.EventModel? currentEvent = schedule.Events
				.Where(x => 
					x.Time != null &&
					x.Time.Value.Hour == now.Hour &&
					x.Time.Value.Minute == now.Minute)
				.FirstOrDefault();
			if (currentEvent is null) continue;
			
			logger.LogDebug($"Event Firing: {schedule.Name} - {currentEvent.Time}");
			lastFiredAt = now;
			foreach (string extension in currentEvent.PageGroups)
			{
				logger.LogDebug($"Paging: {extension}");
				await paging.PageAsync(extension, stoppingToken);
				logger.LogDebug("Waiting for page to finish");
				await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
			}
			logger.LogDebug("Event completed");
		}
		
		logger.LogInformation("PageBackgroundWorker stopping.");
	}
}