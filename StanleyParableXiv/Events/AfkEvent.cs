using System;
using System.Linq;
using Dalamud.Hooking;
using Dalamud.Logging;
using StanleyParableXiv.Services;

namespace StanleyParableXiv.Events;

public class AfkEvent : IDisposable
{
    private delegate long AfkTimerHookDelegate(IntPtr a1, float a2);
    private IntPtr _afkTimerBaseAddress = IntPtr.Zero;
    private readonly Hook<AfkTimerHookDelegate>? _afkTimerHook;
    private TimerService? _afkTimerService;
    private bool _afkPlayed = false;
    
    public AfkEvent()
    {
        _afkTimerHook = Hook<AfkTimerHookDelegate>.FromAddress(
            DalamudService.SigScanner.ScanText("48 8B C4 48 89 58 18 48 89 70 20 55 57 41 55"),
            OnAfkTimerHook);
        _afkTimerHook.Enable();
    }
    
    public void Dispose()
    {
        DisposeAfkTimerHook();
        _afkTimerService?.Stop();
        _afkTimerService?.Dispose();
    }

    private unsafe long OnAfkTimerHook(IntPtr a1, float a2)
    {
        _afkTimerBaseAddress = a1;
        PluginLog.Debug($"Found AFK timer base address: {_afkTimerBaseAddress:X16}");

        _afkTimerService = new TimerService(30, () =>
        {
            if (_afkPlayed) return;
                
            float* afkTimer1 = (float*)(_afkTimerBaseAddress + 20);
            float* afkTimer2 = (float*)(_afkTimerBaseAddress + 24);
            float* afkTimer3 = (float*)(_afkTimerBaseAddress + 28);
            
            PluginLog.Verbose($"AFK Timers = {*afkTimer1}/{*afkTimer2}/{*afkTimer3}");
                
            if (new[] { *afkTimer1, *afkTimer2, *afkTimer3 }.Max() > 300f)
            {
                AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.Afk);
                _afkPlayed = true;
            }
            else _afkPlayed = false;
        });
            
        _afkTimerService.Start();
        DisposeAfkTimerHook();

        return 0;
    }

    private void DisposeAfkTimerHook()
    {
        switch (_afkTimerHook)
        {
            case { IsDisposed: true }:
                return;
            case { IsEnabled: true }:
                _afkTimerHook.Disable();
                break;
        }

        _afkTimerHook?.Dispose();
            
        PluginLog.Debug("AFK timer hook disposed.");
    }
}