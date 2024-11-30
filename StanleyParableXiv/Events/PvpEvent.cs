using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Game.Network;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Lumina.Excel.Sheets;
using StanleyParableXiv.Services;

namespace StanleyParableXiv.Events;

public class PvpEvent : IDisposable
{
    private readonly string _playerName;

    private bool _firstBlood = false;

    private Dictionary<string, uint> _killStreaks = new();
    private Dictionary<string, uint> _multikills = new();
    private Dictionary<string, DateTimeOffset> _multikillCooldowns = new();

    /// <summary>
    /// Fires on specific PvP related events.
    /// </summary>
    public PvpEvent()
    {
        _playerName = DalamudService.ClientState.LocalPlayer?.Name.TextValue!;

        DalamudService.ClientState.EnterPvP += OnEnterPvP;
        DalamudService.ClientState.LeavePvP += OnLeavePvp;
        DalamudService.ChatGui.ChatMessage += OnChatMessage;
        DalamudService.GameNetwork.NetworkMessage += OnGameNetworkMessage;
        TerritoryService.Instance.TerritoryChanged += OnTerritoryChanged;
    }

    public void Dispose()
    {
        DalamudService.ClientState.EnterPvP -= OnEnterPvP;
        DalamudService.ClientState.LeavePvP -= OnLeavePvp;
        DalamudService.ChatGui.ChatMessage -= OnChatMessage;
        DalamudService.GameNetwork.NetworkMessage -= OnGameNetworkMessage;
        TerritoryService.Instance.TerritoryChanged -= OnTerritoryChanged;

        GC.SuppressFinalize(this);
    }

    private static void OnEnterPvP()
    {
        if (TerritoryService.Instance.CurrentTerritory == null) return;
        if (!DalamudService.ClientState.IsPvPExcludingDen) return;

        DalamudService.Log.Debug("Entering PvP");

        DalamudService.Log.Debug("Current PvP territory: {Name}, RowId: {RowId}",
            TerritoryService.Instance.CurrentTerritory?.Name.ExtractText() ?? string.Empty,
            TerritoryService.Instance.CurrentTerritory?.RowId ?? 0);

        if (Configuration.Instance.EnablePvpPrepareEvent)
        {
            Task.Delay(5000).ContinueWith(_ =>
            {
                AudioService.Instance.PlayRandomSoundFromCategory(AudioEvent.PvpPrepare);
            });
        }
    }

    private void OnLeavePvp()
    {
        DalamudService.Log.Debug("Leaving PvP");
        ResetPvp();
    }

