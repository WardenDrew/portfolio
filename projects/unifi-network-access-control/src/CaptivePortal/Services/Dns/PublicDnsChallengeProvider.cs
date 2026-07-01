using Amazon.Route53;
using Amazon.Route53.Model;
using Amazon.Runtime;
using CaptivePortal.Services.Outer;
using LettuceEncrypt.Acme;
using System.Diagnostics;

namespace CaptivePortal.Services.Dns
{
    public class PublicDnsChallengeProvider : IDnsChallengeProvider
    {
        private readonly AmazonRoute53Client? route53;
        private readonly string? hostedZoneId;
        private readonly ILogger logger;

        public bool Ready { get; private set; }

        public PublicDnsChallengeProvider(IronNacConfiguration configuration, ILogger<PublicDnsChallengeProvider> logger)
        {
            this.logger = logger;

            if (string.IsNullOrWhiteSpace(configuration.AwsAccessToken) ||
                string.IsNullOrWhiteSpace(configuration.AwsSecretKey) ||
                string.IsNullOrWhiteSpace(configuration.AwsRegion) ||
                string.IsNullOrWhiteSpace(configuration.AwsHostedZoneId))
            {
                Ready = false;
                return;
            }

            hostedZoneId = configuration.AwsHostedZoneId;

            route53 = new(
                configuration.AwsAccessToken,
                configuration.AwsSecretKey,
                Amazon.RegionEndpoint.GetBySystemName(configuration.AwsRegion));
        }

        public async Task<DnsTxtRecordContext> AddTxtRecordAsync(
            string domainName,
            string txt,
            CancellationToken cancellationToken = default)
        {
            if (route53 is null) throw new InvalidOperationException();
            
            ResourceRecordSet recordSet = new(domainName, RRType.TXT);
            recordSet.ResourceRecords.Add(new ResourceRecord($"\"{txt}\""));
            recordSet.TTL = 300;

            Change change = new(ChangeAction.UPSERT, recordSet);

            logger.LogInformation("Prepared an AWS Route53 UPSERT Change Request for: {domain}", domainName);
            await ExecuteChange(change, cancellationToken);
            logger.LogInformation("UPSERT Completed");

            return new(domainName, txt);
        }

        public async Task RemoveTxtRecordAsync(
            DnsTxtRecordContext context,
            CancellationToken cancellationToken = default)
        {
            if (route53 is null) throw new InvalidOperationException();

            string domainName = context.DomainName;
            if (!domainName.ToLower().StartsWith("_acme-challenge."))
            {
                domainName = $"_acme-challenge.{domainName}";
            }

            ResourceRecordSet recordSet = new(domainName, RRType.TXT);
            recordSet.ResourceRecords.Add(new ResourceRecord($"\"{context.Txt}\""));
            recordSet.TTL = 300;

            Change change = new(ChangeAction.DELETE, recordSet);

            logger.LogInformation("Prepared an AWS Route53 DELETE Change Request for: {domain}", domainName);
            await ExecuteChange(change, cancellationToken);
            logger.LogInformation("DELETE Completed");
        }

        private async Task ExecuteChange(Change change, CancellationToken cancellationToken = default)
        {
            if (route53 is null) throw new InvalidOperationException();

            try
            {
                ChangeBatch batch = new ChangeBatch();
                batch.Changes.Add(change);
                batch.Comment = "Automatic ACME Challenge Resolution by IronNAC";

                ChangeResourceRecordSetsRequest request = new ChangeResourceRecordSetsRequest(hostedZoneId, batch);

                logger.LogInformation("Sending Change Request");
                ChangeResourceRecordSetsResponse response = await route53.ChangeResourceRecordSetsAsync(request, cancellationToken);
                if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
                    throw new ApplicationException("The DNS Record Change Request Failed!");
                logger.LogInformation("Received Change Response. Watching Change Info");

                Stopwatch stopwatch = Stopwatch.StartNew();
                bool inSync = false;
                int timeout = 300;
                while (!inSync &&
                    !cancellationToken.IsCancellationRequested &&
                    stopwatch.Elapsed.TotalSeconds <= timeout)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

                    logger.LogInformation("Checking Change Info");
                    GetChangeResponse changeResponse = await route53.GetChangeAsync(new GetChangeRequest(response.ChangeInfo.Id), cancellationToken);
                    inSync = changeResponse.ChangeInfo.Status == ChangeStatus.INSYNC;

                    if (inSync)
                    {
                        logger.LogInformation("Change is INSYNC");
                    }
                }

                if (!inSync)
                {
                    logger.LogError("Change did not synchronize before timeout!");
                    throw new ApplicationException("The DNS Record Change Request did not synchronize before the timeout elapsed!");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Encountered an error while executing DNS changes: {message}", ex.Message);
                throw new ApplicationException("Executing an AWS DNS Change Failed!", ex);
            }
        }
    }
}
