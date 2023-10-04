using System;
using System.Linq;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using StanleyParableXiv.Services;

namespace StanleyParableXiv.Events;

public class AfkEvent : IDisposable
{
    private delegate long AfkTimerHookDelegate(nint a1, float a2);

    [Signature("48 8B C4 48 89 58 18 48 89 70 20 55 57 41 55", DetourName = nameof(OnAfkTimerHook))]
    private readonly Hook<AfkTimerHookDelegate>? _afkTimerHook = null;
    
    private IntPtr _afkTimerBaseAddress = IntPtr.Zero;
    private TimerService? _afkTimerService;
    private bool _afkPlayed = false;
    private bool _isInCutscene = false;
    
    /// <summary>
    /// Fires an event when a player is AFK for a certain time.
    /// Referenced from https://github.com/NightmareXIV/AntiAfkKick
    /// </summary>
    public AfkEvent()
    {
        DalamudService.GameInteropProvider.InitializeFromAttributes(this);
        _afkTimerHook?.Enable();
        
        DalamudService.Framework.Update += OnFrameworkUpdate;
    }
    
    public void Dispose()
    {
        DalamudService.Framework.Update -= OnFrameworkUpdate;
        
        _afkTimerHook?.Dispose();
        _afkTimerService?.Stop();
        _afkTimerService?.Dispose();
        
        GC.SuppressFinalize(this);
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        _isInCutscene = DalamudService.Condition[ConditionFlag.OccupiedInCutSceneEvent];
    }

    private unsafe long OnAfkTimerHook(nint a1, float a2)
    {
        _afkTimerBaseAddress = a1;
        
        _afkTimerService = new TimerService(30, () =>
        {
            float* afkTimer1 = (float*)(_afkTimerBaseAddress + 20);
            float* afkTimer2 = (float*)(_afkTimerBaseAddress + 24);
            float* afkTimer3 = (float*)(_afkTimerBaseAddress + 28);
            
            DalamudService.Log.Verbose($"AFK Timers = {*afkTimer1}/{*afkTimer2}/{*afkTimer3}");
                
            // Not really sure what each timer is for, so we'll just pick the longest.
            // Skip playing if they're in a cutscene.
            if (new[] { *afkTimer1, *afkTimer2, *afkTimer3 }.Max() > Configuration.Instance.AfkEventTimeframe)
            {
                if (Configuration.Instance.EnableAfkEvent && !_isInCutscene && !_afkPlayed) AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.Afk);
                _afkPlayed = true;
            }
            else _afkPlayed = false;
        });
            
        _afkTimerService.Start();
        _afkTimerHook?.Dispose();

        return 0;
    }
}