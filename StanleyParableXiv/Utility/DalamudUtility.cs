using System.IO;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin;
using StanleyParableXiv.Services;

namespace StanleyParableXiv.Utility;

public static class DalamudUtility
{
    public static string GetResourcePath(DalamudPluginInterface pluginInterface, string resource)
    {
        return Path.Combine(pluginInterface.AssemblyLocation.Directory?.FullName!, $"Resources/{resource}");
    }

    public static PlayerCharacter? GetPlayerCharacterFromPartyMember(PartyMember partyMember)
    {
        uint objId = partyMember.ObjectId;
        GameObject? obj = DalamudService.ObjectTable.SearchById(objId);
            
        if (obj?.GetType() == typeof(PlayerCharacter)) return (obj as PlayerCharacter)!;

        return null;
    }
}