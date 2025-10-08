
using System.Net.Sockets;
using System.Net;
using System.Text;
using Radius;
using Radius.RadiusAttributes;
using CaptivePortal.Database;
using CaptivePortal.Database.Entities;
using Microsoft.EntityFrameworkCore;
using CaptivePortal.Services.Outer;

namespace CaptivePortal.Daemons
{
    public class RadiusAccountingDaemon(
        IronNacConfiguration configuration,
        ILogger<RadiusAccountingDaemon> logger,
        RadiusAttributeParserService parser,
        IDbContextFactory<IronNacDbContext> dbFactory,
        DataRefreshNotificationService dataRefresh) 
        : BaseDaemon<RadiusAccountingDaemon>(logger)
    {
        protected override async Task EntryPoint(CancellationToken cancellationToken)
        {
            UdpClient? udpClient = null;
            byte[] secret;

            try
            {
                try
                {
                    secret = Encoding.ASCII.GetBytes(configuration.RadiusAccountingSecret);

                    udpClient = new(new IPEndPoint(
                        IPAddress.Parse(configuration.RadiusAccountingListenAddress), 
                        configuration.RadiusAccountingPort));
                }
                catch (Exception ex)
                {
                    Logger.LogCritical(ex, "Critical error when starting the {listener}!", nameof(RadiusAuthorizationDaemon));
                    return;
                }

                Logger.LogInformation("{listener} started", nameof(RadiusAuthorizationDaemon));
                this.Running = true;

                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        UdpReceiveResult udpReceiveResult = await udpClient.ReceiveAsync(cancellationToken);
                        if (cancellationToken.IsCancellationRequested) break;

                        RadiusPacket? incoming = null;
                        try
                        {
                            incoming = RadiusPacket.FromBytes(udpReceiveResult.Buffer, parser.Parser);
                        }
                        catch (RadiusException radEx)
                        {
                            Console.WriteLine(radEx.Message);
                            continue;
                        }

                        using IronNacDbContext db = await dbFactory.CreateDbContextAsync(cancellationToken);

                        switch (incoming.Code)
                        {
                            case RadiusCode.ACCOUNTING_REQUEST:

                                // We got an accounting message, acknowledge it
                                await udpClient.SendAsync(RadiusPacket
                                    .Create(RadiusCode.ACCOUNTING_RESPONSE, incoming.Identifier)
                                    .AddMessageAuthenticator(secret)
                                    .AddResponseAuthenticator(secret, incoming.Authenticator)
                                    .ToBytes(),
                                    udpReceiveResult.RemoteEndPoint,
                                    cancellationToken);

                                string? mac = incoming.GetAttribute<UserNameAttribute>()?.Value;
                                if (mac is null) break;

                                AccountingStatusTypeAttribute.StatusTypes? statusType = incoming.GetAttribute<AccountingStatusTypeAttribute>()?.StatusType;
                                if (statusType is null) break;

                                Device? device = await db.Devices
                                    .Where(x => x.DeviceMac == mac)
                                    .FirstOrDefaultAsync(cancellationToken);
                                if (device is null) break;

                                if (statusType == AccountingStatusTypeAttribute.StatusTypes.START ||
                                    statusType == AccountingStatusTypeAttribute.StatusTypes.INTERIM_UPDATE)
                                {
                                    device.DetectedDeviceIpAddress = incoming.GetAttribute<FramedIpAddressAttribute>()?.Address.ToString();
                                    device.NasIpAddress = incoming.GetAttribute<NasIpAddressAttribute>()?.Address.ToString();
                                    device.NasIdentifier = incoming.GetAttribute<NasIdentifierAttribute>()?.Value;
                                    device.CallingStationId = incoming.GetAttribute<CallingStationIdAttribute>()?.Value;
                                    device.AccountingSessionId = incoming.GetAttribute<AccountingSessionIdAttribute>()?.Value;
                                }

                                await db.SaveChangesAsync(cancellationToken);

                                dataRefresh.DeviceDetailsNotify();

                                break;

                            default:
                                break;
                        }
                    }
                    catch (SocketException sockEx)
                    {
                        Logger.LogError(sockEx, "Socket Exception!");
                    }
                    catch (OperationCanceledException) { }
                }
            }
            finally
            {
                udpClient?.Dispose();
            }
        }
    }
}
