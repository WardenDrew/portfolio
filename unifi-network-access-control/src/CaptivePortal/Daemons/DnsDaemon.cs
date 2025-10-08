
using System.Net.Sockets;
using System.Net;
using System.Text;
using Radius;
using Radius.RadiusAttributes;
using DNS.ResourceRecords;
using DNS;
using CaptivePortal.Services.Outer;

namespace CaptivePortal.Daemons
{
    public class DnsDaemon(
        IronNacConfiguration configuration, 
        ILogger<DnsDaemon> logger)
        : BaseDaemon<DnsDaemon>(logger)
    {
        protected override async Task EntryPoint(CancellationToken cancellationToken)
        {
            IPAddress redirectAddress;
            UdpClient? udpClient = null;
            
            // udpClient disposal
            try
            {
                // Thread startup error checking
                try
                {
                    redirectAddress = IPAddress.Parse(configuration.DnsRedirectAddress);

                    udpClient = new(new IPEndPoint(IPAddress.Parse(configuration.DnsListenAddress), configuration.DnsPort));
                }
                catch (Exception ex)
                {
                    Logger.LogCritical(ex, "Critical error when starting the {listener}!", nameof(DnsDaemon));
                    return;
                }

                Logger.LogInformation("{listener} started", nameof(DnsDaemon));
                this.Running = true;

                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        UdpReceiveResult udpReceiveResult = await udpClient.ReceiveAsync(cancellationToken);
                        if (cancellationToken.IsCancellationRequested) break;

                        DnsPacket request = DnsPacket.FromBytes(udpReceiveResult.Buffer);

                        DnsPacket response = new DnsPacket();
                        response.TransactionId = request.TransactionId;
                        response.Flags.IsResponse = true;
                        response.Flags.IsRecursionDesired = request.Flags.IsRecursionDesired;
                        response.Questions = request.Questions;

                        if (request.NumQuestions != 1 ||
                            request.Questions[0].Type != DnsResourceRecordTypes.A)
                        {
                            response.Flags.ReplyCode = DnsPacketFlagsReplyCodes.REFUSED;
                        }
                        else
                        {
                            response.Flags.ReplyCode = DnsPacketFlagsReplyCodes.NO_ERROR;
                            response.Answers.Add(new ARecord(request.Questions[0].Name, redirectAddress, 60));
                        }

                        await udpClient.SendAsync(response.ToBytes(), udpReceiveResult.RemoteEndPoint, cancellationToken);
                    }
                    catch (SocketException ex)
                    {
                        Logger.LogError(ex, "Socket Exception!");
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
