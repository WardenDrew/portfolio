using Amazon.Runtime;
using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using Serilog;
using SESAggregator.Configuration;
using SESAggregator.Models;
using System.Diagnostics;
using System.Net.Mail;
using System.Security.Cryptography.X509Certificates;

namespace SESAggregator.Services;

public class SESDispatchService
{
	private static readonly AWSCredentials credentials = new BasicAWSCredentials(SESSettings.ACCESS_KEY, SESSettings.SECRET_KEY);

	private static readonly AmazonSimpleEmailServiceV2Config config = new()
	{
		
	};

	private readonly AmazonSimpleEmailServiceV2Client client = new(credentials, config);

	public async Task<SendMessageResponse> SendMessage(Data.Entities.Message message, CancellationToken cancellationToken = default)
	{
		SendMessageResponse response = new();
		
		MailAddress fromAddress = new(SESSettings.FROM_ADDRESS, SESSettings.FROM_NAME);
		MailAddress toAddress = new(message.ToAddress, message.ToName);

		SendEmailRequest request = new()
		{
			FromEmailAddress = fromAddress.ToString(),
			Destination = new()
			{
				ToAddresses = new()
				{
					toAddress.ToString()
				}
			},
			Content = new()
			{
				Simple = new()
				{
					Subject = new()
					{ 
						Data = message.Subject,
						Charset = "UTF-8"
					},
					Body = new()
					{
						Html = new()
						{
							Data = message.Body,
							Charset = "UTF-8"
						}
					}
				}
			}
		};

		SendEmailResponse? sesResponse = null;

		try
		{
			sesResponse = await client.SendEmailAsync(request, cancellationToken);

			response.Success = true;
			return response;
		}
		catch (Exception ex)
		{
			if (ex is AmazonServiceException amazonEx)
			{
				response.FailureMessage = amazonEx.Message;
				response.Retryable = amazonEx.Retryable is not null;
				response.Backoff = amazonEx.Retryable?.Throttling == true;

				if (response.Backoff)
				{
					Log.Logger.Warning("Rate Limited by AWS. Backing off for 10 minutes: {error}", response.FailureMessage);
				}
				else if (response.Retryable)
				{
					Log.Logger.Warning("Retryable exception occured when sending message: {error}", response.FailureMessage);
				}
				else
				{
					Log.Logger.Error("Unrecoverable exception occured when sending message: {error}", response.FailureMessage);
				}

				return response;
			}

			response.FailureMessage = ex.Message;

			Log.Logger.Error(ex, "Encountered a non AWS sending Exception: {error}", response.FailureMessage);
		}

		return response;
	}
}
