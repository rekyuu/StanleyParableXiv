using System;
using StanleyParableXiv.Services;

namespace StanleyParableXiv.Events;

public class LoginEvent : IDisposable
{
    /// <summary>
    /// Fires on login events.
    /// </summary>
    public LoginEvent()
    {
        DalamudService.ClientState.Login += OnLogin;
    }
    
    public void Dispose()
    {
        DalamudService.ClientState.Login -= OnLogin;
    }

    private void OnLogin(object? sender, EventArgs eventArgs)
    {
        if (Configuration.Instance.EnableLoginEvent)
        {
            AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.Login);
        }
    }
}