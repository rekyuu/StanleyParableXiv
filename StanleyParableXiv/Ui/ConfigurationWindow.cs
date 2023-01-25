using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using StanleyParableXiv.Services;
using StanleyParableXiv.Utility;

namespace StanleyParableXiv.Ui;

public class ConfigurationWindow : Window, IDisposable
{
    public static string Name => "Stanley Parable XIV Configuration";
    
    public ConfigurationWindow() : base(Name)
    {
        Size = new Vector2(256, 128);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public void Dispose() { }

    public override void Draw()
    {
        int bindToXivVolumeSourceState = Configuration.Instance.BindToXivVolumeSource ? 1 : 0;
        string[] bindToXivVolumeSourceOptions =
        {
            "Set Volume",
            "Bind to game volume"
        };

        if (ImGui.Combo("##BindToXivVolumeSource", ref bindToXivVolumeSourceState, bindToXivVolumeSourceOptions, 
                bindToXivVolumeSourceOptions.Length))
        {
            Configuration.Instance.BindToXivVolumeSource = bindToXivVolumeSourceState == 1;
            Configuration.Instance.Save();
            
            AudioPlayer.Instance.UpdateVolume();
        }

        if (Configuration.Instance.BindToXivVolumeSource)
        {
            XivVolumeSource xivVolumeSource = Configuration.Instance.XivVolumeSource;
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
                Configuration.Instance.XivVolumeSource = (XivVolumeSource)xivVolumeSourceState;
                Configuration.Instance.Save();

                AudioPlayer.Instance.UpdateVolume();
            }
            
            int volumeBoostValue = (int)Configuration.Instance.XivVolumeSourceBoost;
            
            if (ImGui.SliderInt("Volume Boost", ref volumeBoostValue, 0, 100))
            {
                Configuration.Instance.XivVolumeSourceBoost = (uint)volumeBoostValue;
                Configuration.Instance.Save();

                AudioPlayer.Instance.UpdateVolume();
            }
        }
        else
        {
            int volumeValue = (int)Configuration.Instance.Volume;
            
            if (ImGui.SliderInt("Volume", ref volumeValue, 0, 100))
            {
                Configuration.Instance.Volume = (uint)volumeValue;
                Configuration.Instance.Save();

                AudioPlayer.Instance.UpdateVolume();
            }
        }

        if (ImGui.Button("Test"))
        {
            AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.Afk);
        }
    }
}
