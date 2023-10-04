using System;
using System.Threading;
using System.Threading.Tasks;

namespace StanleyParableXiv.Services;

public class TimerService : IDisposable
{
    private Timer? _timer;
    private readonly uint _timerSeconds;
    private readonly Action _method;

    /// <summary>
    /// Runs the supplied action at the supplied cadence.
    /// </summary>
    /// <param name="timerSeconds">The amount of time to run the action.</param>
    /// <param name="method">The action to run.</param>
    public TimerService(uint timerSeconds, Action method)
    {
        _timerSeconds = timerSeconds;
        _method = method;
    }
    
    public void Dispose()
    {
        _timer?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Starts the method loop.
    /// </summary>
    public void Start()
    {
        _timer = new Timer(RunMethod, null, TimeSpan.Zero, TimeSpan.FromSeconds(_timerSeconds));
    }

    /// <summary>
    /// Stops the method loop.
    /// </summary>
    public void Stop()
    {
        _timer?.Change(Timeout.Infinite, 0);
    }

    private void RunMethod(object? state)
    {
        Task.Run(() => _method());
    }
}