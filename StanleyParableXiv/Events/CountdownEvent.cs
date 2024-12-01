using System;
using System.Runtime.InteropServices;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using StanleyParableXiv.Services;

namespace StanleyParableXiv.Events;

public class CountdownEvent : IDisposable
{
    private delegate nint CountdownTimerHookDelegate(ulong a1);
    
    [Signature("40 53 48 83 EC 40 80 79 38 00", DetourName = nameof(OnCountdownTimer))]
    private readonly Hook<CountdownTimerHookDelegate>? _countdownTimerHook = null;
    
    private bool _countdownStartPlayed = false;
    private bool _countdown10Played = false;
    
    /// <summary>
    /// Fires on countdown start and when 10 seconds remain.
    /// Referenced from https://github.com/xorus/EngageTimer
    /// </summary>
    public CountdownEvent()
    {
        DalamudService.GameInteropProvider.InitializeFromAttributes(this);
        _countdownTimerHook?.Enable();
    }

    public void Dispose()
    {
        _countdownTimerHook?.Dispose();
        GC.SuppressFinalize(this);
    }

    private nint OnCountdownTimer(ulong value)
    {
        if (value == 0) return _countdownTimerHook!.Original(value);
            
        float countdownValue = Marshal.PtrToStructure<float>((IntPtr)value + 0x2c);
        
        // DalamudService.Log.Verbose("Countdown Timer hook value = {CountdownValue}", countdownValue);

        // Reset on countdown completion
        if (countdownValue <= 0f)
        {
            _countdownStartPlayed = false;
            _countdown10Played = false;
            
            return _countdownTimerHook!.Original(value);
        }

        // Countdown started
        if (!_countdownStartPlayed)
        {
            if (Configuration.Instance.EnableCountdownStartEvent) AudioService.Instance.PlayRandomSoundFromCategory(AudioEvent.CountdownStart);
            _countdownStartPlayed = true;
        }

        // Countdown has 10 seconds remaining
        if (_countdownStartPlayed && !_countdown10Played && countdownValue < 10f)
        {
            if (Configuration.Instance.EnableCountdown10Event) AudioService.Instance.PlayRandomSoundFromCategory(AudioEvent.Countdown10);
            _countdown10Played = true;
        }

        return _countdownTimerHook!.Original(value);
    }
}