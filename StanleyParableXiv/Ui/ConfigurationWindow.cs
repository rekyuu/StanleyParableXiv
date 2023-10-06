using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
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
            if (ImGui.BeginTabItem("Volume"))
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

                if (AssetsManager.IsUpdating)
                {
                    ImGui.Text("\nVoice lines are currently downloading, please wait...\n\n");
                }
                else if (!AssetsManager.HasEnoughFreeDiskSpace)
                {
                    ImGui.Text("\nUnable to download voice lines!\n\n100MB of free disk space is required.\nPlease clear some space and try again.\n\n");

                    if (ImGui.Button("Download voice lines"))
                    {
                        Plugin.UpdateVoiceLines();
                    }
                }
                else
                {
                    if (ImGui.Button("Play random voice line"))
                    {
                        Random random = new();
                        Array events = Enum.GetValues(typeof(AudioEvent));
                        AudioEvent result = (AudioEvent)events.GetValue(random.Next(events.Length))!;
                    
                        AudioPlayer.Instance.PlayRandomSoundFromCategory(result);
                    }
                }
                
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Events"))
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

            if (ImGui.BeginTabItem("Assets"))
            {
                ImGui.PushID("Assets");
                
                string configDir = DalamudService.PluginInterface.GetPluginConfigDirectory();
                string baseAssetsDir = $"{configDir}/assets";

                bool mp3AssetsDownloaded = Directory.Exists($"{baseAssetsDir}-mp3");
                bool oggAssetsDownloaded = Directory.Exists($"{baseAssetsDir}-ogg");
                
                List<string> assetsDownloaded = new();
                if (mp3AssetsDownloaded) assetsDownloaded.Add("mp3");
                if (oggAssetsDownloaded) assetsDownloaded.Add("ogg");

                ImGui.Text("Asset file type");
                ImGuiComponents.HelpMarker("OGG files are smaller, but may fail to play on systems missing codecs.");
                
                int assetsFileType = (int)Configuration.Instance.AssetsFileType;
                string[] assetsFileTypeOptions =
                {
                    "MP3",
                    "OGG"
                };

                if (ImGui.Combo("##AssetsFileType", ref assetsFileType, assetsFileTypeOptions, 
                        assetsFileTypeOptions.Length))
                {
                    Configuration.Instance.AssetsFileType = (AssetsFileType)assetsFileType;
                    Configuration.Instance.Save();

                    if ((Configuration.Instance.AssetsFileType == AssetsFileType.Mp3 && !mp3AssetsDownloaded) ||
                        (Configuration.Instance.AssetsFileType == AssetsFileType.Ogg && !oggAssetsDownloaded))
                    {
                        Plugin.UpdateVoiceLines();
                    }
                }
                
                ImGui.Text($"Assets currently downloaded: {string.Join(", ", assetsDownloaded)}");
                
                ImGui.Separator();
                
                if (AssetsManager.IsUpdating)
                {
                    ImGui.Text("\nVoice lines are currently downloading, please wait...\n\n");
                }
                else if (!AssetsManager.HasEnoughFreeDiskSpace)
                {
                    long diskSpaceRequired = AssetsManager.GetRequiredDiskSpace();
                    long diskSpaceRequiredMb = diskSpaceRequired / 1024 / 1024;
                    
                    ImGui.Text($"\nUnable to download voice lines!\n\n{diskSpaceRequiredMb}MB of free disk space is required.\nPlease clear some space and try again.\n\n");

                    if (ImGui.Button("Download voice lines"))
                    {
                        Plugin.UpdateVoiceLines();
                    }
                }
                else
                {
                    if (ImGui.Button("Re-download assets"))
                    {
                        Plugin.UpdateVoiceLines(true);
                    }
                }
                
                if (!mp3AssetsDownloaded) ImGui.PushStyleVar(ImGuiStyleVar.Alpha, ImGui.GetStyle().Alpha * 0.5f);
                if (ImGui.Button("Delete MP3 assets"))
                {
                    if (mp3AssetsDownloaded)
                    {
                        Task.Run(() => Directory.Delete($"{baseAssetsDir}-mp3", true));
                    }
                }
                if (!mp3AssetsDownloaded) ImGui.PopStyleVar();
                
                if (!oggAssetsDownloaded) ImGui.PushStyleVar(ImGuiStyleVar.Alpha, ImGui.GetStyle().Alpha * 0.5f);
                if (ImGui.Button("Delete OGG assets"))
                {
                    if (oggAssetsDownloaded)
                    {
                        Task.Run(() => Directory.Delete($"{baseAssetsDir}-ogg", true));
                    }
                }
                if (!oggAssetsDownloaded) ImGui.PopStyleVar();
                
                ImGui.Separator();

                ImGui.Text($"Required assets version: {AssetsManager.RequiredAssetsVersion}");
                ImGui.Text($"Current assets version: {AssetsManager.CurrentAssetsVersion}");
                
                ImGui.PopID();

                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Debug"))
            {
                ImGui.PushID("Debug");
                bool enableDebugLogging = Configuration.Instance.EnableDebugLogging;
                if (ImGui.Checkbox("Enable debug logging", ref enableDebugLogging))
                {
                    Configuration.Instance.EnableDebugLogging = enableDebugLogging;
                    Configuration.Instance.Save();
                }
                ImGui.PopID();

                ImGui.EndTabItem();
            }
        }
        
        ImGui.EndTabBar();
    }
}
