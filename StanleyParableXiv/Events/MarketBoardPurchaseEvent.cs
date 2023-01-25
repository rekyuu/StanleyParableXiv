using System;
using Dalamud.Game.Network;
using StanleyParableXiv.Services;

namespace StanleyParableXiv.Events;

public class MarketBoardPurchaseEvent : IDisposable
{
    private DateTimeOffset _marketBoardPurchaseCooldown; 
    
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
        if (opCode != DalamudService.DataManager.ServerOpCodes["MarketBoardPurchase"]) return;
        if (_marketBoardPurchaseCooldown > DateTimeOffset.Now) return;
        
        AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.MarketBoardPurchase);
        
        _marketBoardPurchaseCooldown = DateTimeOffset.Now + TimeSpan.FromMinutes(1);
    }
}