    private void OnChatMessage(XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        if (!DalamudService.ClientState.IsPvPExcludingDen) return;
        if (type is not ((XivChatType)4922 or (XivChatType)2874)) return;
        DalamudService.Log.Verbose("[{Type}] {Message}", type, message);
        
        PlayerPayload?[] playerPayloads = message.Payloads
            .Where(x => x.Type == PayloadType.Player)
            .Select(x => x as PlayerPayload)
            .ToArray();

        if (playerPayloads.Length == 0) return;

        DalamudService.Log.Verbose("Player payloads: {Payloads}", playerPayloads.Select(x => x?.PlayerName));

        // TextPayload?[] rawPayloads = message.Payloads
        //     .Where(x => x.Type == PayloadType.RawText)
        //     .Select(x => x as TextPayload)
        //     .ToArray();
        // DalamudService.Log.Verbose("Text payloads: {Payloads}", rawPayloads.Select(x => x?.Text));

        DateTimeOffset killTime = DateTimeOffset.Now;
        bool chatLogKillStreaks = Configuration.Instance.EnablePvpChatEvent;

        string? killerName;
        string? killedName;

        switch (type)
        {
            case (XivChatType)4922 when playerPayloads.Length == 2:
            {
                PlayerPayload? player1 = playerPayloads[0];
                PlayerPayload? player2 = playerPayloads[1];

                if (player1 == null || player2 == null) return;

                if (IsProbablyDead(player2))
                {
                    killerName = $"{player1.PlayerName}@{player1.World.Value.Name}";
                    killedName = $"{player2.PlayerName}@{player2.World.Value.Name}";
                }
                else
                {
                    killerName = $"{player2.PlayerName}@{player2.World.Value.Name}";
                    killedName = $"{player1.PlayerName}@{player1.World.Value.Name}";
                }

                break;
            }
            case (XivChatType)2874 when playerPayloads.Length == 1:
            {
                // Determine who killed who depending on if you died or not.
                if (DalamudService.ClientState.LocalPlayer?.IsDead == true)
                {
                    killerName = playerPayloads[0]?.PlayerName;
                    killedName = _playerName;
                }
                else
                {
                    killerName = _playerName;
                    killedName = playerPayloads[0]?.PlayerName;
                }

                break;
            }
            default:
                return;
        }

        if (string.IsNullOrEmpty(killerName) || string.IsNullOrEmpty(killedName)) return;

        DalamudService.Log.Verbose("{KillerName} -> {KilledName}", killerName, killedName);

        // Play on the first kill of the match.
        if (!_firstBlood)
        {
            _firstBlood = true;

            if (chatLogKillStreaks) DalamudService.ChatGui.Print($"{killerName} drew first blood by killing {killedName}!");
            if (Configuration.Instance.EnablePvpFirstBloodEvent)
            {
                AudioService.Instance.PlayRandomSoundFromCategory(AudioEvent.FirstBlood);
            }
        }

        // Update multikills.
        // Multikills are performed within a 15-second window. The timer is refreshed on kill.
        if (!_multikillCooldowns.TryGetValue(killerName, out DateTimeOffset lastKillTime) || lastKillTime <= killTime)
        {
            _multikills[killerName] = 1;
        }
        else if (lastKillTime > killTime)
        {
            _multikills[killerName] += 1;
        }

        _multikillCooldowns[killerName] = DateTimeOffset.Now + TimeSpan.FromSeconds(15);

        DalamudService.Log.Debug("{KillerName} multikill streak: {Count}", killerName, _multikills[killerName]);

        bool multikills = Configuration.Instance.EnablePvpMultikillsEvent;

        switch (_multikills[killerName])
        {
            case 0:
            case 1:
                break;
            case 2:
                if (chatLogKillStreaks) DalamudService.ChatGui.Print($"{killerName} got a double kill!");
                if (multikills) AudioService.Instance.PlayRandomSoundFromCategory(AudioEvent.Multikill2);
                break;
            case 3:
                if (chatLogKillStreaks) DalamudService.ChatGui.Print($"{killerName} has a TRIPLE kill!");
                if (multikills) AudioService.Instance.PlayRandomSoundFromCategory(AudioEvent.Multikill3);
                break;
            case 4:
                if (chatLogKillStreaks) DalamudService.ChatGui.Print($"{killerName} earned an ULTRA KILL!");
                if (multikills) AudioService.Instance.PlayRandomSoundFromCategory(AudioEvent.Multikill4);
                break;
            default:
                if (chatLogKillStreaks) DalamudService.ChatGui.Print($"{killerName} IS ON A RAMPAGE!!");
                if (multikills) AudioService.Instance.PlayRandomSoundFromCategory(AudioEvent.Multikill5);
                break;
        }

        // Update kill streaks.
        // Resets to 0 on death.
        if (!_killStreaks.TryGetValue(killedName, out uint killedLastStreak)) killedLastStreak = 0;

        _killStreaks[killedName] = 0;
        if (!_killStreaks.TryAdd(killerName, 1)) _killStreaks[killerName] += 1;

        bool playKillStreaks = Configuration.Instance.EnablePvpKillStreaksEvent;

        DalamudService.Log.Debug("{KillerName} kill streak: {Count}", killerName, _killStreaks[killerName]);

        switch (_killStreaks[killerName])
        {
            case 0:
            case 1:
            case 2:
                break;
            case 3:
                if (chatLogKillStreaks) DalamudService.ChatGui.Print($"{killerName} is on a killing spree!");
                if (playKillStreaks) AudioService.Instance.PlayRandomSoundFromCategory(AudioEvent.KillStreak3);
                break;
            case 4:
                if (chatLogKillStreaks) DalamudService.ChatGui.Print($"{killerName} is dominating!");
                if (playKillStreaks) AudioService.Instance.PlayRandomSoundFromCategory(AudioEvent.KillStreak4);
                break;
            case 5:
                if (chatLogKillStreaks) DalamudService.ChatGui.Print($"{killerName} is on a mega kill streak!");
                if (playKillStreaks) AudioService.Instance.PlayRandomSoundFromCategory(AudioEvent.KillStreak5);
                break;
            case 6:
                if (chatLogKillStreaks) DalamudService.ChatGui.Print($"{killerName} is unstoppable!");
                if (playKillStreaks) AudioService.Instance.PlayRandomSoundFromCategory(AudioEvent.KillStreak6);
                break;
            case 7:
                if (chatLogKillStreaks) DalamudService.ChatGui.Print($"{killerName} is wicked sick!");
                if (playKillStreaks) AudioService.Instance.PlayRandomSoundFromCategory(AudioEvent.KillStreak7);
                break;
            case 8:
                if (chatLogKillStreaks) DalamudService.ChatGui.Print($"{killerName} is on a monster kill streak!");
                if (playKillStreaks) AudioService.Instance.PlayRandomSoundFromCategory(AudioEvent.KillStreak8);
                break;
            case 9:
                if (chatLogKillStreaks) DalamudService.ChatGui.Print($"{killerName} is GODLIKE!");
                if (playKillStreaks) AudioService.Instance.PlayRandomSoundFromCategory(AudioEvent.KillStreak9);
                break;
            default:
                if (chatLogKillStreaks) DalamudService.ChatGui.Print($"{killerName} is beyond GODLIKE, somebody stop them!!");
                if (playKillStreaks) AudioService.Instance.PlayRandomSoundFromCategory(AudioEvent.KillStreak10);
                break;
        }

        // Post a chat message if someone ended a kill streak.
        if (!chatLogKillStreaks) return;
        
        switch (killedLastStreak)
        {
            case 0:
            case 1:
            case 2:
                break;
            case 3:
                DalamudService.ChatGui.Print($"{killerName} ended {killedName}'s killing spree!");
                break;
            case 4:
                DalamudService.ChatGui.Print($"{killerName} ended {killedName}'s dominating streak!");
                break;
            case 5:
                DalamudService.ChatGui.Print($"{killerName} ended {killedName}'s mega kill streak!");
                break;
            case 6:
                DalamudService.ChatGui.Print($"{killerName} ended {killedName}'s unstoppable streak!");
                break;
            case 7:
                DalamudService.ChatGui.Print($"{killerName} ended {killedName}'s wicked sick streak!");
                break;
            case 8:
                DalamudService.ChatGui.Print($"{killerName} ended {killedName}'s monster kill streak!");
                break;
            case 9:
                DalamudService.ChatGui.Print($"{killerName} ended {killedName}'s GODLIKE streak!");
                break;
            default:
                DalamudService.ChatGui.Print($"{killerName} ended {killedName}'s beyond GODLIKE streak!");
                break;
        }
    }

