using System;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
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
        
        GC.SuppressFinalize(this);
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (!_loginReady || _hasLoggedIn) return;
        
        bool isLoading = DalamudService.Condition[ConditionFlag.BetweenAreas];
        
        if (isLoading || !Configuration.Instance.EnableLoginEvent) return;
        
        AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.Login);
        _hasLoggedIn = true;
    }

    private void OnLogin()
    {
        _loginReady = true;
    }

    private void OnLogout()
    {
        _loginReady = false;
        _hasLoggedIn = false;
    }
}