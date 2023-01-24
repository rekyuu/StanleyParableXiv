using System.IO;
using Dalamud.Plugin;

namespace StanleyParableXiv;

public static class Utility
{
    public static string GetResourcePath(DalamudPluginInterface pluginInterface, string resource)
    {
        return Path.Combine(pluginInterface.AssemblyLocation.Directory?.FullName!, $"Resources/{resource}");
    }
}