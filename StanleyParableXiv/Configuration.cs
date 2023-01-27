using Dalamud.Configuration;
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

    public bool BindToXivVolumeSource { get; set; } = true;

    public XivVolumeSource XivVolumeSource { get; set; } = XivVolumeSource.Voice;

    public uint XivVolumeSourceBoost { get; set; } = 100;

    public bool EnableAfkEvent { get; set; } = true;

    public uint AfkEventTimeframe { get; set; } = 300;

    public bool EnableCountdownStartEvent { get; set; } = true;

    public bool EnableCountdown10Event { get; set; } = true;

    public bool EnableDutyStartEvent { get; set; } = true;

    public bool EnableDutyCompleteEvent { get; set; } = true;

    public bool EnableDutyPartyWipeEvent { get; set; } = true;

    public bool EnableDutyFailedEvent { get; set; } = true;

    public bool EnableDutyPlayerDisconnectedEvent { get; set; } = true;

    public bool EnableDutyPlayerReconnectedEvent { get; set; } = true;

    public bool EnablePvpCountdownStartEvent { get; set; } = true;

    public bool EnablePvpCountdown10Event { get; set; } = true;

    public bool EnablePvpFirstBloodEvent { get; set; } = true;

    public bool EnablePvpKillStreaksEvent { get; set; } = true;

    public bool EnablePvpMultikillsEvent { get; set; } = true;

    public bool EnablePvpPrepareEvent { get; set; } = true;

    public bool EnablePvpWinEvent { get; set; } = true;

    public bool EnablePvpLossEvent { get; set; } = true;

    public bool EnablePvpChatEvent { get; set; } = true;

    public bool EnableLoginEvent { get; set; } = true;

    public bool EnableMarketBoardPurchaseEvent { get; set; } = true;

    public bool EnableRespawnEvent { get; set; } = true;

    public bool EnableSynthesisFailedEvent { get; set; } = true;

    public void Save()
    {
        DalamudService.PluginInterface.SavePluginConfig(this);
    }
}