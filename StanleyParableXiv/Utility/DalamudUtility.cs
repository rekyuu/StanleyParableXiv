using System.IO;
using Dalamud.Plugin;
using StanleyParableXiv.Services;

namespace StanleyParableXiv.Utility;

public static class DalamudUtility
{
    public static string GetResourcePath(DalamudPluginInterface pluginInterface, string resource)
    {
        return Path.Combine(pluginInterface.AssemblyLocation.Directory?.FullName!, $"Resources/{resource}");
    }
}