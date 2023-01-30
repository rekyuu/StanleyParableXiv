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
    private const string RequiredAssetsVersion = "1.2.1.0";

    /// <summary>
    /// Checks if the assets exist, are the current version, and downloads them if necessary.
    /// </summary>
    public static void UpdateVoiceLines()
    {
        bool updateNeeded = false;
        
        // Check if assets are already downloaded and are the current version
        string configDir = DalamudService.PluginInterface.GetPluginConfigDirectory();
        string assetsDir = $"{configDir}/assets";
        string manifestFile = $"{assetsDir}/manifest.json";
        
        if (!File.Exists(manifestFile)) updateNeeded = true;
        
        AssetsManifest? manifest = JsonConvert.DeserializeObject<AssetsManifest>(manifestFile);
        if (manifest?.Version != RequiredAssetsVersion) updateNeeded = true;

        if (!updateNeeded) return;

        // Download assets
        string downloadLocation = $"{configDir}/assets-{RequiredAssetsVersion}.zip";
        Uri assetUri = new($"https://github.com/rekyuu/StanleyParableXiv/releases/download/{RequiredAssetsVersion}/assets.zip");
        
        HttpClient httpClient = new();
        HttpResponseMessage response = httpClient.GetAsync(assetUri).Result;
        
        if (!response.IsSuccessStatusCode)
        {
            PluginLog.Error("Unable to download assets: {ResponseStatusCode} - {ResponseContent}", response.StatusCode, response.Content);
            return;
        }
        
        using (FileStream fs = new(downloadLocation, FileMode.CreateNew))
        {
            response.Content.CopyToAsync(fs).Wait();
        }
        
        // Extract assets
        ZipFile.ExtractToDirectory(downloadLocation, assetsDir);
        File.Delete(downloadLocation);
        
        // Validate the downloaded assets
        manifest = JsonConvert.DeserializeObject<AssetsManifest>(manifestFile);
        if (manifest?.Version != RequiredAssetsVersion) PluginLog.Error("Downloaded assets do not match the requested version. Requested = {RequestedVersion}, Downloaded = {DownloadedVersion}", 
            RequiredAssetsVersion, manifest?.Version!);
    }
}

public class AssetsManifest
{
    public string Version => "0.0.0.0";
}