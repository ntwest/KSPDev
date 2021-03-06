﻿// Kerbal Development tools.
// Author: igor.zavoychinskiy@gmail.com
// This software is distributed under Public domain license.

using System;
using System.Collections.Generic;
using KSPDev.Extensions;

namespace KSPDev.ProcessingUtils {

/// <summary>
/// Simple state machine that allows tracking of the states and checking the basic transition
/// conditions.
/// </summary>
/// <remarks>
/// If a module has more that two modes (which can be controlled by a simple boolean) it makes sense
/// to define each mode as a state, and introduce a definite state transition diagram. Once it's
/// done, a state machine can be setup by defining which transitions are allowed. At this point the
/// module will be able to just react on the state change events instead of checking multiple
/// conditions.
/// </remarks>
/// <typeparam name="T">The enum to use as the state constants.</typeparam>
/// <example><code source="Examples/ProcessingUtils/SimpleStateMachine-Examples.cs" region="SimpleStateMachine1"/></example>
public sealed class SimpleStateMachine<T> where T : struct, IConvertible {
  /// <summary>Current state of the machine.</summary>
  /// <remarks>
  /// <para>
  /// Setting the same state as the current one is a NO-OP. Setting of a new state may throw an
  /// exception in the strict mode.
  /// </para>
  /// <para>
  /// The initial state is always <c>null</c>, whiсh means <i>STOPPED</i>. The caller must set the
  /// initial state before starting using the machine. In spite of the other transitions, the
  /// initial state change is not restricted by the state transition constraints, regardless to the
  /// <see cref="isStrict"/> setting.
  /// </para>
  /// <para>
  /// To <i>STOP</i> the machine, just set the current state to <c>null</c>. No "enter state"
  /// handlers will be executed in this case, but all the "leave state" handlers will do their job.
  /// </para>
  /// </remarks>
  /// <value>The current state. It can be <c>null</c>.</value>
  /// <seealso cref="isStrict"/>
  /// <example><code source="Examples/ProcessingUtils/SimpleStateMachine-Examples.cs" region="SimpleStateMachineFree"/></example>
  public T? currentState {
    get { return _currentState; }
    set { SetState(value); }
  }
  T? _currentState;

  /// <summary>Tells if all the transitions must be excplicitly declared.</summary>
  /// <value>The strict mode state.</value>
  /// <seealso cref="SetTransitionConstraint"/>
  /// <example><code source="Examples/ProcessingUtils/SimpleStateMachine-Examples.cs" region="SimpleStateMachineStrict"/></example>
  public bool isStrict { get; private set; }

  /// <summary>Delegate for a callback which notifies about a state change.</summary>
  /// <param name="state">
  /// The state of the machine. Its exact meaning depends on the circumstances under which the
  /// callback has been called.
  /// </param>
  /// <seealso cref="currentState"/>
  /// <seealso cref="AddStateHandlers"/>
  /// <example><code source="Examples/ProcessingUtils/SimpleStateMachine-Examples.cs" region="SimpleStateMachineFree"/></example>
  public delegate void OnChange(T? state);

  /// <summary>Delegate to track an arbitrary state transition.</summary>
  /// <param name="fromState">The state before the change.</param>
  /// <param name="toState">The state after the change.</param>
  /// <seealso cref="currentState"/>
  /// <seealso cref="onAfterTransition"/>
  /// <example><code source="Examples/ProcessingUtils/SimpleStateMachine-Examples.cs" region="SimpleStateMachineFree"/></example>
  public delegate void OnStateChangeHandler(T? fromState, T? toState);

  /// <summary>Event that fires when the state machine has changed its state.</summary>
  /// <remarks>The event is fired <i>after</i> the actual state change.</remarks>
  /// <seealso cref="currentState"/>
  /// <seealso cref="OnStateChangeHandler"/>
  /// <example><code source="Examples/ProcessingUtils/SimpleStateMachine-Examples.cs" region="SimpleStateMachineFree"/></example>
  public event OnStateChangeHandler onAfterTransition;

