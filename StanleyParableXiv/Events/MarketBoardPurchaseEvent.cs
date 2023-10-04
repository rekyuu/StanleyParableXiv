using System;
using Dalamud.Game.Network;
using Dalamud.Hooking;
using StanleyParableXiv.Services;

namespace StanleyParableXiv.Events;

public class MarketBoardPurchaseEvent : IDisposable
{
    private const string MarketBoardPurchaseSig = "40 55 53 57 48 8B EC 48 83 EC ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 45 ?? 48 8B 0D";
    private DateTimeOffset _marketBoardPurchaseCooldown;
    private readonly Hook<Action<nint, nint>> _marketBoardPurchaseHook;
    
    /// <summary>
    /// Fires on market board purchases.
    /// Referenced from https://github.com/tesu/PennyPincher
    /// </summary>
    public MarketBoardPurchaseEvent()
    {
        _marketBoardPurchaseHook = DalamudService.GameInteropProvider.HookFromSignature(
            MarketBoardPurchaseSig,
            OnMarketBoardPurchase);
        _marketBoardPurchaseHook.Enable();
    }
    
    public void Dispose()
    {
        switch (_marketBoardPurchaseHook)
        {
            case { IsDisposed: true }:
                return;
            case { IsEnabled: true }:
                _marketBoardPurchaseHook.Disable();
                break;
        }

        _marketBoardPurchaseHook?.Dispose();
        
        GC.SuppressFinalize(this);
    }

    private void OnMarketBoardPurchase(nint a1, nint a2)
    {
        _marketBoardPurchaseHook.Original(a1, a2);
        
        if (_marketBoardPurchaseCooldown > DateTimeOffset.Now) return;

        if (Configuration.Instance.EnableMarketBoardPurchaseEvent)
        {
            AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.MarketBoardPurchase);
        }
        
        // Don't spam the user for multiple purchases
        _marketBoardPurchaseCooldown = DateTimeOffset.Now + TimeSpan.FromMinutes(5);
    }
}