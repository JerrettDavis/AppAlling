using System.Reactive.Linq;
using System.Reactive.Subjects;
using AppAlling.Abstractions.State;

namespace AppAlling.Application.State;

/// <summary>
/// Reactive store that holds application state and reduces <see cref="IAction"/>s via a reducer function.
/// </summary>
public sealed class Store<TState> : IStore<TState>, IDisposable
{
    private readonly Func<TState, IAction, TState> _reducer;
    private readonly BehaviorSubject<TState> _subject;
#if NET9_0_OR_GREATER 
    private readonly Lock _gate = new();
#else
    private readonly object _gate = new();
#endif
    /// <summary>
    /// Creates a new store with an initial state and a reducer function.
    /// </summary>
    /// <param name="initial">Initial state snapshot.</param>
    /// <param name="reducer">Pure reducer that returns the next state given (current, action).</param>
    public Store(TState initial, Func<TState, IAction, TState> reducer)
    {
        Current = initial;
        _reducer = reducer;
        _subject = new BehaviorSubject<TState>(initial);
        States = _subject.AsObservable();
    }

    /// <inheritdoc />
    public TState Current { get; private set; }

    /// <inheritdoc />
    public IObservable<TState> States { get; }

    /// <inheritdoc />
    public void Dispatch(IAction action)
    {
        lock (_gate)
        {
            var next = _reducer(Current, action);
            if (Equals(next, Current))
                return;

            Current = next;
            _subject.OnNext(next);
        }
    }

    /// <summary>
    /// Disposes the underlying observable subject.
    /// </summary>
    public void Dispose() => _subject.Dispose();
}