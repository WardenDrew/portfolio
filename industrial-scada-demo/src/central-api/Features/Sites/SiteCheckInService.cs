using central_api.Data;
using central_api.Entities;
using central_api.Features.TagReadings;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using shared_dotnet.CentralSync;
using shared_dotnet.TagReadings;

namespace central_api.Features.Sites;

internal sealed class SiteCheckInService(CentralScadaDbContext dbContext, IClock clock)
{
    public async Task<SiteSyncSite?> FindSiteAsync(Guid siteKey, CancellationToken cancellationToken)
    {
        return await dbContext.Sites
            .AsNoTracking()
            .Where(site => site.SiteKey == siteKey)
            .Select(site => new SiteSyncSite(site.Id, site.Name))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<SiteCheckInResult?> CheckInAsync(
        Guid siteKey,
        SiteCheckInRequestDto request,
        CancellationToken cancellationToken)
    {
        var site = await dbContext.Sites
            .Include(item => item.LatestReadings)
            .FirstOrDefaultAsync(item => item.SiteKey == siteKey, cancellationToken);
        if (site is null)
        {
            return null;
        }

        string siteName = request.SiteName.Trim();
        if (!string.IsNullOrWhiteSpace(siteName) && !string.Equals(site.Name, siteName, StringComparison.Ordinal))
        {
            site.Name = siteName;
        }

        site.LastCheckInAt = clock.GetCurrentInstant();
        site.LastAppliedCentralChangeToken = request.LastAppliedCentralChangeToken;

        SiteCheckInResponseDto response = Reconcile(site, request);
        ApplyLatestReadings(site, request.LatestReadings, request.LatestReadingsComplete);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new SiteCheckInResult(site.Id, response);
    }

    private static SiteCheckInResponseDto Reconcile(Site site, SiteCheckInRequestDto request)
    {
        if (request.ResolveConflictWithLocalConfiguration)
        {
            if (request.Configuration is null)
            {
                return CreateResponse(site, requiresUpload: true);
            }

            AcceptLocalConfiguration(site, request.Configuration, request.LocalChangeToken);
            return CreateResponse(site);
        }

        if (site.SyncStatus == SiteSyncStatus.Conflict)
        {
            return CreateResponse(site);
        }

        bool localChanged = site.LastSyncedLocalChangeToken != request.LocalChangeToken;
        bool centralChanged = site.CentralConfigurationJson is not null
            && request.LastAppliedCentralChangeToken != site.CentralChangeToken;

        if (localChanged
            && request.LastAppliedCentralChangeToken == site.CentralChangeToken
            && request.LocalChangeToken == site.CentralChangeToken)
        {
            site.LastSyncedLocalChangeToken = request.LocalChangeToken;
            site.SyncStatus = SiteSyncStatus.Synchronized;
            site.ConflictMessage = null;
            return CreateResponse(site);
        }

        if (localChanged && centralChanged)
        {
            if (request.Configuration is not null)
            {
                site.LocalConfigurationJson = SiteConfigurationJson.Serialize(request.Configuration);
            }

            site.SyncStatus = SiteSyncStatus.Conflict;
            site.ConflictMessage = "Local and central configuration both changed since the last synchronized baseline.";
            return CreateResponse(site);
        }

        if (localChanged)
        {
            if (request.Configuration is null)
            {
                site.SyncStatus = SiteSyncStatus.LocalChanged;
                return CreateResponse(site, requiresUpload: true);
            }

            AcceptLocalConfiguration(site, request.Configuration, request.LocalChangeToken);
            return CreateResponse(site);
        }

        if (centralChanged)
        {
            site.SyncStatus = SiteSyncStatus.CentralChanged;
            site.ConflictMessage = null;
            return CreateResponse(site, configurationToApply: SiteConfigurationJson.Deserialize(site.CentralConfigurationJson));
        }

        site.SyncStatus = SiteSyncStatus.Synchronized;
        site.ConflictMessage = null;
        return CreateResponse(site);
    }

    private static void AcceptLocalConfiguration(
        Site site,
        SiteConfigurationSnapshotDto configuration,
        Guid localChangeToken)
    {
        configuration.ChangeToken = localChangeToken;
        string configurationJson = SiteConfigurationJson.Serialize(configuration);

        site.LocalConfigurationJson = configurationJson;
        site.CentralConfigurationJson = configurationJson;
        site.LastSyncedLocalChangeToken = localChangeToken;
        site.LastAppliedCentralChangeToken = localChangeToken;
        site.CentralChangeToken = localChangeToken;
        site.SyncStatus = SiteSyncStatus.Synchronized;
        site.ConflictMessage = null;
    }

    private static SiteCheckInResponseDto CreateResponse(
        Site site,
        bool requiresUpload = false,
        SiteConfigurationSnapshotDto? configurationToApply = null)
    {
        return new SiteCheckInResponseDto
        {
            SyncStatus = site.SyncStatus,
            CentralChangeToken = site.CentralChangeToken,
            LastSyncedLocalChangeToken = site.LastSyncedLocalChangeToken,
            RequiresConfigurationUpload = requiresUpload,
            ConfigurationToApply = configurationToApply,
            ConflictMessage = site.ConflictMessage
        };
    }

    private static void ApplyLatestReadings(
        Site site,
        IReadOnlyCollection<TagLatestReadingDto> latestReadings,
        bool latestReadingsComplete)
    {
        if (latestReadings.Count == 0 && !latestReadingsComplete)
        {
            return;
        }

        HashSet<Guid> incomingTagIds = latestReadings
            .Select(reading => reading.TagId)
            .ToHashSet();

        if (latestReadingsComplete)
        {
            foreach (SiteTagLatestReading staleReading in site.LatestReadings
                .Where(reading => !incomingTagIds.Contains(reading.TagId))
                .ToList())
            {
                site.LatestReadings.Remove(staleReading);
            }
        }

        Dictionary<Guid, SiteTagLatestReading> existingByTag = site.LatestReadings
            .ToDictionary(reading => reading.TagId);

        foreach (TagLatestReadingDto incoming in latestReadings)
        {
            if (incoming.ValueKind == TagReadingValueKind.Unavailable || incoming.Value is null)
            {
                continue;
            }

            if (!existingByTag.TryGetValue(incoming.TagId, out SiteTagLatestReading? cached))
            {
                cached = new SiteTagLatestReading
                {
                    Id = Guid.NewGuid(),
                    SiteId = site.Id,
                    TagId = incoming.TagId
                };
                site.LatestReadings.Add(cached);
            }

            cached.DeviceId = incoming.DeviceId;
            cached.DeviceName = incoming.DeviceName.Trim();
            cached.TagId = incoming.TagId;
            cached.TagName = incoming.TagName.Trim();
            cached.DataType = incoming.DataType;
            cached.EngineeringUnit = string.IsNullOrWhiteSpace(incoming.EngineeringUnit)
                ? null
                : incoming.EngineeringUnit.Trim();
            cached.ValueKind = incoming.ValueKind;
            cached.Value = incoming.Value;
            cached.ReadingTimestamp = TagReadingMapper.ParseInstant(incoming.Timestamp);
        }
    }
}

internal sealed record SiteSyncSite(Guid Id, string Name);

internal sealed record SiteCheckInResult(Guid SiteId, SiteCheckInResponseDto Response);
