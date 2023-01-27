using System;
using Dalamud.Game.Network;
using StanleyParableXiv.Services;

namespace StanleyParableXiv.Events;

public class MarketBoardPurchaseEvent : IDisposable
{
    private DateTimeOffset _marketBoardPurchaseCooldown; 
    
    /// <summary>
    /// Fires on market board purchases.
    /// Referenced from https://github.com/tesu/PennyPincher
    /// </summary>
    public MarketBoardPurchaseEvent()
    {
        DalamudService.GameNetwork.NetworkMessage += OnGameNetworkMessage;
    }
    
    public void Dispose()
    {
        DalamudService.GameNetwork.NetworkMessage -= OnGameNetworkMessage;
    }

    private void OnGameNetworkMessage(IntPtr dataPtr, ushort opCode, uint sourceActorId, uint targetActorId,
        NetworkMessageDirection direction)
    {
        // Will probably break on game updates until opcodes are updated.
        if (opCode != DalamudService.DataManager.ServerOpCodes["MarketBoardPurchase"]) return;
        if (_marketBoardPurchaseCooldown > DateTimeOffset.Now) return;

        if (Configuration.Instance.EnableMarketBoardPurchaseEvent)
        {
            AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.MarketBoardPurchase);
        }
        
        // Don't spam the user for multiple purchases
        _marketBoardPurchaseCooldown = DateTimeOffset.Now + TimeSpan.FromMinutes(5);
    }
}