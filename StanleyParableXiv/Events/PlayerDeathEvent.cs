using System;
using Dalamud.Game;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using StanleyParableXiv.Services;

namespace StanleyParableXiv.Events;

public class PlayerDeathEvent : IDisposable
{
    private bool _isDead = false;
    
    public PlayerDeathEvent()
    {
        DalamudService.Framework.Update += OnFrameworkUpdate;
    }

    public void Dispose()
    {
        DalamudService.Framework.Update -= OnFrameworkUpdate;
    }
    
    private void OnFrameworkUpdate(Framework framework)
    {
        PlayerCharacter? player = DalamudService.ClientState.LocalPlayer;
        if (player == null) return;

        bool isDeadNext = player.IsDead;

        if (_isDead && !isDeadNext && !DalamudService.Condition[ConditionFlag.BetweenAreas])
        {
            AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.Respawn);    
        }
        
        _isDead = isDeadNext;
    }
}