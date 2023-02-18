using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Game;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Network;
using Dalamud.Logging;
using Lumina.Excel.GeneratedSheets;
using StanleyParableXiv.Services;
using StanleyParableXiv.Utility;

namespace StanleyParableXiv.Events;

public class DutyEvent : IDisposable
{
    private TerritoryType? _currentTerritory;
    private bool _isInPvp = false;
    private bool _isBoundByDuty = false;
    private bool _isInQuestBattle = false;
    private bool _dutyStarted = false;
    private bool _dutyCompleted = false;
    private readonly Dictionary<uint, uint?> _partyStatus = new();
    
    /// <summary>
    /// Fires various duty instance events.
    /// Game network messages referenced from https://github.com/MidoriKami/KamiLib and https://github.com/Roselyyn/EldenRingDalamud
    /// </summary>
    public DutyEvent()
    {
        DalamudService.ClientState.EnterPvP += OnEnterPvP;
        DalamudService.ClientState.LeavePvP += OnLeavePvp;
        DalamudService.Framework.Update += OnFrameworkUpdate;
        DalamudService.GameNetwork.NetworkMessage += OnGameNetworkMessage;
    }
    
    public void Dispose()
    {
        DalamudService.ClientState.EnterPvP -= OnEnterPvP;
        DalamudService.ClientState.LeavePvP -= OnLeavePvp;
        DalamudService.Framework.Update -= OnFrameworkUpdate;
        DalamudService.GameNetwork.NetworkMessage -= OnGameNetworkMessage;
    }

    private void OnEnterPvP() => _isInPvp = true;

    private void OnLeavePvp() => _isInPvp = false;
        
    private void CheckIfPlayerIsBoundByDuty()
    {
        bool isNextBoundByDuty = DalamudService.Condition[ConditionFlag.BoundByDuty] ||
            DalamudService.Condition[ConditionFlag.BoundByDuty56] ||
            DalamudService.Condition[ConditionFlag.BoundByDuty95];
        
        // Ignore Island Sanctuary
        _currentTerritory = DalamudService.DataManager.Excel.GetSheet<TerritoryType>()?.GetRow(DalamudService.ClientState.TerritoryType);
        isNextBoundByDuty = isNextBoundByDuty && _currentTerritory?.TerritoryIntendedUse != 49;

        // Consider duty failed if it wasn't completed before leaving duty
        if (_isBoundByDuty && !isNextBoundByDuty && !_dutyCompleted && _isInQuestBattle && Configuration.Instance.EnableDutyFailedEvent)
        {
            AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.Failure);
        }
        
