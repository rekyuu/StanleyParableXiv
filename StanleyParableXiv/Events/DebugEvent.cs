using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Network;
using Dalamud.Logging;
using Dalamud.Plugin.Services;
using StanleyParableXiv.Services;

namespace StanleyParableXiv.Events;

public class DebugEvent : IDisposable
{
    private readonly Dictionary<ConditionFlag, bool> _conditions = new();
    
    /// <summary>
    /// Fires on login events.
    /// </summary>
    public DebugEvent()
    {
        DalamudService.Framework.Update += OnFrameworkUpdate;
        DalamudService.GameNetwork.NetworkMessage += OnGameNetworkMessage;
    }

    public void Dispose()
    {
        DalamudService.Framework.Update -= OnFrameworkUpdate;
        DalamudService.GameNetwork.NetworkMessage -= OnGameNetworkMessage;
        
        GC.SuppressFinalize(this);
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (!Configuration.Instance.EnableDebugLogging) return;
        LogConditionFlagChanges();
    }

    private static unsafe void OnGameNetworkMessage(IntPtr dataPtr, ushort opCode, uint sourceActorId, uint targetActorId,
        NetworkMessageDirection direction)
    {
        if (!Configuration.Instance.EnableDebugLogging) return;
        
        ushort cat = *(ushort*)(dataPtr + 0x00);
        uint updateType = *(uint*)(dataPtr + 0x08);
        
        DalamudService.Log.Verbose("OpCode = {OpCode}, Cat = 0x{Cat:X}, UpdateType = 0x{UpdateType:X}", opCode, cat, updateType);
    }

    private void LogConditionFlagChanges()
    {
        foreach (int condition in Enum.GetValues(typeof(ConditionFlag)))
        {
            ConditionFlag flag = (ConditionFlag)condition;
            bool currentValue = DalamudService.Condition[flag];

            if (_conditions.ContainsKey(flag) && _conditions[flag] != currentValue)
            {
                DalamudService.Log.Verbose("Condition for {Flag} changed: {LastValue} -> {CurrentValue}", flag, _conditions[flag], currentValue);
            }
            
            _conditions[flag] = currentValue;
        }
    }
}