using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FreePbxTools.Web.Helpers;

namespace FreePbxTools.Web.Services;

public class SettingsService(ILogger<SettingsService> logger)
{
	private const string baseDirectory = "data";
	private const string fileName = "settings.json";
	
	private static readonly JsonSerializerOptions jsonOptions = new()
	{
		WriteIndented = true,
	};
	
	public SettingsModel Running { get; set; } = new();
	public SettingsModel Staged { get; set; } = new();

	public bool IsDirty { get; private set; } = false;
	public event Action? DirtyStateChanged;

	public bool CheckDirty()
	{
		string runningHash = Md5Hash(JsonSerializer.Serialize(Running, jsonOptions));
		string stagedHash = Md5Hash(JsonSerializer.Serialize(Staged, jsonOptions));

		bool newDirty = runningHash != stagedHash;

		// ReSharper disable once InvertIf
		if (IsDirty != newDirty)
		{
			IsDirty = newDirty;
			DirtyStateChanged?.Invoke();
		}
		
		return IsDirty;
	}
	
	public async Task Save(CancellationToken cancellationToken = default)
	{
		string fullPath = Path.Combine(baseDirectory, fileName);
		
		if (!Directory.Exists(baseDirectory))
		{
			Directory.CreateDirectory(baseDirectory);
			logger.LogInformation($"Created {baseDirectory} directory");
		}
		
		string data = JsonSerializer.Serialize(Running, jsonOptions);
		await File.WriteAllTextAsync(
			fullPath, 
			data, 
			cancellationToken);
		
		logger.LogInformation($"Saved Running Settings to {fullPath}");
		CheckDirty();
	}

	public async Task Load(CancellationToken cancellationToken = default)
	{
		string fullPath = Path.Combine(baseDirectory, fileName);

		if (!File.Exists(fullPath))
		{
			logger.LogInformation($"No saved settings found, using defaults!");
			return;
		}

		string data = await File.ReadAllTextAsync(fullPath, cancellationToken);
		SettingsModel? loadedSettings = JsonSerializer.Deserialize<SettingsModel>(data, jsonOptions);
		if (loadedSettings is null)
		{
			logger.LogWarning($"Saved Settings are malformed, using defaults!");
			return;
		}

		Running = loadedSettings;
		logger.LogInformation($"Loaded Running Settings from {fullPath}");

		Staged = Deep.Copy(Running);
		logger.LogInformation($"Copied Running Settings to Staged Settings");
		CheckDirty();
	}

	public async Task ApplyStagedAsync(CancellationToken cancellationToken = default)
	{
		Running = Deep.Copy(Staged);
		logger.LogInformation($"Copied Staged Settings to Running Settings");
		
		await Save(cancellationToken);
		CheckDirty();
	}

	private static string Md5Hash(string input)
	{
		return string.Concat(
			MD5.Create()
				.ComputeHash(
					Encoding.UTF8.GetBytes(input))
				.Select(x => x.ToString("x2")));
	}
	
	public class SettingsModel
    {
    	public string Password { get; set; } = "changeme";
    	public string AsteriskHost { get; set; } = "127.0.0.1";
    	public ushort AsteriskAmiPort { get; set; } = 5038;
    	public string AsteriskAmiUsername { get; set; } = "amiuser";
    	public string AsteriskAmiSecret { get; set; } = "amisecret";
    	
    	public List<PageGroupModel> PageGroups { get; set; } = new();
    	public List<ScheduleModel> Schedules { get; set; } = new();
	    public PlanModel Plan { get; set; } = new();
	    public List<OverrideModel> Overrides { get; set; } = new();

	    public string ToDisplayString()
	    {
		    SettingsModel copy = Deep.Copy(this);
		    copy.Password = $"[HASH: {Md5Hash(copy.Password)}]";
		    copy.AsteriskAmiSecret = $"[HASH: {Md5Hash(copy.AsteriskAmiSecret)}]";
		    return JsonSerializer.Serialize(copy, jsonOptions);
	    }
    }
    
    public class PageGroupModel
    {
    	public required string Extension { get; set; }
    	public required string Name { get; set; }
    }
    
    public class ScheduleModel
    {
    	public required Guid Id { get; set; }
    	public string? Name { get; set; }
    	public List<EventModel> Events { get; set; } = new();
    }
    
    public class EventModel
    {
	    public TimeOnly? Time { get; set; }
	    public List<string> PageGroups { get; set; } = new();
    }
    
    public class PlanModel
    {
    	public Guid? Monday { get; set; } = null;
    	public Guid? Tuesday { get; set; } = null;
    	public Guid? Wednesday { get; set; } = null;
    	public Guid? Thursday { get; set; } = null;
    	public Guid? Friday { get; set; } = null;
    	public Guid? Saturday { get; set; } = null;
    	public Guid? Sunday { get; set; } = null;
    }
    
    public class OverrideModel
    {
    	public required Guid Id { get; set; }
	    public DateOnly? Date { get; set; }
    	
	    public string? Reason { get; set; }
	    public Guid? Schedule { get; set; }
    }
}