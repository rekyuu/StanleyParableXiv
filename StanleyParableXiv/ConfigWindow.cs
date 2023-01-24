using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using ImGuiNET;

namespace StanleyParableXiv;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration _configuration;
    private readonly DalamudPluginInterface _pluginInterface;
    private readonly AudioPlayer _audioPlayer;
    
    public ConfigWindow(Plugin plugin) : base("Stanley Parable XIV Configuration")
    {
        Size = new Vector2(256, 128);
        SizeCondition = ImGuiCond.FirstUseEver;
        
        _configuration = plugin.Configuration;
        _pluginInterface = plugin.PluginInterface;
        _audioPlayer = plugin.AudioPlayer;
    }

    public void Dispose() { }

    public override void Draw()
    {
        int bindToXivVolumeSourceState = _configuration.BindToXivVolumeSource ? 1 : 0;
        string[] bindToXivVolumeSourceOptions =
        {
            "Set Volume",
            "Bind to game volume"
        };

        if (ImGui.Combo("##BindToXivVolumeSource", ref bindToXivVolumeSourceState, bindToXivVolumeSourceOptions, 
                bindToXivVolumeSourceOptions.Length))
        {
            _configuration.BindToXivVolumeSource = bindToXivVolumeSourceState == 1;
            _configuration.Save();
            
            _audioPlayer.UpdateVolume();
        }

        if (_configuration.BindToXivVolumeSource)
        {
            XivVolumeSource xivVolumeSource = _configuration.XivVolumeSource;
            int xivVolumeSourceState = (int)xivVolumeSource;
            string[] xivVolumeSourceOptions =
            {
                "BGM",
                "Sound Effects",
                "Voice",
                "System Sounds",
                "Ambient Sounds",
                "Performance"
            };

            if (ImGui.Combo("##XivVolumeSource", ref xivVolumeSourceState, xivVolumeSourceOptions,
                    xivVolumeSourceOptions.Length))
            {
                _configuration.XivVolumeSource = (XivVolumeSource)xivVolumeSourceState;
                _configuration.Save();

                _audioPlayer.UpdateVolume();
            }
            
            int volumeBoostValue = (int)_configuration.XivVolumeSourceBoost;
            
            if (ImGui.SliderInt("Volume Boost", ref volumeBoostValue, 0, 100))
            {
                _configuration.XivVolumeSourceBoost = (uint)volumeBoostValue;
                _configuration.Save();

                _audioPlayer.UpdateVolume();
            }
        }
        else
        {
            int volumeValue = (int)_configuration.Volume;
            
            if (ImGui.SliderInt("Volume", ref volumeValue, 0, 100))
            {
                _configuration.Volume = (uint)volumeValue;
                _configuration.Save();

                _audioPlayer.UpdateVolume();
            }
        }

        if (ImGui.Button("Test"))
        {
            _audioPlayer.PlayRandomSoundFromCategory(AudioEvent.Afk);
        }
    }
}
