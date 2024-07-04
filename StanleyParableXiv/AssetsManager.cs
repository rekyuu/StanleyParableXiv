using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using Dalamud.Interface.ImGuiNotification;
using Newtonsoft.Json;
using StanleyParableXiv.Services;

namespace StanleyParableXiv;

public enum AssetsFileType
{
    Mp3,
    Ogg
}

public static class AssetsManager
{
    
    public const string RequiredAssetsVersion = "2.1.0.0";
    
    public static bool IsUpdating { get; private set; } = false;

    public static bool HasEnoughFreeDiskSpace { get; private set; } = true;

    public static string? CurrentAssetsVersion { get; private set; }
    
    private const long RequiredDiskSpaceMp3Compressed = 40_101_040;
    private const long RequiredDiskSpaceMp3Extracted = 44_343_296;
    private const long RequiredDiskSpaceOggCompressed = 22_699_321;
    private const long RequiredDiskSpaceOggExtracted = 24_547_328;

    /// <summary>
    /// Checks if the assets exist, are the current version, and downloads them if necessary.
    /// </summary>
    public static void UpdateVoiceLines(bool force = false)
    {
        DalamudService.Log.Information("Validating assets");
        
        // MIGRATION: delete old assets folder if it exists
        string oldAssetsDir = $"{DalamudService.PluginInterface.GetPluginConfigDirectory()}/assets";
        if (Directory.Exists(oldAssetsDir)) Directory.Delete(oldAssetsDir, true);
        
        bool updateNeeded = force;
        
        // Check if assets are already downloaded and are the current version
        string configDir = DalamudService.PluginInterface.GetPluginConfigDirectory();
        string assetsDir = GetAssetsDirectory();
        string assetsType = Configuration.Instance.AssetsFileType switch
        {
            AssetsFileType.Mp3 => "mp3",
            AssetsFileType.Ogg => "ogg",
            _ => throw new ArgumentOutOfRangeException()
        };

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
        if (Directory.Exists(assetsDir)) Directory.Delete(assetsDir, true);

        // Download assets
        string downloadLocation = $"{configDir}/assets-{RequiredAssetsVersion}-{assetsType}.zip";
        Uri assetUri = new($"https://github.com/rekyuu/StanleyParableXiv/releases/download/{RequiredAssetsVersion}/assets-{assetsType}.zip");

        HasEnoughFreeDiskSpace = true;
        long freeDiskSpace = new DriveInfo(downloadLocation).AvailableFreeSpace;
        if (freeDiskSpace < GetRequiredDiskSpace())
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
            LogAndNotify($"Unable to download assets: {response.StatusCode} - {response.Content}", NotificationType.Error);
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
        if (CurrentAssetsVersion == RequiredAssetsVersion) return;
        
        DalamudService.Log.Error("Downloaded assets do not match the requested version. Requested = {RequiredAssetsVersion}, Downloaded = {CurrentAssetsVersion}", RequiredAssetsVersion, CurrentAssetsVersion ?? "null");
    }

    public static long GetRequiredDiskSpace()
    {
        return Configuration.Instance.AssetsFileType switch
        {
            AssetsFileType.Mp3 => RequiredDiskSpaceMp3Compressed + RequiredDiskSpaceMp3Extracted,
            AssetsFileType.Ogg => RequiredDiskSpaceOggCompressed + RequiredDiskSpaceOggExtracted,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public static string GetAssetsDirectory()
    {
        string configDir = DalamudService.PluginInterface.GetPluginConfigDirectory();
        string baseAssetsDir = $"{configDir}/assets";
        
        return Configuration.Instance.AssetsFileType switch
        {
            AssetsFileType.Mp3 => $"{baseAssetsDir}-mp3",
            AssetsFileType.Ogg => $"{baseAssetsDir}-ogg",
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private static string? CurrentDownloadedAssetVersion()
    {
        string assetsDir = GetAssetsDirectory();
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

        Notification notification = new()
        {
            Content = message,
            Type = type
        };
        
        DalamudService.NotificationManager.AddNotification(notification);
    }
}

public class AssetsManifest
{
    [JsonProperty("version")]
    public string? Version { get; set; }
}