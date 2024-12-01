using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using StanleyParableXiv.Services;
using StanleyParableXiv.Utility;

namespace StanleyParableXiv.Events;

public class DutyEvent : IDisposable
{
    private bool _isBoundByDuty = false;
    private bool _dutyCompleted = false;
    private Dictionary<string, uint> _partyMembers = new();

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

    private readonly uint?[] _ignoredTerritories = 
    {
        653, // Company Workshop
        941, // Triple Triad Invitational Parlor
    };

    private readonly uint?[] _ignoredIntendedUses =
    [
        49 // Island Sanctuary
    ];
    
    /// <summary>
    /// Fires various duty instance events.
    /// Game network messages referenced from https://github.com/MidoriKami/KamiLib and https://github.com/Roselyyn/EldenRingDalamud
    /// </summary>
    public DutyEvent()
    {
        DalamudService.DutyState.DutyStarted += OnDutyStarted;
        DalamudService.DutyState.DutyWiped += OnDutyWiped;
        DalamudService.DutyState.DutyCompleted += OnDutyCompleted;
        DalamudService.Framework.Update += OnFrameworkUpdate;
        TerritoryService.Instance.TerritoryChanged += OnTerritoryChanged;
    }

    public void Dispose()
    {
        DalamudService.DutyState.DutyStarted -= OnDutyStarted;
        DalamudService.DutyState.DutyWiped -= OnDutyWiped;
        DalamudService.DutyState.DutyCompleted -= OnDutyCompleted;
        DalamudService.Framework.Update -= OnFrameworkUpdate;
        TerritoryService.Instance.TerritoryChanged -= OnTerritoryChanged;

        GC.SuppressFinalize(this);
    }

    private void OnDutyStarted(object? sender, ushort e)
    {
        if (DalamudService.ClientState.IsPvPExcludingDen) return;
        if (!TerritoryIsValidDuty()) return;

        DalamudService.Log.Debug("Duty started");

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
        CheckPartyMembersConnectionStatus();
    }

    private void OnTerritoryChanged(TerritoryType? territoryType)
    {
        if (DalamudService.ClientState.IsPvPExcludingDen) return;

        bool isNextBoundByDuty = DalamudService.Condition[ConditionFlag.BoundByDuty] ||
                                 DalamudService.Condition[ConditionFlag.BoundByDuty56] ||
                                 DalamudService.Condition[ConditionFlag.BoundByDuty95];

        if (!isNextBoundByDuty) _partyMembers = [];

        // Consider duty failed if it wasn't completed before leaving duty
        if (_isBoundByDuty && !isNextBoundByDuty && !_dutyCompleted && !TerritoryIsValidDuty() && Configuration.Instance.EnableDutyFailedEvent)
        {
            AudioService.Instance.PlayRandomSoundFromCategory(AudioEvent.Failure);
        }

        _isBoundByDuty = isNextBoundByDuty;
    }

    private void CheckPartyMembersConnectionStatus()
    {
        if (!_isBoundByDuty) return;
        if (DalamudService.ClientState.IsPvPExcludingDen) return;
        if (DalamudService.PartyList.Length == 0) return;
        if (DalamudService.Condition[ConditionFlag.BetweenAreas]) return;

        foreach (IPartyMember partyMember in DalamudService.PartyList)
        {
            string? partyMemberName = XivUtility.GetFullPlayerName(partyMember);
            if (string.IsNullOrEmpty(partyMemberName)) continue;

            if (!_partyMembers.TryAdd(partyMemberName, partyMember.ObjectId)) continue;
            DalamudService.Log.Debug("Added party member: [{Id}] {Name}", partyMember.ObjectId, partyMemberName);
        }

        foreach (string partyMemberName in _partyMembers.Keys)
        {
            IPartyMember? partyMember = DalamudService.PartyList
                .FirstOrDefault(x => XivUtility.GetFullPlayerName(x) == partyMemberName);

            // Status is unchanged, continue
            if (_partyMembers[partyMemberName] == partyMember?.ObjectId) continue;

            // They likely left or disconnected
            if (partyMember?.ObjectId == 0)
            {
                DalamudService.Log.Debug("Party member disconnected: [{Id}] {Name}", _partyMembers[partyMemberName], partyMemberName);
                _partyMembers[partyMemberName] = 0;

                if (Configuration.Instance.EnableDutyPlayerDisconnectedEvent)
                {
                    AudioService.Instance.PlayRandomSoundFromCategory(AudioEvent.Disconnect);
                }
            }
            // They likely reconnected
            else if (partyMember != null)
            {
                _partyMembers[partyMemberName] = partyMember.ObjectId;
                DalamudService.Log.Debug("Party member reconnected: [{Id}] {Name}", _partyMembers[partyMemberName], partyMemberName);

                if (Configuration.Instance.EnableDutyPlayerReconnectedEvent)
                {
                    AudioService.Instance.PlayRandomSoundFromCategory(AudioEvent.Reconnect);
                }
            }
        }
    }

    private void PlayDutyCompleteAudio()
    {
        if (!TerritoryIsValidDuty()) return;

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

    private bool TerritoryIsValidDuty()
    {
        bool isNextInAllowedContentType = _allowedContentTypes.Contains(
            TerritoryService.Instance.CurrentTerritory?.ContentFinderCondition.Value.ContentType.Value.RowId);
        bool isInIgnoredTerritory = _ignoredTerritories.Contains(
            TerritoryService.Instance.CurrentTerritory?.RowId);
        bool isInIgnoredIntendedUse = _ignoredIntendedUses.Contains(
            TerritoryService.Instance.CurrentTerritory?.TerritoryIntendedUse.Value.RowId);

        return isNextInAllowedContentType && !isInIgnoredTerritory && !isInIgnoredIntendedUse;
    }
}