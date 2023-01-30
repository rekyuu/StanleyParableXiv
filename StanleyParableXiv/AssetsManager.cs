using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using Dalamud.Logging;
using Newtonsoft.Json;
using StanleyParableXiv.Services;

namespace StanleyParableXiv;

public static class AssetsManager
{
    private const string? RequiredAssetsVersion = "1.2.2.0";

    /// <summary>
    /// Checks if the assets exist, are the current version, and downloads them if necessary.
    /// </summary>
    public static void UpdateVoiceLines()
    {
        PluginLog.Information("Validating assets");
        
        bool updateNeeded = false;
        
        // Check if assets are already downloaded and are the current version
        string configDir = DalamudService.PluginInterface.GetPluginConfigDirectory();
        string assetsDir = $"{configDir}/assets";
        string manifestFile = $"{assetsDir}/manifest.json";

        AssetsManifest? manifest;
        if (File.Exists(manifestFile))
        {
            string jsonData = File.ReadAllText(manifestFile);
            manifest = JsonConvert.DeserializeObject<AssetsManifest>(jsonData);
            
            if (manifest?.Version != RequiredAssetsVersion)
            {
                updateNeeded = true;
                Directory.Delete(assetsDir, true);
            }
        }
        else updateNeeded = true;

        if (!updateNeeded) return;
        
        PluginLog.Information("Downloading assets");

        // Download assets
        string downloadLocation = $"{configDir}/assets-{RequiredAssetsVersion}.zip";
        Uri assetUri = new($"https://github.com/rekyuu/StanleyParableXiv/releases/download/{RequiredAssetsVersion}/assets.zip");
        
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
        PluginLog.Information("Extracting assets");
        
        ZipFile.ExtractToDirectory(downloadLocation, assetsDir);
        File.Delete(downloadLocation);
        
        PluginLog.Debug("Asset extraction complete");
        
        // Validate the downloaded assets
        if (File.Exists(manifestFile))
        {
            string jsonData = File.ReadAllText(manifestFile);
            manifest = JsonConvert.DeserializeObject<AssetsManifest>(jsonData);

            if (manifest?.Version != RequiredAssetsVersion)
            {
                throw new Exception($"Downloaded assets do not match the requested version. Requested = {RequiredAssetsVersion}, Downloaded = {manifest?.Version}");
            }
        }
        else
        {
            throw new Exception("Manifest file does not exist from downloaded assets");
        }
    }
}

public class AssetsManifest
{
    [JsonProperty("version")]
    public string? Version { get; set; }
}