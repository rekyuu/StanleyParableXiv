using System;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;

namespace StanleyParableXiv.Services;

public class TerritoryService : IDisposable
{
    public static TerritoryService Instance { get; private set; } = new();

    public TerritoryType? CurrentTerritory;

    public event OnTerritoryChangedDelegate? TerritoryChanged;

    public delegate void OnTerritoryChangedDelegate(TerritoryType? territoryType);

    private uint? _currentTerritoryRowId;

    public TerritoryService()
    {
        DalamudService.Framework.Update += OnFrameworkUpdate;
    }

    public void Dispose()
    {
        DalamudService.Framework.Update -= OnFrameworkUpdate;

        GC.SuppressFinalize(this);
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (_currentTerritoryRowId == DalamudService.ClientState.TerritoryType) return;

        DalamudService.Log.Debug("Territory changed: {LastTerritory} -> {NewTerritory}",
            _currentTerritoryRowId ?? 0, DalamudService.ClientState.TerritoryType);

        bool currentTerritoryExists = DalamudService.DataManager.Excel
            .GetSheet<TerritoryType>()
            .TryGetRow(DalamudService.ClientState.TerritoryType, out TerritoryType nextTerritory);
        if (!currentTerritoryExists) return;

        CurrentTerritory = nextTerritory;
        _currentTerritoryRowId = DalamudService.ClientState.TerritoryType;

        TerritoryChanged?.Invoke(CurrentTerritory);
    }
}