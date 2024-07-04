using System;
using Dalamud.Game.Network.Structures;
using StanleyParableXiv.Services;

namespace StanleyParableXiv.Events;

public class MarketBoardPurchaseEvent : IDisposable
{
    private DateTimeOffset _marketBoardPurchaseCooldown;
    
    public MarketBoardPurchaseEvent()
    {
        DalamudService.MarketBoard.ItemPurchased += OnMarketBoardItemPurchased;
    }
    
    public void Dispose()
    {
        DalamudService.MarketBoard.ItemPurchased -= OnMarketBoardItemPurchased;
        GC.SuppressFinalize(this);
    }

    private void OnMarketBoardItemPurchased(IMarketBoardPurchase purchase)
    {
        if (_marketBoardPurchaseCooldown > DateTimeOffset.Now) return;

        if (Configuration.Instance.EnableMarketBoardPurchaseEvent)
        {
            AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.MarketBoardPurchase);
        }
        
        // Don't spam the user for multiple purchases
        _marketBoardPurchaseCooldown = DateTimeOffset.Now + TimeSpan.FromMinutes(5);
    }
}