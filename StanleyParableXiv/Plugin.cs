using System;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Game;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using StanleyParableXiv.Services;
using StanleyParableXiv.Ui;
using StanleyParableXiv.Utility;

namespace StanleyParableXiv;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class Plugin : IDalamudPlugin
{
    public string Name => "Stanley Parable XIV";

    private readonly WindowSystem _windowSystem = new("StanleyParableXiv");
    private readonly EventService _eventService;

    private uint _lastXivVolumeSource = 0;
    private uint _lastXivMasterVolume = 0;

    public Plugin(DalamudPluginInterface pluginInterface)
    {
        PluginLog.Information("Starting plugin");
        DalamudService.Initialize(pluginInterface);

        DalamudService.CommandManager.AddHandler("/narrator", new CommandInfo(OnConfigCommand)
        {
            HelpMessage = "Opens the Stanley Parable XIV configuration."
        });

        DalamudService.CommandManager.AddHandler("/narratorvolume", new CommandInfo(OnVolumeCommand)
        {
            HelpMessage = "Sets the volume for the Narrator."
        });
            
        _windowSystem.AddWindow(new ConfigurationWindow());
        _eventService = new EventService();

        DalamudService.PluginInterface.UiBuilder.Draw += DrawUi;
        DalamudService.Framework.Update += OnFrameworkUpdate;
            
        #if DEBUG
        _windowSystem.GetWindow(ConfigurationWindow.Name)!.IsOpen = true;
        #endif
    }

    public void Dispose()
    {
        PluginLog.Information("Disposing plugin");
            
        _eventService.Dispose();
        AudioPlayer.Instance.Dispose();
            
        _windowSystem.RemoveAllWindows();
        DalamudService.CommandManager.RemoveHandler("/narrator");
        DalamudService.CommandManager.RemoveHandler("/narratorvolume");

        DalamudService.PluginInterface.UiBuilder.Draw -= DrawUi;
        DalamudService.Framework.Update -= OnFrameworkUpdate;
    }

    private void OnConfigCommand(string command, string commandArgs)
    {
        _windowSystem.GetWindow(ConfigurationWindow.Name)!.IsOpen = true;
    }

    private void OnVolumeCommand(string command, string commandArgs)
    {
        try
        {
            uint volumeSetting = 0;
            string[] args = commandArgs.Split(" ");
                
            if (args.Length >= 1 && !string.IsNullOrEmpty(args[0])) volumeSetting = uint.Parse(args[0]);
            if (volumeSetting is < 0 or > 100) throw new Exception("Volume must be between 0 and 100");
                
            Configuration.Instance.Volume = volumeSetting;
            Configuration.Instance.BindToXivVolumeSource = false;
            Configuration.Instance.Save();
            
            AudioPlayer.Instance.UpdateVolume();
                
            DalamudService.ChatGui.Print($"Narrator volume set to {volumeSetting}.");
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, "Exception occurred while setting volume via command");
            DalamudService.ChatGui.PrintError($"\"{commandArgs}\" is not a valid setting.");
        }
    }

    private void DrawUi()
    {
        _windowSystem.Draw();
    }

    private void OnFrameworkUpdate(Framework framework)
    {
        if (!Configuration.Instance.BindToXivVolumeSource) return;

        uint nextVolumeSource = XivUtility.GetVolume(Configuration.Instance.XivVolumeSource);
        uint nextMasterVolume = XivUtility.GetVolume(XivVolumeSource.Master);
            
        if (_lastXivVolumeSource == nextVolumeSource && _lastXivMasterVolume == nextMasterVolume) return;
            
        PluginLog.Debug("Updating volume due to framework update");
        AudioPlayer.Instance.UpdateVolume();
            
        _lastXivVolumeSource = nextVolumeSource;
        _lastXivMasterVolume = nextMasterVolume;
    }
}