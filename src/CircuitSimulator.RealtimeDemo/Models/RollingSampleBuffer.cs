using CircuitSimulator.Core.Results;

namespace CircuitSimulator.RealtimeDemo.Models;

/// <summary>Thread-safe bounded storage for the newest fixed-step realtime samples.</summary>
public sealed class RollingSampleBuffer
{
    private readonly object _sync = new();
    private readonly TransientSample?[] _samples;
    private int _head;
    private int _count;

    /// <summary>Initializes a rolling sample window.</summary>
    public RollingSampleBuffer(double duration, double timeStep)
    {
        if (!double.IsFinite(duration) || duration <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), duration, "Duration must be finite and greater than zero.");
        }

        if (!double.IsFinite(timeStep) || timeStep <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(timeStep), timeStep, "Time step must be finite and greater than zero.");
        }

        var requiredCapacity = Math.Ceiling(duration / timeStep) + 1.0;
        if (requiredCapacity > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), duration, "The requested history window is too large.");
        }

        Duration = duration;
        Capacity = (int)requiredCapacity;
        _samples = new TransientSample[Capacity];
    }

    /// <summary>Gets the retained history duration in seconds.</summary>
    public double Duration { get; }

    /// <summary>Gets the maximum number of retained samples.</summary>
    public int Capacity { get; }

    /// <summary>Gets the current retained sample count.</summary>
    public int Count
    {
        get
        {
            lock (_sync)
            {
                return _count;
            }
        }
    }

    /// <summary>Adds one chronologically newer immutable sample.</summary>
    public void Add(TransientSample sample)
    {
        ArgumentNullException.ThrowIfNull(sample);
        lock (_sync)
        {
            if (_count > 0 && sample.Time <= GetAt(_count - 1).Time)
            {
                throw new ArgumentException("Realtime samples must be added in strictly ascending time order.", nameof(sample));
            }

            if (_count == Capacity)
            {
                RemoveOldest();
            }

            _samples[(_head + _count) % Capacity] = sample;
            _count++;
            var cutoff = sample.Time - Duration;
            while (_count > 1 && GetAt(0).Time < cutoff)
            {
                RemoveOldest();
            }
        }
    }

    /// <summary>Returns an immutable-in-practice ordered snapshot of retained samples.</summary>
    public IReadOnlyList<TransientSample> Snapshot()
    {
        lock (_sync)
        {
            var snapshot = new TransientSample[_count];
            for (var index = 0; index < _count; index++)
            {
                snapshot[index] = GetAt(index);
            }

            return snapshot;
        }
    }

    private TransientSample GetAt(int index) =>
        _samples[(_head + index) % Capacity]
        ?? throw new InvalidOperationException("The rolling sample buffer contains an empty slot.");

    private void RemoveOldest()
    {
        _samples[_head] = null;
        _head = (_head + 1) % Capacity;
        _count--;
    }
}
