using System;
using System.Collections.Generic;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Common.Configuration;
using Lumina.Data;
using Lumina.Excel.GeneratedSheets;
using StanleyParableXiv.Services;

namespace StanleyParableXiv.Utility;

public enum XivVolumeSource
{
    Bgm,
    Se,
    Voice,
    Env,
    System,
    Perform,
    Master
}

public static class XivUtility
{
    private static readonly Dictionary<XivVolumeSource, string> XivVolumeSourceMap = new()
    {
        { XivVolumeSource.Bgm, "Bgm" },
        { XivVolumeSource.Se, "Se" },
        { XivVolumeSource.Voice, "Voice" },
        { XivVolumeSource.Env, "Env" },
        { XivVolumeSource.System, "System" },
        { XivVolumeSource.Perform, "Perform" },
        { XivVolumeSource.Master, "Master" },
    };

    /// <summary>
    /// Gets the volume amount of the supplied FFXIV channel.
    /// </summary>
    /// <param name="soundType">The FFXIV volume channel.</param>
    /// <returns>The volume amount between 0-100.</returns>
    /// <exception cref="Exception">Will be thrown if the supplied channel is not found.</exception>
    public static unsafe uint GetVolume(XivVolumeSource soundType)
    {
        string volumeSourceName = XivVolumeSourceMap[soundType];
        string volumeSourceAmountKey = $"Sound{volumeSourceName}";
        string volumeSourceMutedKey = $"IsSnd{volumeSourceName}";

        uint? volumeAmount = null;
        bool? volumeMuted = null;
        
        try
        {
            Framework* framework = Framework.Instance();
            ConfigBase configBase = framework->SystemConfig.CommonSystemConfig.ConfigBase;

            for (int i = 0; i < configBase.ConfigCount; i++)
            {
                ConfigEntry entry = configBase.ConfigEntry[i];
                if (entry.Name == null) continue;

                string name = MemoryHelper.ReadStringNullTerminated(new IntPtr(entry.Name));

                if (name == volumeSourceAmountKey) volumeAmount = entry.Value.UInt;
                else if (name == volumeSourceMutedKey) volumeMuted = entry.Value.UInt == 1;
            }

            if (volumeAmount == null || volumeMuted == null)
            {
                throw new Exception($"Unable to determine volume for {volumeSourceName}");
            }

            return volumeMuted.Value ? 0 : volumeAmount.Value;
        }
        catch (Exception ex)
        {
            DalamudService.Log.Error(ex, "An exception occurred while obtaining volume");
            return 50;
        }
    }

    /// <summary>
    /// Checks if the territory is Unreal, Extreme, Savage, or Ultimate difficulty.
    /// </summary>
    /// <param name="territoryType">The territory to check against.</param>
    /// <returns>True if high-end, false otherwise.</returns>
    public static bool TerritoryIsHighEndDuty(ushort territoryType)
    {
        string name = DalamudService.DataManager.Excel
            .GetSheet<TerritoryType>(Language.English)!
            .GetRow(territoryType)!
            .ContentFinderCondition.Value!
            .Name
            .RawString;

        bool isHighEndDuty = name.StartsWith("the Minstrel's Ballad")
            || name.EndsWith("(Unreal)")
            || name.EndsWith("(Extreme)")
            || name.EndsWith("(Savage)")
            || name.EndsWith("(Ultimate)");
        
        DalamudService.Log.Debug("{DutyName} is high end: {IsHighEnd}", name, isHighEndDuty);

        return isHighEndDuty;
    }

    /// <summary>
    /// Checks if player's current territory is Unreal, Extreme, Savage, or Ultimate difficulty.
    /// </summary>
    /// <returns>True if high-end, false otherwise.</returns>
    public static bool PlayerIsInHighEndDuty()
    {
        return TerritoryIsHighEndDuty(DalamudService.ClientState.TerritoryType);
    }
}