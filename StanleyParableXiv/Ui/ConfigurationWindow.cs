using System;
using System.Numerics;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using StanleyParableXiv.Services;
using StanleyParableXiv.Utility;

namespace StanleyParableXiv.Ui;

public class ConfigurationWindow : Window, IDisposable
{
    public ConfigurationWindow() : base("Stanley Parable XIV Configuration")
    {
        Size = new Vector2(320, 192);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public void Dispose() { }

    public override void Draw()
    {
        if (ImGui.BeginTabBar("##StanleyParableConfigurationTabBar", ImGuiTabBarFlags.None))
        {
            if (ImGui.BeginTabItem("Volume Settings"))
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
                
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Event Settings"))
            {
                ImGui.PushID("General");
                if (ImGui.CollapsingHeader("General"))
                {
                    bool enableLogin = Configuration.Instance.EnableLoginEvent;
                    if (ImGui.Checkbox("Login", ref enableLogin))
                    {
                        Configuration.Instance.EnableLoginEvent = enableLogin;
                        Configuration.Instance.Save();
                    }
                    
                    bool enableRespawn = Configuration.Instance.EnableRespawnEvent;
                    if (ImGui.Checkbox("Respawn", ref enableRespawn))
                    {
                        Configuration.Instance.EnableRespawnEvent = enableRespawn;
                        Configuration.Instance.Save();
                    }
                    
                    bool enableSynthesisFailed = Configuration.Instance.EnableSynthesisFailedEvent;
                    if (ImGui.Checkbox("Synthesis Failed", ref enableSynthesisFailed))
                    {
                        Configuration.Instance.EnableSynthesisFailedEvent = enableSynthesisFailed;
                        Configuration.Instance.Save();
                    }
                    
                    bool enableMarketBoardPurchase = Configuration.Instance.EnableMarketBoardPurchaseEvent;
                    if (ImGui.Checkbox("Market Board Purchase", ref enableMarketBoardPurchase))
                    {
                        Configuration.Instance.EnableMarketBoardPurchaseEvent = enableMarketBoardPurchase;
                        Configuration.Instance.Save();
                    }
                    
                    ImGui.Separator();
                    
                    bool enableAfkEvent = Configuration.Instance.EnableAfkEvent;
                    if (ImGui.Checkbox("AFK", ref enableAfkEvent))
                    {
                        Configuration.Instance.EnableAfkEvent = enableAfkEvent;
                        Configuration.Instance.Save();
                    }

                    ImGui.PushItemWidth(64);
                    int afkEventTimeframe = (int)Configuration.Instance.AfkEventTimeframe;
                    if (ImGui.DragInt("AFK Timeframe", ref afkEventTimeframe, 30, 30))
                    {
                        if (afkEventTimeframe < 30) afkEventTimeframe = 30;
                        
                        Configuration.Instance.AfkEventTimeframe = (uint)afkEventTimeframe;
                        Configuration.Instance.Save();
                    }
                }
                ImGui.PopID();

                ImGui.PushID("Countdown");
                if (ImGui.CollapsingHeader("Countdown"))
                {
                    bool enableCountdownEvent = Configuration.Instance.EnableCountdownStartEvent;
                    if (ImGui.Checkbox("Countdown Start", ref enableCountdownEvent))
                    {
                        Configuration.Instance.EnableCountdownStartEvent = enableCountdownEvent;
                        Configuration.Instance.Save();
                    }
                    
                    bool enableCountdown10Event = Configuration.Instance.EnableCountdown10Event;
                    if (ImGui.Checkbox("10 Seconds Remaining", ref enableCountdown10Event))
                    {
                        Configuration.Instance.EnableCountdown10Event = enableCountdown10Event;
                        Configuration.Instance.Save();
                    }
                }
                ImGui.PopID();

                ImGui.PushID("Duty");
                if (ImGui.CollapsingHeader("Duty"))
                {
                    bool enableDutyStart = Configuration.Instance.EnableDutyStartEvent;
                    if (ImGui.Checkbox("Duty Start", ref enableDutyStart))
                    {
                        Configuration.Instance.EnableDutyStartEvent = enableDutyStart;
                        Configuration.Instance.Save();
                    }
                    
                    bool enableDutyComplete = Configuration.Instance.EnableDutyCompleteEvent;
                    if (ImGui.Checkbox("Duty Complete", ref enableDutyComplete))
                    {
                        Configuration.Instance.EnableDutyCompleteEvent = enableDutyComplete;
                        Configuration.Instance.Save();
                    }
                    
                    bool enableDutyFailed = Configuration.Instance.EnableDutyFailedEvent;
                    if (ImGui.Checkbox("Duty Failed", ref enableDutyFailed))
                    {
                        Configuration.Instance.EnableDutyFailedEvent = enableDutyFailed;
                        Configuration.Instance.Save();
                    }
                    
                    ImGuiComponents.HelpMarker("Plays on leaving the duty before completion.");
                    
                    bool enablePartyWipe = Configuration.Instance.EnableDutyPartyWipeEvent;
                    if (ImGui.Checkbox("On Party Wipe", ref enablePartyWipe))
                    {
                        Configuration.Instance.EnableDutyPartyWipeEvent = enablePartyWipe;
                        Configuration.Instance.Save();
                    }
                    
                    bool enableBossKillStreaks = Configuration.Instance.EnableBossKillStreaks;
                    if (ImGui.Checkbox("High End Boss Kill Streaks", ref enableBossKillStreaks))
                    {
                        Configuration.Instance.EnableBossKillStreaks = enableBossKillStreaks;
                        Configuration.Instance.Save();
                    }
                    
                    ImGui.Separator();
                    
                    bool enablePlayerDisconnect = Configuration.Instance.EnableDutyPlayerDisconnectedEvent;
                    if (ImGui.Checkbox("On Player Disconnect", ref enablePlayerDisconnect))
                    {
                        Configuration.Instance.EnableDutyPlayerDisconnectedEvent = enablePlayerDisconnect;
                        Configuration.Instance.Save();
                    }
                    
                    bool enablePlayerReconnect = Configuration.Instance.EnableDutyPlayerReconnectedEvent;
                    if (ImGui.Checkbox("On Player Reconnect", ref enablePlayerReconnect))
                    {
                        Configuration.Instance.EnableDutyPlayerReconnectedEvent = enablePlayerReconnect;
                        Configuration.Instance.Save();
                    }
                }
                ImGui.PopID();

                ImGui.PushID("PvP");
                if (ImGui.CollapsingHeader("PvP"))
                {
                    bool enablePvpCountdownEvent = Configuration.Instance.EnablePvpCountdownStartEvent;
                    if (ImGui.Checkbox("Countdown Start", ref enablePvpCountdownEvent))
                    {
                        Configuration.Instance.EnablePvpCountdownStartEvent = enablePvpCountdownEvent;
                        Configuration.Instance.Save();
                    }
                    
                    bool enablePvpCountdown10Event = Configuration.Instance.EnablePvpCountdown10Event;
                    if (ImGui.Checkbox("10 Seconds Remaining", ref enablePvpCountdown10Event))
                    {
                        Configuration.Instance.EnablePvpCountdown10Event = enablePvpCountdown10Event;
                        Configuration.Instance.Save();
                    }
                    
                    ImGui.Separator();
                    
                    bool enableFirstBlood = Configuration.Instance.EnablePvpFirstBloodEvent;
                    if (ImGui.Checkbox("First Blood", ref enableFirstBlood))
                    {
                        Configuration.Instance.EnablePvpFirstBloodEvent = enableFirstBlood;
                        Configuration.Instance.Save();
                    }
                    
                    bool enableKillStreaks = Configuration.Instance.EnablePvpKillStreaksEvent;
                    if (ImGui.Checkbox("Kill Streaks", ref enableKillStreaks))
                    {
                        Configuration.Instance.EnablePvpKillStreaksEvent = enableKillStreaks;
                        Configuration.Instance.Save();
                    }
                    
                    bool enableMultikills = Configuration.Instance.EnablePvpMultikillsEvent;
                    if (ImGui.Checkbox("Multikills", ref enableMultikills))
                    {
                        Configuration.Instance.EnablePvpMultikillsEvent = enableMultikills;
                        Configuration.Instance.Save();
                    }
                    
                    bool enableChat = Configuration.Instance.EnablePvpChatEvent;
                    if (ImGui.Checkbox("Kill Events in Chat", ref enableChat))
                    {
                        Configuration.Instance.EnablePvpChatEvent = enableChat;
                        Configuration.Instance.Save();
                    }
                    
                    ImGui.Separator();
                    
                    bool enablePrepare = Configuration.Instance.EnablePvpPrepareEvent;
                    if (ImGui.Checkbox("Prepare", ref enablePrepare))
                    {
                        Configuration.Instance.EnablePvpPrepareEvent = enablePrepare;
                        Configuration.Instance.Save();
                    }
                    
                    bool enableWin = Configuration.Instance.EnablePvpWinEvent;
                    if (ImGui.Checkbox("Win", ref enableWin))
                    {
                        Configuration.Instance.EnablePvpWinEvent = enableWin;
                        Configuration.Instance.Save();
                    }
                    
                    bool enableLoss = Configuration.Instance.EnablePvpLossEvent;
                    if (ImGui.Checkbox("Loss", ref enableLoss))
                    {
                        Configuration.Instance.EnablePvpLossEvent = enableLoss;
                        Configuration.Instance.Save();
                    }
                }
                ImGui.PopID();

                ImGui.EndTabItem();
            }
        }
        
        ImGui.EndTabBar();
    }
}