  readonly Dictionary<T, HashSet<OnChange>> enterHandlers = new Dictionary<T, HashSet<OnChange>>();
  readonly Dictionary<T, HashSet<OnChange>> leaveHandlers = new Dictionary<T, HashSet<OnChange>>();
  readonly Dictionary<T, T[]> transitionContstraints = new Dictionary<T, T[]>();

  /// <summary>Constructs a new uninitialized state machine.</summary>
  /// <param name="strict">Tells if all the transitions must be excplicitly declared.</param>
  /// <seealso cref="isStrict"/>
  /// <seealso cref="currentState"/>
  /// <seealso cref="SetTransitionConstraint"/>
  /// <example><code source="Examples/ProcessingUtils/SimpleStateMachine-Examples.cs" region="SimpleStateMachineStrict"/></example>
  /// <example><code source="Examples/ProcessingUtils/SimpleStateMachine-Examples.cs" region="SimpleStateMachineFree"/></example>
  public SimpleStateMachine(bool strict) {
    isStrict = strict;
  }

  /// <summary>Defines a state and the allowed target states for it.</summary>
  /// <remarks>
  /// In the strict mode it's required that every transition is declared excplicitly.
  /// </remarks>
  /// <param name="fromState">The source state.</param>
  /// <param name="toStates">The list of the states that are allowed as the targets.</param>
  /// <seealso cref="isStrict"/>
  /// <seealso cref="currentState"/>
  /// <example><code source="Examples/ProcessingUtils/SimpleStateMachine-Examples.cs" region="SimpleStateMachineStrict"/></example>
  public void SetTransitionConstraint(T fromState, T[] toStates) {
    CheckIsNotStarted();
    transitionContstraints.Remove(fromState);
    transitionContstraints.Add(fromState, toStates);
  }

  /// <summary>Clears the transitions for the source state if any.</summary>
  /// <param name="fromState">The source state to clear the tarnsitions for.</param>
  /// <seealso cref="SetTransitionConstraint"/>
  public void ResetTransitionConstraint(T fromState) {
    CheckIsNotStarted();
    transitionContstraints.Remove(fromState);
  }

  /// <summary>Adds a state change event.</summary>
  /// <remarks>
  /// <para>
  /// When the state is changed by setting the <see cref="currentState"/> property, the transition
  /// callbacks are only called when the state has actually changed.
  /// </para>
  /// <para>
  /// Note, that the code must not expect that the handlers will be called in the same order as they
  /// were added. Each handler must be independent from the others.
  /// </para>
  /// </remarks>
  /// <param name="state">The state to call a callback on.</param>
  /// <param name="enterHandler">
  /// The callback to call when the state machine has switched to a new state. The callback is
  /// triggered <i>after</i> the state has actually changed. The callback's parameter is the
  /// <i>old</i> state, from which the machine has switched.
  /// </param>
  /// <param name="leaveHandler">
  /// The callback to call when the state machine is going to leave the current state. The callback
  /// is triggered <i>before</i> the state has actually changed. The callback's parameter is the
  /// <i>new</i> state, to which the machine is going to switch. 
  /// </param>
  /// <seealso cref="currentState"/>
  /// <example><code source="Examples/ProcessingUtils/SimpleStateMachine-Examples.cs" region="SimpleStateMachineFree"/></example>
  public void AddStateHandlers(
      T state, OnChange enterHandler = null, OnChange leaveHandler = null) {
    CheckIsNotStarted();
    if (enterHandler != null) {
      enterHandlers.SetDefault(state).Add(enterHandler);
    }
    if (leaveHandler != null) {
      leaveHandlers.SetDefault(state).Add(leaveHandler);
    }
  }

