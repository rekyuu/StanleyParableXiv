using System;
using StanleyParableXiv.Services;

namespace StanleyParableXiv.Events;

public class LoginEvent : IDisposable
{
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
        AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.Login);
    }
}