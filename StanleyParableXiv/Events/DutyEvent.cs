using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using StanleyParableXiv.Services;
using StanleyParableXiv.Utility;

namespace StanleyParableXiv.Events;

public class DutyEvent : IDisposable
{
    private TerritoryType? _currentTerritory;
    private bool _isLoggedIn = false;
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
        DalamudService.ClientState.Login += OnLogin;
        DalamudService.ClientState.Logout += OnLogout;
        DalamudService.Framework.Update += OnFrameworkUpdate;
    }

    private void OnLogin() => _isLoggedIn = true;

    private void OnLogout(int type, int code) => _isLoggedIn = false;

    public void Dispose()
    {
        DalamudService.DutyState.DutyStarted -= OnDutyStarted;
        DalamudService.DutyState.DutyWiped -= OnDutyWiped;
        DalamudService.DutyState.DutyCompleted -= OnDutyCompleted;
        DalamudService.ClientState.Login -= OnLogin;
        DalamudService.ClientState.Logout -= OnLogout;
        DalamudService.Framework.Update -= OnFrameworkUpdate;
        
        GC.SuppressFinalize(this);
    }

    private void OnDutyStarted(object? sender, ushort e)
    {
        if (DalamudService.ClientState.IsPvPExcludingDen) return;
        if (!_isInAllowedContentType || _isInIgnoredTerritory) return;

        DalamudService.Log.Debug("Duty started");

        _dutyStarted = true;
        _dutyCompleted = false;

        if (!Configuration.Instance.EnableDutyStartEvent) return;
        
        AudioService.Instance.PlayRandomSoundFromCategory(AudioEvent.EncounterStart);
    }

    private static void OnDutyWiped(object? sender, ushort e)
    {
        DalamudService.Log.Debug("Duty wiped");
        
        if (!Configuration.Instance.EnableDutyPartyWipeEvent) return;
        
        Task.Delay(1000).ContinueWith(_ =>
        {
            AudioService.Instance.PlayRandomSoundFromCategory(AudioEvent.Wipe);
        });
    }

    private void OnDutyCompleted(object? sender, ushort e)
    {
        if (DalamudService.ClientState.IsPvPExcludingDen) return;

        _dutyCompleted = true;

        DalamudService.Log.Debug("Duty completed");
        PlayDutyCompleteAudio();
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        CheckIfPlayerIsBoundByDuty();
        CheckPartyMembers();
    }
        
    private void CheckIfPlayerIsBoundByDuty()
    {
        if (!_isLoggedIn) return;

        bool isNextBoundByDuty = DalamudService.Condition[ConditionFlag.BoundByDuty] ||
            DalamudService.Condition[ConditionFlag.BoundByDuty56] ||
            DalamudService.Condition[ConditionFlag.BoundByDuty95];
        
        // Ignore Island Sanctuary
        bool currentTerritoryExists = DalamudService.DataManager.Excel
            .GetSheet<TerritoryType>()
            .TryGetRow(DalamudService.ClientState.TerritoryType, out TerritoryType currentTerritory);
        if (!currentTerritoryExists) return;

        _currentTerritory = currentTerritory;
        isNextBoundByDuty = isNextBoundByDuty && _currentTerritory?.TerritoryIntendedUse.RowId != 49;
        _isInIgnoredTerritory = _territoriesToIgnore.Contains(_currentTerritory?.RowId);
        bool isNextInAllowedContentType = _allowedContentTypes.Contains(_currentTerritory?.ContentFinderCondition.Value.ContentType.Value.RowId);
        
        // Consider duty failed if it wasn't completed before leaving duty
        if (_isBoundByDuty && !isNextBoundByDuty && !_dutyCompleted && !_isInIgnoredTerritory && _isInAllowedContentType && Configuration.Instance.EnableDutyFailedEvent)
        {
            AudioService.Instance.PlayRandomSoundFromCategory(AudioEvent.Failure);
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
        
        foreach (IPartyMember partyMember in DalamudService.PartyList)
        {
            // Skip if they're not in the same instance
            if (!partyMember.Territory.ValueNullable.Equals(_currentTerritory)) continue;
            
            uint objId = partyMember.ObjectId;
            _partyStatus.TryAdd(objId, null);
            
            uint? nextStatus = null;
            uint? lastStatus = _partyStatus[objId];

            IPlayerCharacter? player = DalamudUtility.GetPlayerCharacterFromPartyMember(partyMember);
            if (player == null) continue;
            
            OnlineStatus? onlineStatus = player.OnlineStatus.ValueNullable;
            
            if (onlineStatus != null) nextStatus = player.OnlineStatus.RowId;
            if (nextStatus == lastStatus) continue;
            
            _partyStatus[objId] = nextStatus;
            DalamudService.Log.Debug("Party member status changed = {PlayerId} {PlayerName}, {PreviousOnlineStatus} -> {NextOnlineStatus}", 
                objId, partyMember.Name, lastStatus!, nextStatus!);

            if (!_dutyStarted) return;
            
            // Assume the player went offline (or left the instance)
            if (nextStatus == null && Configuration.Instance.EnableDutyPlayerDisconnectedEvent)
            {
                AudioService.Instance.PlayRandomSoundFromCategory(AudioEvent.Disconnect);
            }
            // Assume the player reconnected
            else if (lastStatus == null && Configuration.Instance.EnableDutyPlayerReconnectedEvent)
            {
                AudioService.Instance.PlayRandomSoundFromCategory(AudioEvent.Reconnect);
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
                        AudioService.Instance.PlaySound("announcer_dlc_stanleyparable_killing_spree/announcer_kill_limit_15.mp3");
                        break;
                    case 20:
                        AudioService.Instance.PlaySound("announcer_dlc_stanleyparable_killing_spree/announcer_kill_limit_20.mp3");
                        break;
                    case 30:
                        AudioService.Instance.PlaySound("announcer_dlc_stanleyparable_killing_spree/announcer_kill_limit_30.mp3");
                        break;
                    case 50:
                        AudioService.Instance.PlaySound("announcer_dlc_stanleyparable_killing_spree/announcer_kill_limit_50.mp3");
                        break;
                    case 69:
                        AudioService.Instance.PlaySound("announcer_dlc_stanleyparable_killing_spree/announcer_kill_limit_69.mp3");
                        break;
                    case 70:
                        AudioService.Instance.PlaySound("announcer_dlc_stanleyparable_killing_spree/announcer_kill_limit_70.mp3");
                        break;
                    case 71:
                        AudioService.Instance.PlaySound("announcer_dlc_stanleyparable_killing_spree/announcer_kill_limit_71.mp3");
                        break;
                    case 85:
                        AudioService.Instance.PlaySound("announcer_dlc_stanleyparable_killing_spree/announcer_kill_limit_85.mp3");
                        break;
                    case 90:
                        AudioService.Instance.PlaySound("announcer_dlc_stanleyparable_killing_spree/announcer_kill_limit_90.mp3");
                        break;
                    case 99:
                        AudioService.Instance.PlaySound("announcer_dlc_stanleyparable_killing_spree/announcer_kill_limit_99.mp3");
                        break;
                    case 100:
                        AudioService.Instance.PlaySound("announcer_dlc_stanleyparable_killing_spree/announcer_kill_limit_100.mp3");
                        break;
                    case 101:
                        AudioService.Instance.PlaySound("announcer_dlc_stanleyparable_killing_spree/announcer_kill_limit_101.mp3");
                        break;
                }
            }
        }

        if (Configuration.Instance.EnableDutyCompleteEvent)
        {
            Task.Delay(1000).ContinueWith(_ =>
            {
                AudioService.Instance.PlayRandomSoundFromCategory(AudioEvent.EncounterComplete);
            });
        }
    }
}