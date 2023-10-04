using System;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using StanleyParableXiv.Services;

namespace StanleyParableXiv.Events;

public class MarketBoardPurchaseEvent : IDisposable
{
    private unsafe delegate long OnMarketBoardPurchaseDelegate(nint a1, uint* a2);

    [Signature("40 55 53 57 48 8B EC 48 83 EC ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 45 ?? 48 8B 0D", 
        DetourName = nameof(OnMarketBoardPurchase))]
    private readonly Hook<OnMarketBoardPurchaseDelegate>? _marketBoardPurchaseHook = null;
    
    private DateTimeOffset _marketBoardPurchaseCooldown;
    
    /// <summary>
    /// Fires on market board purchases.
    /// Originally referenced from https://github.com/tesu/PennyPincher
    /// </summary>
    public MarketBoardPurchaseEvent()
    {
        DalamudService.GameInteropProvider.InitializeFromAttributes(this);
        _marketBoardPurchaseHook?.Enable();
    }
    
    public void Dispose()
    {
        _marketBoardPurchaseHook?.Dispose();
        GC.SuppressFinalize(this);
    }

    private unsafe void OnMarketBoardPurchase(nint a1, uint* a2)
    {
        _marketBoardPurchaseHook?.Original(a1, a2);
        
        if (_marketBoardPurchaseCooldown > DateTimeOffset.Now) return;

        if (Configuration.Instance.EnableMarketBoardPurchaseEvent)
        {
            AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.MarketBoardPurchase);
        }
        
        // Don't spam the user for multiple purchases
        _marketBoardPurchaseCooldown = DateTimeOffset.Now + TimeSpan.FromMinutes(5);
    }
}