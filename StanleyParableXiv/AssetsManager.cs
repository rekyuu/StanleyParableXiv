using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using Dalamud.Interface.Internal.Notifications;
using Newtonsoft.Json;
using StanleyParableXiv.Services;

namespace StanleyParableXiv;

public static class AssetsManager
{
    public static bool IsUpdating { get; private set; } = false;

    public static bool HasEnoughFreeDiskSpace { get; private set; } = true;

    public static string? CurrentAssetsVersion { get; private set; }
    
    public const string RequiredAssetsVersion = "1.2.2.0";
    private const long RequiredDiskSpaceBytes = 100_000_000; // Expanded bytes

    /// <summary>
    /// Checks if the assets exist, are the current version, and downloads them if necessary.
    /// </summary>
    public static void UpdateVoiceLines(bool force = false)
    {
        DalamudService.Log.Information("Validating assets");
        
        bool updateNeeded = force;
        
        // Check if assets are already downloaded and are the current version
        string configDir = DalamudService.PluginInterface.GetPluginConfigDirectory();
        string assetsDir = $"{configDir}/assets";

        CurrentAssetsVersion = CurrentDownloadedAssetVersion();
        if (CurrentAssetsVersion != RequiredAssetsVersion) updateNeeded = true;

        if (!updateNeeded)
        {
            DalamudService.Log.Information("Assets validated, nothing to do");
            return;
        }

        IsUpdating = true;
        LogAndNotify("Downloading assets", NotificationType.Info);
        
        // Clear folder if it exists
        Directory.Delete(assetsDir, true);

        // Download assets
        string downloadLocation = $"{configDir}/assets-{RequiredAssetsVersion}.zip";
        Uri assetUri = new($"https://github.com/rekyuu/StanleyParableXiv/releases/download/{RequiredAssetsVersion}/assets.zip");

        HasEnoughFreeDiskSpace = true;
        long freeDiskSpace = new DriveInfo(downloadLocation).AvailableFreeSpace;
        if (freeDiskSpace < RequiredDiskSpaceBytes)
        {
            LogAndNotify("Not enough free disk space to extract assets", NotificationType.Error);
            
            HasEnoughFreeDiskSpace = false;
            IsUpdating = false;
            return;
        }
        
        if (File.Exists(downloadLocation)) File.Delete(downloadLocation);
        
        HttpClient httpClient = new();
        HttpResponseMessage response = httpClient.GetAsync(assetUri).Result;
        
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Unable to download assets: {response.StatusCode} - {response.Content}");
        }
        
        using (FileStream fs = new(downloadLocation, FileMode.CreateNew))
        {
            response.Content.CopyToAsync(fs).Wait();
        }
        
        // Extract assets
        LogAndNotify("Extracting assets", NotificationType.Info);
        
        ZipFile.ExtractToDirectory(downloadLocation, assetsDir);
        File.Delete(downloadLocation);
        
        LogAndNotify("Asset extraction complete", NotificationType.Success);
        IsUpdating = false;
        
        // Validate the downloaded assets
        CurrentAssetsVersion = CurrentDownloadedAssetVersion();
        if (CurrentAssetsVersion != RequiredAssetsVersion)
        {
            throw new Exception($"Downloaded assets do not match the requested version. Requested = {RequiredAssetsVersion}, Downloaded = {CurrentAssetsVersion}");
        }
    }

    public static string? CurrentDownloadedAssetVersion()
    {
        string configDir = DalamudService.PluginInterface.GetPluginConfigDirectory();
        string assetsDir = $"{configDir}/assets";
        string manifestFile = $"{assetsDir}/manifest.json";

        if (!File.Exists(manifestFile)) return null;
        
        string jsonData = File.ReadAllText(manifestFile);
        AssetsManifest? manifest = JsonConvert.DeserializeObject<AssetsManifest>(jsonData);

        return manifest?.Version;
    }

    private static void LogAndNotify(string message, NotificationType type)
    {
        switch (type)
        {
            case NotificationType.Success:
            case NotificationType.Info:
                DalamudService.Log.Info(message);
                break;
            case NotificationType.Warning:
                DalamudService.Log.Warning(message);
                break;
            case NotificationType.Error:
                DalamudService.Log.Error(message);
                break;
            default:
            case NotificationType.None:
                DalamudService.Log.Debug(message);
                break;
        }
        
        DalamudService.PluginInterface.UiBuilder.AddNotification(message, "StanleyParableXiv", type);
    }
}

public class AssetsManifest
{
    [JsonProperty("version")]
    public string? Version { get; set; }
}