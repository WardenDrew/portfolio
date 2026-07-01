using local_tagcollector.Api;
using local_tagcollector.Configuration;
using local_tagcollector.Influx;
using local_tagcollector.Models;
using local_tagcollector.Modbus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using shared_dotnet.LocalConfiguration;

namespace local_tagcollector.Services;

internal sealed class TagCollectorWorker : BackgroundService
{
    private readonly TagCollectorOptions _tagCollectorOptions;
    private readonly InfluxDbOptions _influxDbOptions;
    private readonly LocalApiConfigurationClient _configurationClient;
    private readonly InfluxLineProtocolWriter _influxWriter;
    private readonly ILogger<TagCollectorWorker> _logger;
    private TagCollectorConfigurationDto? _cachedConfiguration;
    private Guid? _cachedChangeToken;

    public TagCollectorWorker(
        IOptions<TagCollectorOptions> tagCollectorOptions,
        IOptions<InfluxDbOptions> influxDbOptions,
        LocalApiConfigurationClient configurationClient,
        InfluxLineProtocolWriter influxWriter,
        ILogger<TagCollectorWorker> logger)
    {
        _tagCollectorOptions = tagCollectorOptions.Value;
        _influxDbOptions = influxDbOptions.Value;
        _configurationClient = configurationClient;
        _influxWriter = influxWriter;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ValidateOptions();

        _logger.LogInformation(
            "Tag collector running. Poll interval: {PollInterval}; InfluxDB: {InfluxDbUrl}; Bucket: {InfluxDbBucket}; Measurement: {InfluxDbMeasurement}.",
            _tagCollectorOptions.PollInterval,
            _influxDbOptions.Url,
            _influxDbOptions.Bucket,
            _influxDbOptions.Measurement);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Tag collector polling cycle failed.");
            }