  /// <summary>Removes a state change event handler.</summary>
  /// <remarks>It's safe to call it for a non-existing handler.</remarks>
  /// <param name="state">The state to delete a handler for.</param>
  /// <param name="enterHandler">The enter state handler to delete.</param>
  /// <param name="leaveHandler">The leave state handler to delete.</param>
  public void RemoveHandlers(T state, OnChange enterHandler = null, OnChange leaveHandler = null) {
    CheckIsNotStarted();
    if (enterHandler != null && enterHandlers.ContainsKey(state)) {
      enterHandlers[state].Remove(enterHandler);
    }
    if (leaveHandler != null && leaveHandlers.ContainsKey(state)) {
      leaveHandlers[state].Remove(leaveHandler);
    }
  }

  /// <summary>Verifies if the machine can move into the desired state.</summary>
  /// <param name="newState">The state to check the transition for.</param>
  /// <returns><c>true</c> if the transition is allowed.</returns>
  /// <seealso cref="isStrict"/>
  /// <seealso cref="SetTransitionConstraint"/>
  /// <example><code source="Examples/ProcessingUtils/SimpleStateMachine-Examples.cs" region="SimpleStateMachineStrict"/></example>
  public bool CheckCanSwitchTo(T newState) {
    return !isStrict
        || transitionContstraints.ContainsKey(_currentState.Value)
           && transitionContstraints[_currentState.Value].IndexOf(newState) != -1;
  }

  #region Local utility methods
  /// <summary>Verifies that the state machine is started.</summary>
  /// <exception cref="InvalidOperationException">If state machine is not yet started.</exception>
  void CheckIsStarted() {
    if (!currentState.HasValue) {
      throw new InvalidOperationException("Not allowed in STOPPED state");
    }
  }

  /// <summary>Verifies that the state machine is <i>not</i> started.</summary>
  /// <exception cref="InvalidOperationException">If state machine is already started.</exception>
  void CheckIsNotStarted() {
    if (currentState.HasValue) {
      throw new InvalidOperationException("Not allowed in STARTED state");
    }
  }

  /// <summary>
  /// Changes the machine's state if the current and the new states are different. Checks if the
  /// transition is allowed before actually changing the state.
  /// </summary>
  /// <param name="newState">
  /// The state to change to. If <c>null</c> then the machine will be stopped.
  /// </param>
  /// <seealso cref="CheckCanSwitchTo"/>
  /// <exception cref="InvalidOperationException">If the transition is not allowed.</exception>
  void SetState(T? newState) {
    if (!_currentState.Equals(newState)) {
      var oldState = _currentState;
      if (oldState.HasValue && newState.HasValue) {
        if (!CheckCanSwitchTo(newState.Value)) {
          throw new InvalidOperationException(string.Format(
              "Transition {0}=>{1} is not allowed", oldState.Value, newState.Value));
        }
      }
      if (oldState.HasValue) {
        FireLeaveState(newState);
      }
      _currentState = newState;
      if (newState.HasValue) {
        FireEnterState(oldState);
      }
      if (onAfterTransition != null) {
        onAfterTransition(oldState, newState);
      }
    }
  }

  /// <summary>Notifies all the handlers about leaving the current state.</summary>
  /// <param name="newState">The new state where the machine is going to.</param>
  void FireLeaveState(T? newState) {
    HashSet<OnChange> handlers;
    if (leaveHandlers.TryGetValue(_currentState.Value, out handlers)) {
      foreach (var @event in handlers) {
        @event.Invoke(newState);
      }
    }
  }

  /// <summary>Notifies all the handlers about entering a new state.</summary>
  /// <param name="oldState">The old state where the machine is going from.</param>
  void FireEnterState(T? oldState) {
    HashSet<OnChange> handlers;
    if (enterHandlers.TryGetValue(_currentState.Value, out handlers)) {
      foreach (var @event in handlers) {
        @event.Invoke(oldState);
      }
    }
  }
  #endregion
}

}  // namespace
