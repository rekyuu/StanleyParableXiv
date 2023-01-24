using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace StanleyParableXiv;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public uint Volume { get; set; } = 50;

    public bool BindToXivVolumeSource = true;

    public XivVolumeSource XivVolumeSource = XivVolumeSource.Voice;

    public uint XivVolumeSourceBoost = 100;

    // the below exist just to make saving less cumbersome
    [NonSerialized]
    private DalamudPluginInterface? _pluginInterface;

    public void Initialize(DalamudPluginInterface pluginInterface)
    {
        _pluginInterface = pluginInterface;
    }

    public void Save()
    {
        _pluginInterface!.SavePluginConfig(this);
    }
}