    private static unsafe void OnGameNetworkMessage(IntPtr dataPtr, ushort opCode, uint sourceActorId, uint targetActorId,
        NetworkMessageDirection direction)
    {
        ushort cat = *(ushort*)(dataPtr + 0x00);
        uint updateType = *(uint*)(dataPtr + 0x08);

        switch (cat)
        {
            // Start PvP Countdown
            case 0x6D when updateType == 0x40000004:
                if (Configuration.Instance.EnablePvpCountdownStartEvent)
                {
                    AudioService.Instance.PlayRandomSoundFromCategory(AudioEvent.CountdownStart);
                }

                if (Configuration.Instance.EnablePvpCountdown10Event)
                {
                    Task.Delay(20_000).ContinueWith(_ =>
                    {
                        AudioService.Instance.PlayRandomSoundFromCategory(AudioEvent.Countdown10);
                    });
                }

                break;
            // PvP win
            case 0x355 when updateType == 0x1F4:
                if (Configuration.Instance.EnablePvpWinEvent)
                {
                    Task.Delay(3000).ContinueWith(_ =>
                    {
                        AudioService.Instance.PlayRandomSoundFromCategory(AudioEvent.PvpWin);
                    });
                }

                break;
            // PvP loss
            case 0x355 when updateType == 0xFA:
                if (Configuration.Instance.EnablePvpLossEvent)
                {
                    Task.Delay(3000).ContinueWith(_ =>
                    {
                        AudioService.Instance.PlayRandomSoundFromCategory(AudioEvent.Failure);
                    });
                }

                break;
        }
    }

    private void OnTerritoryChanged(TerritoryType? territoryType)
    {
        if (DalamudService.ClientState.IsPvPExcludingDen) OnEnterPvP();
        else if (DalamudService.ClientState.IsPvP) OnLeavePvp();
    }

    private static bool IsProbablyDead(PlayerPayload playerPayload)
    {
        return DalamudService.PartyList.Any(
            player =>
                player.Name.TextValue == playerPayload.PlayerName &&
                player.World.RowId == playerPayload.World.RowId &&
                player.CurrentHP <= 0);
    }

    private void ResetPvp()
    {
        DalamudService.Log.Debug("Resetting PvP");

        _firstBlood = false;

        _killStreaks = new Dictionary<string, uint>();
        _multikills = new Dictionary<string, uint>();
        _multikillCooldowns = new Dictionary<string, DateTimeOffset>();
    }
}