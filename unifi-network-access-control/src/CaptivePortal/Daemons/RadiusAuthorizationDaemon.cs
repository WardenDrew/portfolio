
using System.Net.Sockets;
using System.Net;
using System.Text;
using Radius;
using Radius.RadiusAttributes;
using CaptivePortal.Database;
using Microsoft.EntityFrameworkCore;
using CaptivePortal.Database.Entities;
using static System.Formats.Asn1.AsnWriter;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using CaptivePortal.Services.Outer;

namespace CaptivePortal.Daemons
{
    public class RadiusAuthorizationDaemon(
        IronNacConfiguration configuration,
        ILogger<RadiusAuthorizationDaemon> logger,
        RadiusAttributeParserService parser,
        IDbContextFactory<IronNacDbContext> dbFactory,
        DataRefreshNotificationService dataRefresh)
        : BaseDaemon<RadiusAuthorizationDaemon>(logger)
    {
        protected override async Task EntryPoint(CancellationToken cancellationToken)
        {
            UdpClient? udpClient = null;
            byte[] secret;

            try
            {
                try
                {
                    secret = Encoding.ASCII.GetBytes(configuration.RadiusAuthorizationSecret);

                    udpClient = new(new IPEndPoint(
                        IPAddress.Parse(configuration.RadiusAuthorizationListenAddress), 
                        configuration.RadiusAuthorizationPort));
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
                        RadiusPacket response;

                        using IronNacDbContext db = await dbFactory.CreateDbContextAsync(cancellationToken);

                        switch (incoming.Code)
                        {
                            case RadiusCode.ACCESS_REQUEST:

                                string? mac = incoming.GetAttribute<UserNameAttribute>()?.Value;
                                if (mac is null)
                                {
                                    await udpClient.SendAsync(RadiusPacket
                                        .Create(RadiusCode.ACCESS_REJECT, incoming.Identifier)
                                        .AddMessageAuthenticator(secret)
                                        .AddResponseAuthenticator(secret, incoming.Authenticator)
                                        .ToBytes(),
                                        udpReceiveResult.RemoteEndPoint,
                                        cancellationToken);
                                    break;
                                }

                                Device? device = await db.Devices
                                    .Include(x => x.DeviceNetwork)
                                        .ThenInclude(x => x!.Network)
                                    .Where(x => x.DeviceMac == mac)
                                    .FirstOrDefaultAsync(cancellationToken);

                                if (device is null ||
                                    !device.Authorized ||
                                    device.AuthorizedUntil <= DateTime.UtcNow)
                                {
                                    // New Device, Not Authorized or missing a network assignment so go to registration
                                    
                                    // Get Registration Network with spare capacity
                                    Network? registrationNetwork = await db.Networks
                                        .Where(x => x.NetworkGroup.Registration)
                                        .Where(x => x.DeviceNetworks.Count < x.Capacity)
                                        .FirstOrDefaultAsync(cancellationToken);
                                    if (registrationNetwork is null)
                                    {
                                        // Registration network not configured yet!
                                        response = RadiusPacket
                                            .Create(RadiusCode.ACCESS_REJECT, incoming.Identifier);
                                    }
                                    else
                                    {
                                        try
                                        {
                                            if (device is null)
                                            {
                                                // New Device
                                                device = new()
                                                {
                                                    DeviceMac = mac,
                                                    DeviceNetwork = new()
                                                    {
                                                        NetworkId = registrationNetwork.Id
                                                    }
                                                };

                                                db.Add(device);
                                                await db.SaveChangesAsync(cancellationToken);
                                            }
                                            else
                                            {
                                                // Authorization Expired
                                                // Kick off current device assignment and give a new one just to be safe
                                                device.DeviceNetwork.Network = registrationNetwork;

                                                db.Update(device);

                                                await db.SaveChangesAsync(cancellationToken);

                                            }

                                            response = RadiusPacket
                                                .Create(RadiusCode.ACCESS_ACCEPT, incoming.Identifier)
                                                .AddAttribute(new TunnelTypeAttribute(0,
                                                    TunnelTypeAttribute.TunnelTypes.VLAN))
                                                .AddAttribute(new TunnelMediumTypeAttribute(0,
                                                    TunnelMediumTypeAttribute.Values.IEEE_802))
                                                .AddAttribute(new TunnelPrivateGroupIdAttribute(0,
                                                    registrationNetwork.Vlan.ToString()));
                                        }
                                        catch (DbUpdateConcurrencyException)
                                        {
                                            response = RadiusPacket
                                                .Create(RadiusCode.ACCESS_REJECT, incoming.Identifier);
                                        }
                                    }
                                }
                                else
                                {
                                    // Authorized and has a network assignment

                                    response = RadiusPacket
                                        .Create(RadiusCode.ACCESS_ACCEPT, incoming.Identifier)
                                        .AddAttribute(new TunnelTypeAttribute(0,
                                            TunnelTypeAttribute.TunnelTypes.VLAN))
                                        .AddAttribute(new TunnelMediumTypeAttribute(0,
                                            TunnelMediumTypeAttribute.Values.IEEE_802))
                                        .AddAttribute(new TunnelPrivateGroupIdAttribute(0,
                                            device.DeviceNetwork.Network.Vlan.ToString()));
                                }

                                response = response
                                        .AddMessageAuthenticator(secret)
                                        .AddResponseAuthenticator(secret, incoming.Authenticator);

                                await udpClient.SendAsync(
                                    response.ToBytes(),
                                    udpReceiveResult.RemoteEndPoint,
                                    cancellationToken);

                                dataRefresh.DeviceDetailsNotify();
                                dataRefresh.NetworkUsageNotify();

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
