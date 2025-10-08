using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Text;

namespace FreePbxTools.Common;

public class AmiClient : IAsyncDisposable
{
	private readonly TcpClient client = new();
	private NetworkStream? stream = null;

	public bool Authenticated { get; private set; } = false;

	public async Task ConnectAsync(
		string host, 
		ushort port,
		CancellationToken cancellationToken = default)
	{
		await this.client.ConnectAsync(host, port, cancellationToken);
		this.ThrowIfNotConnected();
		this.stream = this.client.GetStream();
		this.ThrowIfNoStream();
	}

	private void ThrowIfNotConnected()
	{
		if (!this.client.Connected)
		{
			throw new InvalidOperationException("Failed to connect");
		}
	}
	
	[MemberNotNull(nameof(stream))]
	private void ThrowIfNoStream()
	{
		if (stream is null)
		{
			throw new InvalidOperationException("Stream was not initialized");
		}
		
		if (!stream.CanRead)
		{
			throw new InvalidOperationException("Cannot read from stream");
		}
		
		if (!stream.CanWrite)
		{
			throw new InvalidOperationException("Cannot write to stream");
		}
	}

	public async Task<string> SendMessageAsync(
		Dictionary<string, string> fields,
		CancellationToken cancellationToken = default)
	{
		ThrowIfNoStream();
		
		string idempotency = Guid.NewGuid().ToString("N");
		fields["ActionID"] = idempotency;
		
		string message = String.Join(
			"\r\n",
			fields.Select(x => 
				$"{x.Key}: {x.Value}"));
		message += "\r\n\r\n"; // End the message with a double NT style newline
		byte[] messageBytes = Encoding.UTF8.GetBytes(message);
		
		await stream.WriteAsync(messageBytes, cancellationToken);

		return idempotency;
	}

	public async Task<Dictionary<string,string>> ReadMessageAsync(
		CancellationToken cancellationToken = default)
	{
		ThrowIfNoStream();
		
		using StreamReader reader = new(
			this.stream, 
			encoding: Encoding.UTF8, 
			leaveOpen: true);
		Dictionary<string, string> fields = new();
		bool endOfMessage = false;
		while (!endOfMessage)
		{
			string? line = await reader.ReadLineAsync(cancellationToken);
			
			if (string.IsNullOrWhiteSpace(line))
			{
				endOfMessage = true;
				continue;
			}

			string[] parts = line.Split(':', 2);
			if (parts.Length != 2)
			{
				// Log later
				continue;
			}
			
			fields.Add(parts[0].Trim(), parts[1].Trim());
		}

		return fields;
	}

	public async Task<Dictionary<string, string>> SendMessageReadResponseAsync(
		Dictionary<string, string> fields,
		CancellationToken cancellationToken = default)
	{
		string idempotency 
			= await this.SendMessageAsync(fields, cancellationToken);
		
		Dictionary<string,string> resultFields 
			= await ReadMessageAsync(cancellationToken);
		
		if (!resultFields.TryGetValue("ActionID", out string? actionId) ||
		    !actionId.Equals(idempotency, StringComparison.OrdinalIgnoreCase))
		{
			throw new InvalidOperationException($"Idempotency mismatch");
		}

		return resultFields;
	}

	public async Task<Dictionary<string, string>> SendMessageValidateResponseAsync(
		Dictionary<string, string> fields,
		string expectedResponse = "Success",
		CancellationToken cancellationToken = default)
	{
		Dictionary<string,string> resultFields
			= await SendMessageReadResponseAsync(fields, cancellationToken);
		
		if (!resultFields.TryGetValue("Response", out string? response) ||
		    !response.Equals(expectedResponse, StringComparison.OrdinalIgnoreCase))
		{
			_ = resultFields.TryGetValue("Message", out string? message);
			throw new InvalidOperationException($"{response}: {message}");
		}
		
		return resultFields;
	}

	public async Task LoginAsync(
		string username,
		string secret,
		CancellationToken cancellationToken = default)
	{
		await SendMessageValidateResponseAsync(new()
		{
			["Action"] = "Login",
			["Username"] = username,
			["AuthType"] = "plain",
			["Secret"] = secret,
			["Events"] = "off",
		}, cancellationToken: cancellationToken);
		
		this.Authenticated = true;
	}

	public async Task LogoffAsync(
		CancellationToken cancellationToken = default)
	{
		_ = await SendMessageAsync(new()
		{
			["Action"] = "Logoff",
		}, cancellationToken);
		
		this.Authenticated = false;
	}

	public async ValueTask DisposeAsync()
	{
		if (this.Authenticated)
		{
			await LogoffAsync();
		}
		
		if (this.stream != null)
		{
			await this.stream.DisposeAsync();
		}
		
		this.client.Dispose();
	}
}

/*public bool Originate()
{
	using Ami;
	var client = new AmiClient();

	var socket = new TcpClient(hostname: "10.0.0.3", port: 5038);

	client.Stopped += (s, e) =>
	{
		if(e.Exception != null)
		{
			Console.Error.WriteLine($"Exception: {e.Exception}");
		}
	};

	using(socket)
	using(client)
	{
		await client.Start(socket.GetStream());

		if(!await client.Login(username: "belldispatch", secret: "22ff59cd48783e96d950a65ecfecb089a7e893d2", md5: true))
		{
			Console.Out.WriteLine("Login failed");
			return;
		}

		var response = await client.Publish(new AmiMessage
		{
			{ "Action", "Originate" },
			{ "Channel", "Local/599@from-internal"},
			{ "Application", "Hangup" },
			{ "CallerID", "Bell <599>" }
		});

		if(response["Response"] != "Success")
		{
			Console.WriteLine("Failure to send!");
		}
	}
}*/