        _isBoundByDuty = isNextBoundByDuty;
    }

    private void CheckPartyMembers()
    {
        if (!_isBoundByDuty) return;
        if (DalamudService.PartyList.Length == 0) return;
        if (DalamudService.Condition[ConditionFlag.BetweenAreas]) return;

        uint[] partyStatusObjIds = _partyStatus.Keys.ToArray();
        uint[] partyListObjIds = DalamudService.PartyList.Select(x => x.ObjectId).ToArray();

        // If the party member isn't in cache, remove them
        foreach (uint objId in partyStatusObjIds)
        {
            if (!partyListObjIds.Contains(objId)) _partyStatus.Remove(objId);
        }
        
        foreach (PartyMember partyMember in DalamudService.PartyList)
        {
            // Skip if they're not in the same instance
            if (_currentTerritory != partyMember.Territory.GameData) continue;
            
            uint objId = partyMember.ObjectId;
            if (!_partyStatus.ContainsKey(objId)) _partyStatus[objId] = null;
            
            uint? nextStatus = null;
            uint? lastStatus = _partyStatus[objId];
            
            PlayerCharacter? player = DalamudUtility.GetPlayerCharacterFromPartyMember(partyMember);
            if (player == null) continue;
            
            OnlineStatus? onlineStatus = player.OnlineStatus.GameData;
            
            if (onlineStatus != null) nextStatus = onlineStatus.RowId;
            if (nextStatus == lastStatus) continue;
            
            _partyStatus[objId] = nextStatus;
            PluginLog.Debug("Party member status changed = {PlayerId} {PlayerName}, {PreviousOnlineStatus} -> {NextOnlineStatus}", 
                objId, partyMember.Name, lastStatus!, nextStatus!);

            if (!_dutyStarted) return;
            
            // Assume the player went offline (or left the instance)
            if (nextStatus == null && Configuration.Instance.EnableDutyPlayerDisconnectedEvent)
            {
                AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.Disconnect);
            }
            // Assume the player reconnected
            else if (lastStatus == null && Configuration.Instance.EnableDutyPlayerReconnectedEvent)
            {
                AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.Reconnect);
            }
        }
    }

    private void OnFrameworkUpdate(Framework framework)
    {
        _isInQuestBattle = _currentTerritory?.ContentFinderCondition?.Value?.ContentType?.Value?.RowId == 7;
        CheckIfPlayerIsBoundByDuty();
        CheckPartyMembers();
    }

    private unsafe void OnGameNetworkMessage(IntPtr dataPtr, ushort opCode, uint sourceActorId, uint targetActorId,
        NetworkMessageDirection direction)
    {
        if (opCode != DalamudService.DataManager.ServerOpCodes["ActorControlSelf"]) return;
        
        ushort cat = *(ushort*)(dataPtr + 0x00);
        uint updateType = *(uint*)(dataPtr + 0x08);
        
        PluginLog.Verbose("OpCode = {OpCode}, Cat = 0x{Cat:X}, UpdateType = 0x{UpdateType:X}", opCode, cat, updateType);

        switch (cat)
        {
            // Encounter Start
            case 0x6D when updateType == 0x40000001:
                if (_isInQuestBattle) break;
                
                _dutyStarted = true;
                _dutyCompleted = false;

                if (Configuration.Instance.EnableDutyStartEvent)
                {
                    AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.EncounterStart);
                }
                
                break;
            case 0x6D when updateType == 0x40000002: // Alternate duty complete flag?
            case 0x6D when updateType == 0x40000003: // Encounter Complete
                if (_isInPvp) break;
                PlayDutyCompleteAudio();
                break;
            // Start PvP Countdown 
            case 0x6D when updateType == 0x40000004:
                _dutyStarted = true;
                _dutyCompleted = false;

                if (Configuration.Instance.EnablePvpCountdownStartEvent)
                {
                    AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.CountdownStart);
                }

                if (Configuration.Instance.EnablePvpCountdown10Event)
                {
                    Task.Delay(20_000).ContinueWith(_ =>
                    {
                        AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.Countdown10);
                    });
                }
                
                break;
            // Party Wipe
            case 0x6D when updateType == 0x40000005:
                if (Configuration.Instance.EnableDutyPartyWipeEvent)
                {
                    Task.Delay(1000).ContinueWith(_ =>
                    {
                        AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.Wipe);
                    });
                }
                
                break;
            // Encounter Recommence
            case 0x6D when updateType == 0x40000006:
                break;
            // Possible PvP complete 
            case 0x6D when updateType == 0x40000007:
                PluginLog.Verbose("0x40000007 fired");
                break;
            // PvP win
            case 0x355 when updateType == 0x1F4:
                _dutyStarted = false;
                _dutyCompleted = true;

                if (Configuration.Instance.EnablePvpWinEvent)
                {
                    Task.Delay(3000).ContinueWith(_ =>
                    {
                        AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.PvpWin);
                    });
                }
                
                break;
            // PvP loss
            case 0x355 when updateType == 0xFA:
                _dutyStarted = false;
                _dutyCompleted = true;

                if (Configuration.Instance.EnablePvpLossEvent)
                {
                    Task.Delay(3000).ContinueWith(_ =>
                    {
                        AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.Failure);
                    });
                }
                
                break;
        }
    }

    private void PlayDutyCompleteAudio()
    {
        if (!_dutyStarted && _dutyCompleted) return;
        
        _dutyStarted = false;
        _dutyCompleted = true;

        if (XivUtility.PlayerIsInHighEndDuty())
        {
            uint territory = DalamudService.ClientState.TerritoryType;
            
            if (!Configuration.Instance.CompletedHighEndDuties.ContainsKey(territory))
            {
                Configuration.Instance.CompletedHighEndDuties[territory] = 1;
            }
            else
            {
                Configuration.Instance.CompletedHighEndDuties[territory] += 1;
            }
            
            Configuration.Instance.Save();
            
            PluginLog.Debug("Kills updated for {Territory}: {Kills}", territory, Configuration.Instance.CompletedHighEndDuties[territory]);

            if (Configuration.Instance.EnableBossKillStreaks)
            {
                switch (Configuration.Instance.CompletedHighEndDuties[territory])
                {
                    case 15:
                        AudioPlayer.Instance.PlaySound("announcer_dlc_stanleyparable_killing_spree/announcer_kill_limit_15.mp3");
                        break;
                    case 20:
                        AudioPlayer.Instance.PlaySound("announcer_dlc_stanleyparable_killing_spree/announcer_kill_limit_20.mp3");
                        break;
                    case 30:
                        AudioPlayer.Instance.PlaySound("announcer_dlc_stanleyparable_killing_spree/announcer_kill_limit_30.mp3");
                        break;
                    case 50:
                        AudioPlayer.Instance.PlaySound("announcer_dlc_stanleyparable_killing_spree/announcer_kill_limit_50.mp3");
                        break;
                    case 69:
                        AudioPlayer.Instance.PlaySound("announcer_dlc_stanleyparable_killing_spree/announcer_kill_limit_69.mp3");
                        break;
                    case 70:
                        AudioPlayer.Instance.PlaySound("announcer_dlc_stanleyparable_killing_spree/announcer_kill_limit_70.mp3");
                        break;
                    case 71:
                        AudioPlayer.Instance.PlaySound("announcer_dlc_stanleyparable_killing_spree/announcer_kill_limit_71.mp3");
                        break;
                    case 85:
                        AudioPlayer.Instance.PlaySound("announcer_dlc_stanleyparable_killing_spree/announcer_kill_limit_85.mp3");
                        break;
                    case 90:
                        AudioPlayer.Instance.PlaySound("announcer_dlc_stanleyparable_killing_spree/announcer_kill_limit_90.mp3");
                        break;
                    case 99:
                        AudioPlayer.Instance.PlaySound("announcer_dlc_stanleyparable_killing_spree/announcer_kill_limit_99.mp3");
                        break;
                    case 100:
                        AudioPlayer.Instance.PlaySound("announcer_dlc_stanleyparable_killing_spree/announcer_kill_limit_100.mp3");
                        break;
                    case 101:
                        AudioPlayer.Instance.PlaySound("announcer_dlc_stanleyparable_killing_spree/announcer_kill_limit_101.mp3");
                        break;
                }
            }
        }

        if (Configuration.Instance.EnableDutyCompleteEvent)
        {
            Task.Delay(1000).ContinueWith(_ =>
            {
                AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.EncounterComplete);
            });
        }
    }
}