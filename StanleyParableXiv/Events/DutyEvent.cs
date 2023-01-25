using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dalamud.Game;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Network;
using Dalamud.Logging;
using Lumina.Excel.GeneratedSheets;
using StanleyParableXiv.Services;

namespace StanleyParableXiv.Events;

public class DutyEvent : IDisposable
{

    private TerritoryType? _currentTerritory;
    private bool _isBoundByDuty = false;
    private bool _victoryPlayed = false;
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
        bool isBoundByDuty = DalamudService.Condition[ConditionFlag.BoundByDuty] ||
                             DalamudService.Condition[ConditionFlag.BoundByDuty56] ||
                             DalamudService.Condition[ConditionFlag.BoundByDuty95];

        // Ignore Island Sanctuary
        _currentTerritory = DalamudService.DataManager.Excel.GetSheet<TerritoryType>()?.GetRow(DalamudService.ClientState.TerritoryType);
        isBoundByDuty = isBoundByDuty && _currentTerritory?.TerritoryIntendedUse != 49;

        if (_isBoundByDuty && !isBoundByDuty && !_victoryPlayed) AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.Failure);
            
        _isBoundByDuty = isBoundByDuty;
    }

    private void CheckPartyMembers()
    {
        if (!_isBoundByDuty) return;
        if (DalamudService.PartyList.Length == 0) return;
        if (DalamudService.Condition[ConditionFlag.BetweenAreas]) return;
        
        foreach (PartyMember partyMember in DalamudService.PartyList)
        {
            if (_currentTerritory != partyMember.Territory.GameData) continue;
            
            uint objId = partyMember.ObjectId;
            if (!_partyStatus.ContainsKey(objId)) _partyStatus[objId] = null;
            
            uint? nextStatus = null;
            uint? lastStatus = _partyStatus[objId];
            
            GameObject? obj = DalamudService.ObjectTable.SearchById(objId);
            
            if (obj != null && obj.GetType() != typeof(PlayerCharacter))
            {
                PluginLog.Warning("Party member parsed is incorrect object type: {Type}", obj.GetType());
            }
            else if (obj != null && obj.GetType() == typeof(PlayerCharacter))
            {
                PlayerCharacter player = (obj as PlayerCharacter)!;
                OnlineStatus? onlineStatus = player.OnlineStatus.GameData;
            
                if (onlineStatus != null) nextStatus = onlineStatus.RowId;
            }

            if (nextStatus == lastStatus) continue;
            
            _partyStatus[objId] = nextStatus;
            PluginLog.Debug("Party member status changed = {PlayerId} {PlayerName}, {PreviousOnlineStatus} -> {NextOnlineStatus}", 
                objId, partyMember.Name, lastStatus!, nextStatus!);

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

        switch (cat)
        {
            // Encounter Start
            case 0x6D when updateType == 0x40000001:
                _victoryPlayed = false;
                AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.EncounterStart);
                break;
            // Encounter Complete
            case 0x6D when updateType == 0x40000003:
                Task.Delay(1000).ContinueWith(_ =>
                {
                    _victoryPlayed = true;
                    AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.EncounterComplete);
                });
                break;
        }
    }
}