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

    /// <summary>
    /// The manual volume set by the user.
    /// </summary>
    public uint Volume { get; set; } = 50;

    /// <summary>
    /// Binds the volume to an FFXIV sound channel.
    /// </summary>
    public bool BindToXivVolumeSource { get; set; } = true;

    /// <summary>
    /// The FFXIV sound channel to bind to.
    /// </summary>
    public XivVolumeSource XivVolumeSource { get; set; } = XivVolumeSource.Voice;

    /// <summary>
    /// The amount of volume boosting to apply to the bound FFXIV sound channel.
    /// </summary>
    public uint XivVolumeSourceBoost { get; set; } = 100;

    /// <summary>
    /// Enables AFK sound events.
    /// </summary>
    public bool EnableAfkEvent { get; set; } = true;

    /// <summary>
    /// The amount of time before the AFK sound plays.
    /// </summary>
    public uint AfkEventTimeframe { get; set; } = 300;

    /// <summary>
    /// Enables the countdown start event.
    /// </summary>
    public bool EnableCountdownStartEvent { get; set; } = true;

    /// <summary>
    /// Enables the countdown event when 10 seconds remain.
    /// </summary>
    public bool EnableCountdown10Event { get; set; } = true;

    /// <summary>
    /// Enables the event on starting duties.
    /// </summary>
    public bool EnableDutyStartEvent { get; set; } = true;

    /// <summary>
    /// Enables the duty completion event.
    /// </summary>
    public bool EnableDutyCompleteEvent { get; set; } = true;

    /// <summary>
    /// Enables the party wipe event.
    /// </summary>
    public bool EnableDutyPartyWipeEvent { get; set; } = true;

    /// <summary>
    /// Enables the event when leaving the duty before completion.
    /// </summary>
    public bool EnableDutyFailedEvent { get; set; } = true;

    /// <summary>
    /// Enables the player disconnect event.
    /// </summary>
    public bool EnableDutyPlayerDisconnectedEvent { get; set; } = true;

    /// <summary>
    /// Enables the player reconnect event.
    /// </summary>
    public bool EnableDutyPlayerReconnectedEvent { get; set; } = true;

    /// <summary>
    /// Enables the countdown start event in PvP.
    /// </summary>
    public bool EnablePvpCountdownStartEvent { get; set; } = true;

    /// <summary>
    /// Enables the countdown event when 10 seconds remain in PvP.
    /// </summary>
    public bool EnablePvpCountdown10Event { get; set; } = true;

    /// <summary>
    /// Enables the event when the first player dies in PvP.
    /// </summary>
    public bool EnablePvpFirstBloodEvent { get; set; } = true;

    /// <summary>
    /// Enables PvP kill streaks.
    /// </summary>
    public bool EnablePvpKillStreaksEvent { get; set; } = true;

    /// <summary>
    /// Enables PvP multikill streaks.
    /// </summary>
    public bool EnablePvpMultikillsEvent { get; set; } = true;

    /// <summary>
    /// Enables the event when PvP starts.
    /// </summary>
    public bool EnablePvpPrepareEvent { get; set; } = true;

    /// <summary>
    /// Enables the PvP win event.
    /// </summary>
    public bool EnablePvpWinEvent { get; set; } = true;

    /// <summary>
    /// Enables the PvP loss event.
    /// </summary>
    public bool EnablePvpLossEvent { get; set; } = true;

    /// <summary>
    /// Enables chat output for PvP kill events.
    /// </summary>
    public bool EnablePvpChatEvent { get; set; } = true;

    /// <summary>
    /// Enables the user login event.
    /// </summary>
    public bool EnableLoginEvent { get; set; } = true;

    /// <summary>
    /// Enables the market board purchase event.
    /// </summary>
    public bool EnableMarketBoardPurchaseEvent { get; set; } = true;

    /// <summary>
    /// Enables the user respawn event.
    /// </summary>
    public bool EnableRespawnEvent { get; set; } = true;

    /// <summary>
    /// Enables the user crafting failure event.
    /// </summary>
    public bool EnableSynthesisFailedEvent { get; set; } = true;

    /// <summary>
    /// Saves the user configuration.
    /// </summary>
    public void Save()
    {
        DalamudService.PluginInterface.SavePluginConfig(this);
    }
}