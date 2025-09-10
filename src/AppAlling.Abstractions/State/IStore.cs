namespace AppAlling.Abstractions.State;

/// <summary>
/// The store interface that defines the contract for a state container.
/// </summary>
/// <typeparam name="TState">The type of the state managed by the store.</typeparam>
public interface IStore<out TState>
{
    /// <summary>
    /// Gets the latest materialized state synchronously.
    /// </summary>
    TState Current { get; }

    /// <summary>
    /// Observable stream of state snapshots; emits when the state changes.
    /// </summary>
    IObservable<TState> States { get; }

    /// <summary>
    /// Dispatches an action to be reduced into the next state.
    /// </summary>
    /// <param name="action">The action to apply via reducers.</param>
    void Dispatch(IAction action);
}