            await Task.Delay(_tagCollectorOptions.PollInterval, stoppingToken);
        }
    }

    private async Task PollOnceAsync(CancellationToken cancellationToken)
    {
        TagCollectorConfigurationDto configuration = await GetCurrentConfigurationAsync(cancellationToken);

        if (configuration.Devices.Count == 0)
        {
            _logger.LogWarning("Tag collector polling cycle skipped because local API returned no enabled devices.");
            return;
        }

        List<TagReading> readings = [];

        foreach (TagCollectorDeviceDto device in configuration.Devices)
        {
            try
            {
                IReadOnlyCollection<TagReading> deviceReadings = device.ConnectionMethod switch
                {
                    DeviceConnectionMethod.ModbusTcp => await PollModbusTcpDeviceAsync(device, cancellationToken),
                    _ => throw new InvalidOperationException($"Connection method '{device.ConnectionMethod}' is not supported.")
                };

                readings.AddRange(deviceReadings);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to poll device {DeviceName} at {DeviceHost}:{DevicePort}.",
                    device.Name,
                    device.ModbusTcpConnectionSettings?.Host,
                    device.ModbusTcpConnectionSettings?.Port);
            }
        }

        if (readings.Count == 0)
        {
            _logger.LogWarning("Tag collector polling cycle completed with no readings.");
            return;
        }

        await _influxWriter.WriteAsync(readings, cancellationToken);

        _logger.LogInformation(
            "Tag collector wrote {ReadingCount} readings from {DeviceCount} configured devices.",
            readings.Count,
            configuration.Devices.Count);
    }

    private async Task<TagCollectorConfigurationDto> GetCurrentConfigurationAsync(CancellationToken cancellationToken)
    {
        Guid currentChangeToken;
        try
        {
            currentChangeToken = await _configurationClient.GetChangeTokenAsync(cancellationToken);
        }
        catch (Exception exception) when (_cachedConfiguration is not null)
        {
            _logger.LogWarning(
                exception,
                "Failed to check local API configuration token. Continuing with cached configuration token {ChangeToken}.",
                _cachedChangeToken);

            return _cachedConfiguration;
        }

        TagCollectorConfigurationDto? cachedConfiguration = _cachedConfiguration;
        bool hasCachedConfiguration = cachedConfiguration is not null;
        bool cachedChangeTokenMatches = _cachedChangeToken == currentChangeToken;

        if (cachedConfiguration is not null
            && cachedChangeTokenMatches
            && HasPollableTags(cachedConfiguration))
        {
            return cachedConfiguration;
        }

        TagCollectorConfigurationDto configuration =
            await _configurationClient.GetConfigurationAsync(cancellationToken);

        ValidateConfiguration(configuration);

        _cachedConfiguration = configuration;
        _cachedChangeToken = configuration.ChangeToken == Guid.Empty
            ? currentChangeToken
            : configuration.ChangeToken;

        if (hasCachedConfiguration && cachedChangeTokenMatches && !HasPollableTags(configuration))
        {
            _logger.LogDebug(
                "Checked empty local API configuration token {ChangeToken}. Devices: {DeviceCount}.",
                _cachedChangeToken,
                configuration.Devices.Count);
        }
        else
        {
            _logger.LogInformation(
                "Loaded local API configuration token {ChangeToken}. Devices: {DeviceCount}.",
                _cachedChangeToken,
                configuration.Devices.Count);
        }

        return configuration;
    }

    private static bool HasPollableTags(TagCollectorConfigurationDto configuration)
    {
        return configuration.Devices.Any(device => device.Tags.Count > 0);
    }

    private async Task<IReadOnlyCollection<TagReading>> PollModbusTcpDeviceAsync(
        TagCollectorDeviceDto device,
        CancellationToken cancellationToken)
    {
        if (device.Tags.Count == 0)
        {
            _logger.LogWarning("Device {DeviceName} has no enabled tags to poll.", device.Name);
            return [];
        }

        using ModbusTcpDeviceClient client = new(device);
        await client.ConnectAsync(cancellationToken);

        List<TagReading> readings = [];

        foreach (TagCollectorTagDto tag in device.Tags)
        {
            try
            {
                readings.Add(await client.ReadTagAsync(tag, cancellationToken));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Failed to read tag {TagName} from device {DeviceName}.",
                    tag.Name,
                    device.Name);
            }
        }

        return readings;
    }

    private void ValidateOptions()
    {
        if (_tagCollectorOptions.PollInterval <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("TagCollector:PollInterval must be greater than zero.");
        }

        ValidateInfluxOptions();
    }

    private static void ValidateConfiguration(TagCollectorConfigurationDto configuration)
    {
        HashSet<string> deviceNames = new(StringComparer.OrdinalIgnoreCase);
        foreach (TagCollectorDeviceDto device in configuration.Devices)
        {
            ValidateDevice(device, deviceNames);
        }
    }

    private void ValidateInfluxOptions()
    {
        if (!Uri.IsWellFormedUriString(_influxDbOptions.Url.ToString(), UriKind.Absolute))
        {
            throw new InvalidOperationException("InfluxDb:Url must be an absolute URI.");
        }

        if (string.IsNullOrWhiteSpace(_influxDbOptions.Org))
        {
            throw new InvalidOperationException("InfluxDb:Org is required.");
        }

        if (string.IsNullOrWhiteSpace(_influxDbOptions.Bucket))
        {
            throw new InvalidOperationException("InfluxDb:Bucket is required.");
        }

        if (string.IsNullOrWhiteSpace(_influxDbOptions.Token))
        {
            throw new InvalidOperationException("InfluxDb:Token is required.");
        }

        if (string.IsNullOrWhiteSpace(_influxDbOptions.Measurement))
        {
            throw new InvalidOperationException("InfluxDb:Measurement is required.");
        }
    }

    private static void ValidateDevice(TagCollectorDeviceDto device, HashSet<string> deviceNames)
    {
        if (string.IsNullOrWhiteSpace(device.Name))
        {
            throw new InvalidOperationException("Each local API device entry must include a name.");
        }

        if (!deviceNames.Add(device.Name))
        {
            throw new InvalidOperationException($"Local API configuration contains duplicate device name '{device.Name}'.");
        }

        if (device.ConnectionMethod != DeviceConnectionMethod.ModbusTcp)
        {
            throw new InvalidOperationException(
                $"Device '{device.Name}' uses unsupported connection method '{device.ConnectionMethod}'.");
        }

        ModbusTcpConnectionSettingsDto settings = device.ModbusTcpConnectionSettings
            ?? throw new InvalidOperationException($"Device '{device.Name}' is missing Modbus TCP/IP settings.");

        if (string.IsNullOrWhiteSpace(settings.Host))
        {
            throw new InvalidOperationException($"Device '{device.Name}' must include a Modbus TCP/IP host.");
        }

        if (settings.Port is < 1 or > 65535)
        {
            throw new InvalidOperationException($"Device '{device.Name}' Modbus TCP/IP port must be between 1 and 65535.");
        }

        if (settings.UnitId is < 0 or > byte.MaxValue)
        {
            throw new InvalidOperationException($"Device '{device.Name}' Modbus unit id must be between 0 and {byte.MaxValue}.");
        }

        HashSet<string> tagNames = new(StringComparer.OrdinalIgnoreCase);
        foreach (TagCollectorTagDto tag in device.Tags)
        {
            ValidateTag(device, tag, tagNames);
        }
    }

    private static void ValidateTag(
        TagCollectorDeviceDto device,
        TagCollectorTagDto tag,
        HashSet<string> tagNames)
    {
        if (string.IsNullOrWhiteSpace(tag.Name))
        {
            throw new InvalidOperationException($"Each tag for device '{device.Name}' must include a name.");
        }

        if (!tagNames.Add(tag.Name))
        {
            throw new InvalidOperationException($"Device '{device.Name}' contains duplicate tag name '{tag.Name}'.");
        }

        if (tag.Address is < 0 or > ushort.MaxValue)
        {
            throw new InvalidOperationException(
                $"Tag '{tag.Name}' on device '{device.Name}' address must be between 0 and {ushort.MaxValue}.");
        }

        if (ModbusTcpDeviceClient.IsBitArea(tag.Area) && tag.DataType != TagDataType.Bool)
        {
            throw new InvalidOperationException(
                $"Tag '{tag.Name}' on device '{device.Name}' uses bit area '{tag.Area}' and must use Bool data type.");
        }

        if (!ModbusTcpDeviceClient.IsBitArea(tag.Area))
        {
            int registerCount = ModbusTcpDeviceClient.GetRegisterCount(tag.DataType);
            if (tag.Address > ushort.MaxValue - registerCount + 1)
            {
                throw new InvalidOperationException(
                    $"Tag '{tag.Name}' on device '{device.Name}' exceeds the Modbus register address range.");
            }
        }
    }
}
