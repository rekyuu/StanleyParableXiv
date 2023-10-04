using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Lumina.Excel.GeneratedSheets;
using StanleyParableXiv.Services;
using StanleyParableXiv.Utility;

namespace StanleyParableXiv.Events;

public class DutyEvent : IDisposable
{
    private const string ActorControlSelfSig = "48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 30 48 8B D9 49 8B F8 41 0F B7 08";
    private readonly Hook<Action<uint, uint, uint, uint, uint, uint, uint, uint, ulong, byte>> _actorControlSelfHook;
    
    private TerritoryType? _currentTerritory;
    private bool _isInPvp = false;
    private bool _isBoundByDuty = false;
    private bool _isInIgnoredTerritory = false;
    private bool _isInAllowedContentType = false;
    private bool _dutyStarted = false;
    private bool _dutyCompleted = false;
    private readonly Dictionary<uint, uint?> _partyStatus = new();

    private readonly uint?[] _territoriesToIgnore = 
    {
        653, // Company Workshop
    };

    private readonly uint?[] _allowedContentTypes = 
    {
        1,  // Duty Roulette
        2,  // Dungeons
        3,  // Guildhests
        4,  // Trials
        5,  // Raids
        21, // Deep Dungeons
        28, // Ultimate Raids
        30  // V&C Dungeon Finder
    };
    
    /// <summary>
    /// Fires various duty instance events.
    /// Game network messages referenced from https://github.com/MidoriKami/KamiLib and https://github.com/Roselyyn/EldenRingDalamud
    /// </summary>
    public DutyEvent()
    {
        DalamudService.DutyState.DutyStarted += OnDutyStarted;
        DalamudService.DutyState.DutyWiped += OnDutyWiped;
        DalamudService.DutyState.DutyCompleted += OnDutyCompleted;
        DalamudService.ClientState.EnterPvP += OnEnterPvP;
        DalamudService.ClientState.LeavePvP += OnLeavePvp;
        DalamudService.Framework.Update += OnFrameworkUpdate;
        
        _actorControlSelfHook = DalamudService.GameInteropProvider.HookFromSignature(
            ActorControlSelfSig,
            OnActorControlSelf);
        _actorControlSelfHook.Enable();
    }

    public void Dispose()
    {
        DalamudService.DutyState.DutyStarted -= OnDutyStarted;
        DalamudService.DutyState.DutyWiped -= OnDutyWiped;
        DalamudService.DutyState.DutyCompleted -= OnDutyCompleted;
        DalamudService.ClientState.EnterPvP -= OnEnterPvP;
        DalamudService.ClientState.LeavePvP -= OnLeavePvp;
        DalamudService.Framework.Update -= OnFrameworkUpdate;
        
        _actorControlSelfHook.Dispose();
        
        GC.SuppressFinalize(this);
    }

    private void OnDutyStarted(object? sender, ushort e)
    {
        if (!_isInAllowedContentType || _isInIgnoredTerritory) return;
                
        _dutyStarted = true;
        _dutyCompleted = false;

        if (!Configuration.Instance.EnableDutyStartEvent) return;
        
        AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.EncounterStart);
    }

    private void OnDutyWiped(object? sender, ushort e)
    {
        if (!Configuration.Instance.EnableDutyPartyWipeEvent) return;
        
        Task.Delay(1000).ContinueWith(_ =>
        {
            AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.Wipe);
        });
    }

    private void OnDutyCompleted(object? sender, ushort e)
    {
        if (_isInPvp) return;
        PlayDutyCompleteAudio();
    }

    private void OnEnterPvP() => _isInPvp = true;

    private void OnLeavePvp() => _isInPvp = false;

    private void OnFrameworkUpdate(IFramework framework)
    {
        CheckIfPlayerIsBoundByDuty();
        CheckPartyMembers();
    }

    private void OnActorControlSelf(uint category, uint cat, uint a3, uint updateType, uint a5, uint a6, uint a7, 
        uint a8, ulong targetId, byte a10)
    {
        _actorControlSelfHook.Original(category, cat, a3, updateType, a5, a6, a7, a8, targetId, a10);

        switch (cat)
        {
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
        
    private void CheckIfPlayerIsBoundByDuty()
    {
        bool isNextBoundByDuty = DalamudService.Condition[ConditionFlag.BoundByDuty] ||
            DalamudService.Condition[ConditionFlag.BoundByDuty56] ||
            DalamudService.Condition[ConditionFlag.BoundByDuty95];
        
        // Ignore Island Sanctuary
        _currentTerritory = DalamudService.DataManager.Excel.GetSheet<TerritoryType>()?.GetRow(DalamudService.ClientState.TerritoryType);
        isNextBoundByDuty = isNextBoundByDuty && _currentTerritory?.TerritoryIntendedUse != 49;
        _isInIgnoredTerritory = _territoriesToIgnore.Contains(_currentTerritory?.RowId);
        bool isNextInAllowedContentType = _allowedContentTypes.Contains(_currentTerritory?.ContentFinderCondition?.Value?.ContentType?.Value?.RowId);
        
        // Consider duty failed if it wasn't completed before leaving duty
        if (_isBoundByDuty && !isNextBoundByDuty && !_dutyCompleted && !_isInIgnoredTerritory && _isInAllowedContentType && Configuration.Instance.EnableDutyFailedEvent)
        {
            AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.Failure);
        }

        _isBoundByDuty = isNextBoundByDuty;
        _isInAllowedContentType = isNextInAllowedContentType;
    }

    private void CheckPartyMembers()
    {
        if (!_isBoundByDuty || _isInIgnoredTerritory) return;
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
            _partyStatus.TryAdd(objId, null);
            
            uint? nextStatus = null;
            uint? lastStatus = _partyStatus[objId];
            
            PlayerCharacter? player = DalamudUtility.GetPlayerCharacterFromPartyMember(partyMember);
            if (player == null) continue;
            
            OnlineStatus? onlineStatus = player.OnlineStatus.GameData;
            
            if (onlineStatus != null) nextStatus = onlineStatus.RowId;
            if (nextStatus == lastStatus) continue;
            
            _partyStatus[objId] = nextStatus;
            DalamudService.Log.Debug("Party member status changed = {PlayerId} {PlayerName}, {PreviousOnlineStatus} -> {NextOnlineStatus}", 
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
            
            DalamudService.Log.Debug("Kills updated for {Territory}: {Kills}", territory, Configuration.Instance.CompletedHighEndDuties[territory]);

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