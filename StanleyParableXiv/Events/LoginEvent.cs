using System;
using System.Collections.Generic;
using Dalamud.Game;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Logging;
using StanleyParableXiv.Services;

namespace StanleyParableXiv.Events;

public class LoginEvent : IDisposable
{
    private bool _loginReady = false;
    private bool _hasLoggedIn = false;
    
    /// <summary>
    /// Fires on login events.
    /// </summary>
    public LoginEvent()
    {
        DalamudService.ClientState.Login += OnLogin;
        DalamudService.ClientState.Logout += OnLogout;
        DalamudService.Framework.Update += OnFrameworkUpdate;
    }

    public void Dispose()
    {
        DalamudService.ClientState.Login -= OnLogin;
        DalamudService.ClientState.Logout -= OnLogout;
        DalamudService.Framework.Update -= OnFrameworkUpdate;
    }

    private void OnFrameworkUpdate(Framework framework)
    {
        if (!_loginReady || _hasLoggedIn) return;
        
        bool isLoading = DalamudService.Condition[ConditionFlag.BetweenAreas];
        
        if (isLoading || !Configuration.Instance.EnableLoginEvent) return;
        
        AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.Login);
        _hasLoggedIn = true;
    }

    private void OnLogin(object? sender, EventArgs eventArgs)
    {
        _loginReady = true;
    }

    private void OnLogout(object? sender, EventArgs e)
    {
        _loginReady = false;
        _hasLoggedIn = false;
    }
}