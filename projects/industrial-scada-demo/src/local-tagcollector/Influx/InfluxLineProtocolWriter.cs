using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using local_tagcollector.Configuration;
using local_tagcollector.Models;
using Microsoft.Extensions.Options;

namespace local_tagcollector.Influx;

internal sealed class InfluxLineProtocolWriter : IDisposable
{
    private readonly InfluxDbOptions _options;
    private readonly HttpClient _httpClient = new();
    private readonly Uri _writeUri;

    public InfluxLineProtocolWriter(IOptions<InfluxDbOptions> options)
    {
        _options = options.Value;
        _writeUri = BuildWriteUri(_options);
    }

    public async Task WriteAsync(IReadOnlyCollection<TagReading> readings, CancellationToken cancellationToken)
    {
        if (readings.Count == 0)
        {
            return;
        }

        string payload = string.Join('\n', readings.Select(FormatReading));

        using HttpRequestMessage request = new(HttpMethod.Post, _writeUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Token", _options.Token);
        request.Content = new StringContent(payload, Encoding.UTF8, "text/plain");

        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            string body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"InfluxDB write failed with status {(int)response.StatusCode} {response.ReasonPhrase}: {body}");
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    private string FormatReading(TagReading reading)
    {
        string field = reading.Value is bool boolValue
            ? $"value_bool={boolValue.ToString().ToLowerInvariant()}"
            : $"value={FormatNumericValue(reading.Value)}";

        List<string> tags =
        [
            $"device_id={EscapeTagValue(reading.DeviceId.ToString())}",
            $"device={EscapeTagValue(reading.DeviceName)}",
            $"protocol={EscapeTagValue(reading.ConnectionMethod.ToString())}",
            $"unit_id={reading.UnitId}",
            $"tag_id={EscapeTagValue(reading.TagId.ToString())}",
            $"tag={EscapeTagValue(reading.TagName)}",
            $"area={EscapeTagValue(reading.Area.ToString())}",
            $"address={reading.Address}",
            $"data_type={EscapeTagValue(reading.DataType.ToString())}"
        ];

        if (!string.IsNullOrWhiteSpace(reading.EngineeringUnit))
        {
            tags.Add($"engineering_unit={EscapeTagValue(reading.EngineeringUnit)}");
        }

        return $"{EscapeMeasurement(_options.Measurement)},{string.Join(',', tags)} {field} {ToNanoseconds(reading.Timestamp)}";
    }

    private static Uri BuildWriteUri(InfluxDbOptions options)
    {
        UriBuilder builder = new(options.Url)
        {
            Path = "/api/v2/write",
            Query = $"org={Uri.EscapeDataString(options.Org)}&bucket={Uri.EscapeDataString(options.Bucket)}&precision=ns"
        };

        return builder.Uri;
    }

    private static string FormatNumericValue(object value)
    {
        double numericValue = Convert.ToDouble(value, CultureInfo.InvariantCulture);
        if (double.IsNaN(numericValue) || double.IsInfinity(numericValue))
        {
            throw new InvalidOperationException($"InfluxDB numeric field value '{numericValue}' is not supported.");
        }

        return numericValue.ToString("R", CultureInfo.InvariantCulture);
    }

    private static string EscapeMeasurement(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace(",", "\\,", StringComparison.Ordinal)
            .Replace(" ", "\\ ", StringComparison.Ordinal);
    }

    private static string EscapeTagValue(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace(",", "\\,", StringComparison.Ordinal)
            .Replace(" ", "\\ ", StringComparison.Ordinal)
            .Replace("=", "\\=", StringComparison.Ordinal);
    }

    private static long ToNanoseconds(DateTimeOffset timestamp)
    {
        return (timestamp.ToUniversalTime().Ticks - DateTimeOffset.UnixEpoch.Ticks) * 100L;
    }
}
