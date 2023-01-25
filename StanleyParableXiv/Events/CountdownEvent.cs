using System;
using System.Runtime.InteropServices;
using Dalamud.Hooking;
using Dalamud.Logging;
using StanleyParableXiv.Services;

namespace StanleyParableXiv.Events;

public class CountdownEvent : IDisposable
{
    private delegate IntPtr CountdownTimerHookDelegate(ulong p1);
    private readonly Hook<CountdownTimerHookDelegate>? _countdownTimerHook;
    private bool _countdownStartPlayed = false;
    private bool _countdown10Played = false;
    
    public CountdownEvent()
    {
        _countdownTimerHook = Hook<CountdownTimerHookDelegate>.FromAddress(
            DalamudService.SigScanner.ScanText("48 89 5C 24 ?? 57 48 83 EC 40 8B 41"),
            OnCountdownTimerHook);
        _countdownTimerHook.Enable();
    }

    public void Dispose()
    {
        switch (_countdownTimerHook)
        {
            case { IsDisposed: true }:
                return;
            case { IsEnabled: true }:
                _countdownTimerHook.Disable();
                break;
        }

        _countdownTimerHook?.Dispose();
    }

    private nint OnCountdownTimerHook(ulong value)
    {
        if (value == 0) return _countdownTimerHook!.Original(value);
            
        float countdownValue = Marshal.PtrToStructure<float>((IntPtr)value + 0x2c);
        
        PluginLog.Verbose("Countdown Timer hook value = {CountdownValue}", countdownValue);

        if (countdownValue < 0f)
        {
            _countdownStartPlayed = false;
            _countdown10Played = false;
            
            return _countdownTimerHook!.Original(value);
        }

        if (!_countdownStartPlayed)
        {
            AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.CountdownStart);
            _countdownStartPlayed = true;
        }

        if (_countdownStartPlayed && !_countdown10Played && countdownValue < 10f)
        {
            AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.Countdown10);
            _countdown10Played = true;
        }

        return _countdownTimerHook!.Original(value);
    }
}