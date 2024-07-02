using System;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using StanleyParableXiv.Services;

namespace StanleyParableXiv.Events;

public class AfkEvent : IDisposable
{
    private bool _afkPlayed = false;
    
    public AfkEvent()
    {
        DalamudService.Framework.Update += OnFrameworkUpdate;
    }
    
    public void Dispose()
    {
        DalamudService.Framework.Update -= OnFrameworkUpdate;
        GC.SuppressFinalize(this);
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        bool isAfk = DalamudService.ClientState.LocalPlayer?.OnlineStatus.Id == 17;
        bool isInCutscene = DalamudService.Condition[ConditionFlag.OccupiedInCutSceneEvent];

        if (isAfk && !isInCutscene && Configuration.Instance.EnableAfkEvent)
        {
            if (_afkPlayed) return;
            
            AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.Afk);
            _afkPlayed = true;
        }
        else _afkPlayed = false;
    }
}