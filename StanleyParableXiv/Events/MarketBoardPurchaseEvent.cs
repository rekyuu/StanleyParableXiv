using System;
using Dalamud.Game.Network;
using StanleyParableXiv.Services;

namespace StanleyParableXiv.Events;

public class MarketBoardPurchaseEvent : IDisposable
{
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
        if (opCode == DalamudService.DataManager.ServerOpCodes["MarketBoardPurchase"])
        {
            AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.MarketBoardPurchase);
        }
    }
}