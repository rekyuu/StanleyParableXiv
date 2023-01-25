using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Logging;
using StanleyParableXiv.Services;
using StanleyParableXiv.Utility;

namespace StanleyParableXiv.Events;

public class PvpEvent : IDisposable
{
    private string _playerName;
    private Dictionary<string, PlayerCharacter> _partyMembers = new();

    private bool _firstBlood = false;
    
    private Dictionary<string, uint> _killStreaks = new();
    private Dictionary<string, uint> _multikills = new();
    private Dictionary<string, DateTimeOffset> _multikillCooldowns = new();

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
    }

    private void ResetKillCounts()
    {
        PluginLog.Debug("Clearing PvP kill streak stats");
        
        _partyMembers = new Dictionary<string, PlayerCharacter>();
        
        _firstBlood = false;
        
        _killStreaks = new Dictionary<string, uint>();
        _multikills = new Dictionary<string, uint>();
        _multikillCooldowns = new Dictionary<string, DateTimeOffset>();
    }

    private void OnEnterPvP()
    {
        ResetKillCounts();

        foreach (PartyMember partyMember in DalamudService.PartyList)
        {
            string name = partyMember.Name.TextValue;
            _partyMembers[name] = DalamudUtility.GetPlayerCharacterFromPartyMember(partyMember)!;
            
            PluginLog.Debug("{Name} is added to the PvP party", name);
        }
        
        Task.Delay(5000).ContinueWith(_ =>
        {
            AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.PvpPrepare);
        });
    }

    private void OnLeavePvp()
    {
        ResetKillCounts();
    }

    private void OnChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        if (type is not ((XivChatType)4922 or (XivChatType)2874)) return;
        
        Payload[] playerPayloads = message.Payloads.Where(x => x.Type == PayloadType.Player).ToArray();
        PluginLog.Verbose("[{Type}] {Message}", type, message);

        if (playerPayloads.Length == 0) return;

        DateTimeOffset killTime = DateTimeOffset.Now;
        bool isDead = DalamudService.ClientState.LocalPlayer!.IsDead;

        string killerName;
        string killedName;
        
        switch (type)
        {
            case (XivChatType)4922 when playerPayloads.Length == 2:
            {
                PlayerPayload player1 = (PlayerPayload)playerPayloads[0];
                PlayerPayload player2 = (PlayerPayload)playerPayloads[1];

                string player1Name = player1.PlayerName;
                string player2Name = player2.PlayerName;

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
                else
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

        PluginLog.Verbose("{KillerName} -> {KilledName}", killerName, killedName);

        if (!_firstBlood)
        {
            _firstBlood = true;
            
            DalamudService.ChatGui.Print($"{killerName} drew first blood by killing {killedName}!");
            AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.FirstBlood);
        }

        if (!_multikillCooldowns.ContainsKey(killerName) || _multikillCooldowns[killerName] <= killTime)
        {
            _multikills[killerName] = 1;
        }
        else if (_multikillCooldowns[killerName] > killTime)
        {
            _multikills[killerName] += 1;
        }
                
        _multikillCooldowns[killerName] = DateTimeOffset.Now + TimeSpan.FromSeconds(15);
        
        PluginLog.Debug("{KillerName} multikill streak: {Count}", killerName, _multikills[killerName]);

        switch (_multikills[killerName])
        {
            case 0:
            case 1:
                break;
            case 2:
                DalamudService.ChatGui.Print($"{killerName} got a double kill!");
                AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.Multikill2);
                break;
            case 3:
                DalamudService.ChatGui.Print($"{killerName} has a TRIPLE kill!");
                AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.Multikill3);
                break;
            case 4:
                DalamudService.ChatGui.Print($"{killerName} earned an ULTRA KILL!");
                AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.Multikill4);
                break;
            default:
                DalamudService.ChatGui.Print($"{killerName} IS ON A RAMPAGE!!");
                AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.Multikill5);
                break;
        }

        uint killedLastStreak = 0;
        if (_killStreaks.ContainsKey(killedName)) killedLastStreak = _killStreaks[killedName];
        
        _killStreaks[killedName] = 0;
        if (!_killStreaks.ContainsKey(killerName)) _killStreaks[killerName] = 1;
        else _killStreaks[killerName] += 1;
        
        PluginLog.Debug("{KillerName} kill streak: {Count}", killerName, _killStreaks[killerName]);

        switch (_killStreaks[killerName])
        {
            case 0:
            case 1:
            case 2:
                break;
            case 3:
                DalamudService.ChatGui.Print($"{killerName} is on a killing spree!");
                AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.KillStreak3);
                break;
            case 4:
                DalamudService.ChatGui.Print($"{killerName} is dominating!");
                AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.KillStreak4);
                break;
            case 5:
                DalamudService.ChatGui.Print($"{killerName} is on a mega kill streak!");
                AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.KillStreak5);
                break;
            case 6:
                DalamudService.ChatGui.Print($"{killerName} is unstoppable!");
                AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.KillStreak6);
                break;
            case 7:
                DalamudService.ChatGui.Print($"{killerName} is wicked sick!");
                AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.KillStreak7);
                break;
            case 8:
                DalamudService.ChatGui.Print($"{killerName} is on a monster kill streak!");
                AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.KillStreak8);
                break;
            case 9:
                DalamudService.ChatGui.Print($"{killerName} is GODLIKE!");
                AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.KillStreak9);
                break;
            default:
                DalamudService.ChatGui.Print($"{killerName} is beyond GODLIKE, somebody stop them!!");
                AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.KillStreak10);
                break;
        }
        
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