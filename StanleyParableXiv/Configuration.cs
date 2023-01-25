using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using StanleyParableXiv.Services;
using StanleyParableXiv.Utility;

namespace StanleyParableXiv;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public static Configuration Instance { get; } = DalamudService.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
    
    public int Version { get; set; } = 0;

    public uint Volume { get; set; } = 50;

    public bool BindToXivVolumeSource = true;

    public XivVolumeSource XivVolumeSource = XivVolumeSource.Voice;

    public uint XivVolumeSourceBoost = 100;

    public void Save()
    {
        DalamudService.PluginInterface.SavePluginConfig(this);
    }
}