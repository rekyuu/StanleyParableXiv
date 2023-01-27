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
    private bool _isBoundByDuty = false;
    private bool _dutyStarted = false;
    private bool _dutyCompleted = false;
    private readonly Dictionary<uint, uint?> _partyStatus = new();
    
    public DutyEvent()
    {
        DalamudService.Framework.Update += OnFrameworkUpdate;
        DalamudService.GameNetwork.NetworkMessage += OnGameNetworkMessage;
    }
    
    public void Dispose()
    {
        DalamudService.Framework.Update -= OnFrameworkUpdate;
        DalamudService.GameNetwork.NetworkMessage -= OnGameNetworkMessage;
    }

    private void CheckIfPlayerIsBoundByDuty()
    {
        bool isNextBoundByDuty = DalamudService.Condition[ConditionFlag.BoundByDuty] ||
            DalamudService.Condition[ConditionFlag.BoundByDuty56] ||
            DalamudService.Condition[ConditionFlag.BoundByDuty95];

        // Ignore Island Sanctuary
        _currentTerritory = DalamudService.DataManager.Excel.GetSheet<TerritoryType>()?.GetRow(DalamudService.ClientState.TerritoryType);
        isNextBoundByDuty = isNextBoundByDuty && _currentTerritory?.TerritoryIntendedUse != 49;

        if (_isBoundByDuty && !isNextBoundByDuty && !_dutyCompleted) AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.Failure);
            
        _isBoundByDuty = isNextBoundByDuty;
    }

    private void CheckPartyMembers()
    {
        if (!_isBoundByDuty) return;
        if (DalamudService.PartyList.Length == 0) return;
        if (DalamudService.Condition[ConditionFlag.BetweenAreas]) return;

        uint[] partyStatusObjIds = _partyStatus.Keys.ToArray();
        uint[] partyListObjIds = DalamudService.PartyList.Select(x => x.ObjectId).ToArray();

        foreach (uint objId in partyStatusObjIds)
        {
            if (!partyListObjIds.Contains(objId)) _partyStatus.Remove(objId);
        }
        
        foreach (PartyMember partyMember in DalamudService.PartyList)
        {
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
            if (nextStatus == null) AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.Disconnect);
            // Assume the player reconnected
            else if (lastStatus == null) AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.Reconnect);
        }
    }

    private void OnFrameworkUpdate(Framework framework)
    {
        CheckIfPlayerIsBoundByDuty();
        CheckPartyMembers();
    }

    private unsafe void OnGameNetworkMessage(IntPtr dataPtr, ushort opCode, uint sourceActorId, uint targetActorId,
        NetworkMessageDirection direction)
    {
        if (opCode != DalamudService.DataManager.ServerOpCodes["ActorControlSelf"]) return;
        
        ushort cat = *(ushort*)(dataPtr + 0x00);
        uint updateType = *(uint*)(dataPtr + 0x08);
        
        // PluginLog.Verbose("OpCode = {OpCode}, Cat = 0x{Cat:X}, UpdateType = 0x{UpdateType:X}", opCode, cat, updateType);

        switch (cat)
        {
            // Encounter Start
            case 0x6D when updateType == 0x40000001:
                _dutyStarted = true;
                _dutyCompleted = false;
                
                AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.EncounterStart);
                break;
            // Possible PvP complete
            case 0x6D when updateType == 0x40000002:
                break;
            // Encounter Complete
            case 0x6D when updateType == 0x40000003:
                _dutyStarted = false;
                _dutyCompleted = true;
                
                Task.Delay(1000).ContinueWith(_ =>
                {
                    AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.EncounterComplete);
                });
                break;
            // Start PvP Countdown 
            case 0x6D when updateType == 0x40000004:
                _dutyStarted = true;
                _dutyCompleted = false;
                
                AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.CountdownStart);
                Task.Delay(20_000).ContinueWith(_ =>
                {
                    AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.Countdown10);
                });
                break;
            // Party Wipe
            case 0x6D when updateType == 0x40000005:
                Task.Delay(1000).ContinueWith(_ =>
                {
                    AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.Wipe);
                });
                break;
            // Encounter Recommence
            case 0x6D when updateType == 0x40000006:
                break;
            // Possible PvP complete 
            case 0x6D when updateType == 0x40000007:
                break;
            // PvP win
            case 0x355 when updateType == 0x1F4:
                _dutyStarted = false;
                _dutyCompleted = true;
                
                Task.Delay(3000).ContinueWith(_ =>
                {
                    AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.PvpWin);
                });
                break;
            // PvP loss
            case 0x355 when updateType == 0xFA:
                _dutyStarted = false;
                _dutyCompleted = true;
                
                Task.Delay(3000).ContinueWith(_ =>
                {
                    AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.Failure);
                });
                break;
        }
    }
}