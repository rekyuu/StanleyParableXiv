using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using StanleyParableXiv.Services;
using StanleyParableXiv.Utility;

namespace StanleyParableXiv.Events;

public class PvpEvent : IDisposable
{
    private readonly string _playerName;
    private Dictionary<string, IPlayerCharacter> _partyMembers = new();

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
    }

    public void Dispose()
    {
        DalamudService.ClientState.EnterPvP -= OnEnterPvP;
        DalamudService.ClientState.LeavePvP -= OnLeavePvp;
        DalamudService.ChatGui.ChatMessage -= OnChatMessage;
        
        GC.SuppressFinalize(this);
    }

    private void ResetKillCounts()
    {
        DalamudService.Log.Debug("Clearing PvP kill streak stats");
        
        _partyMembers = new Dictionary<string, IPlayerCharacter>();
        
        _firstBlood = false;
        
        _killStreaks = new Dictionary<string, uint>();
        _multikills = new Dictionary<string, uint>();
        _multikillCooldowns = new Dictionary<string, DateTimeOffset>();
    }

    private void OnEnterPvP()
    {
        ResetKillCounts();

        // This needs to be done to determine who killed who
        foreach (IPartyMember partyMember in DalamudService.PartyList)
        {
            string name = partyMember.Name.TextValue;
            _partyMembers[name] = DalamudUtility.GetPlayerCharacterFromPartyMember(partyMember)!;
            
            DalamudService.Log.Debug("{Name} is added to the PvP party", name);
        }

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
        ResetKillCounts();
    }

    private void OnChatMessage(XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool ishandled)
    {
        if (type is not ((XivChatType)4922 or (XivChatType)2874)) return;
        
        Payload[] playerPayloads = message.Payloads.Where(x => x.Type == PayloadType.Player).ToArray();
        DalamudService.Log.Verbose("[{Type}] {Message}", type, message);

        if (playerPayloads.Length == 0) return;

        DateTimeOffset killTime = DateTimeOffset.Now;
        bool isDead = DalamudService.ClientState.LocalPlayer!.IsDead;
        bool chat = Configuration.Instance.EnablePvpChatEvent;

        string killerName = "";
        string killedName = "";
        
        switch (type)
        {
            case (XivChatType)4922 when playerPayloads.Length == 2:
            {
                PlayerPayload player1 = (PlayerPayload)playerPayloads[0];
                PlayerPayload player2 = (PlayerPayload)playerPayloads[1];

                string player1Name = player1.PlayerName;
                string player2Name = player2.PlayerName;

                // Can only seem to determine who is dead when they're in your party, 
                // so this insane block of code determines who killed who based on who
                // is still alive in the party.
                if (_partyMembers.ContainsKey(player1Name))
                {
                    if (_partyMembers[player1Name].IsDead)
                    {
                        killerName = player2Name;
                        killedName = player1Name;
                    }
                    else
                    {
                        killerName = player1Name;
                        killedName = player2Name;
                    }
                }
                else if (_partyMembers.ContainsKey(player2Name))
                {
                    if (_partyMembers[player2Name].IsDead)
                    {
                        killerName = player1Name;
                        killedName = player2Name;
                    }
                    else
                    {
                        killerName = player2Name;
                        killedName = player1Name;
                    }
                }
            
                break;
            }
            case (XivChatType)2874 when playerPayloads.Length == 1:
            {
                // Determine who killed who depending on if you died or not.
                if (isDead)
                {
                    PlayerPayload killer = (PlayerPayload)playerPayloads[0];
                    killerName = killer.PlayerName;
                    killedName = _playerName;
                }
                else
                {
                    PlayerPayload killed = (PlayerPayload)playerPayloads[0];
                    killedName = killed.PlayerName;
                    killerName = _playerName;
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

            if (chat) DalamudService.ChatGui.Print($"{killerName} drew first blood by killing {killedName}!");
            if (Configuration.Instance.EnablePvpFirstBloodEvent)
            {
                AudioService.Instance.PlayRandomSoundFromCategory(AudioEvent.FirstBlood);
            }
        }

        // Update multikills.
        // Multikills are performed within a 15 second window. The timer is refreshed on kill. 
        if (!_multikillCooldowns.ContainsKey(killerName) || _multikillCooldowns[killerName] <= killTime)
        {
            _multikills[killerName] = 1;
        }
        else if (_multikillCooldowns[killerName] > killTime)
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
                if (chat) DalamudService.ChatGui.Print($"{killerName} got a double kill!");
                if (multikills) AudioService.Instance.PlayRandomSoundFromCategory(AudioEvent.Multikill2);
                break;
            case 3:
                if (chat) DalamudService.ChatGui.Print($"{killerName} has a TRIPLE kill!");
                if (multikills) AudioService.Instance.PlayRandomSoundFromCategory(AudioEvent.Multikill3);
                break;
            case 4:
                if (chat) DalamudService.ChatGui.Print($"{killerName} earned an ULTRA KILL!");
                if (multikills) AudioService.Instance.PlayRandomSoundFromCategory(AudioEvent.Multikill4);
                break;
            default:
                if (chat) DalamudService.ChatGui.Print($"{killerName} IS ON A RAMPAGE!!");
                if (multikills) AudioService.Instance.PlayRandomSoundFromCategory(AudioEvent.Multikill5);
                break;
        }
        
        // Update kill streaks.
        // Resets to 0 on death.
        uint killedLastStreak = 0;
        if (_killStreaks.ContainsKey(killedName)) killedLastStreak = _killStreaks[killedName];
        
        _killStreaks[killedName] = 0;
        if (!_killStreaks.ContainsKey(killerName)) _killStreaks[killerName] = 1;
        else _killStreaks[killerName] += 1;
        
        bool killStreaks = Configuration.Instance.EnablePvpKillStreaksEvent;
        
        DalamudService.Log.Debug("{KillerName} kill streak: {Count}", killerName, _killStreaks[killerName]);
        
        switch (_killStreaks[killerName])
        {
            case 0:
            case 1:
            case 2:
                break;
            case 3:
                if (chat) DalamudService.ChatGui.Print($"{killerName} is on a killing spree!");
                if (killStreaks) AudioService.Instance.PlayRandomSoundFromCategory(AudioEvent.KillStreak3);
                break;
            case 4:
                if (chat) DalamudService.ChatGui.Print($"{killerName} is dominating!");
                if (killStreaks) AudioService.Instance.PlayRandomSoundFromCategory(AudioEvent.KillStreak4);
                break;
            case 5:
                if (chat) DalamudService.ChatGui.Print($"{killerName} is on a mega kill streak!");
                if (killStreaks) AudioService.Instance.PlayRandomSoundFromCategory(AudioEvent.KillStreak5);
                break;
            case 6:
                if (chat) DalamudService.ChatGui.Print($"{killerName} is unstoppable!");
                if (killStreaks) AudioService.Instance.PlayRandomSoundFromCategory(AudioEvent.KillStreak6);
                break;
            case 7:
                if (chat) DalamudService.ChatGui.Print($"{killerName} is wicked sick!");
                if (killStreaks) AudioService.Instance.PlayRandomSoundFromCategory(AudioEvent.KillStreak7);
                break;
            case 8:
                if (chat) DalamudService.ChatGui.Print($"{killerName} is on a monster kill streak!");
                if (killStreaks) AudioService.Instance.PlayRandomSoundFromCategory(AudioEvent.KillStreak8);
                break;
            case 9:
                if (chat) DalamudService.ChatGui.Print($"{killerName} is GODLIKE!");
                if (killStreaks) AudioService.Instance.PlayRandomSoundFromCategory(AudioEvent.KillStreak9);
                break;
            default:
                if (chat) DalamudService.ChatGui.Print($"{killerName} is beyond GODLIKE, somebody stop them!!");
                if (killStreaks) AudioService.Instance.PlayRandomSoundFromCategory(AudioEvent.KillStreak10);
                break;
        }

        // Post a chat message if someone ended a kill streak.
        if (!chat) return;
        
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
}