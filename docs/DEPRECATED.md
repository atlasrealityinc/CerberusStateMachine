# Deprecated APIs

APIs listed here still ship with CerberusStateMachine and behave exactly as they always have, so upgrading does not break existing code. Each one is marked `[Obsolete]`, so using it produces compiler warning **CS0618** whose message names the replacement and points to this page. They are kept so that existing code keeps compiling; removing any of them would be a breaking change.

- [Per-State Controllers](#per-state-controllers)

## Per-State Controllers

> **Status:** Deprecated. Replaced by `IStateMachine<StateIdT>.StateController`, documented in the [State Controllers](../README.md#state-controllers) section of the README.

Before the state machine had a single controller, triggering an event meant first looking up a controller bound to one specific state through `IStateMachine<StateIdT>.StateControllerProvider`, then calling `TriggerEvent` on that controller. Everything involved in that lookup is deprecated:

| Deprecated symbol | Namespace | Replacement |
|---|---|---|
| `IStateMachine<StateIdT>.StateControllerProvider` | `Cerberus` | `IStateMachine<StateIdT>.StateController` |
| `IStateControllerProvider` | `Cerberus.StateController` | None needed, `StateController` requires no lookup |
| `IStateController<EventIdT>` | `Cerberus` | `IStateController` (non-generic) |
| `IStateController<EventIdT, SubStateIdT>` | `Cerberus` | `IStateController` for triggering. **No replacement for `CurrentSubState`**, see [Querying the Current Sub-State](#querying-the-current-sub-state) |
| `BindInfo` | `Cerberus.StateController` | None |
| `StateControllerBindInfo<StateIdT>` | `Cerberus.StateController` | None |

### Why it is deprecated

- **You had to know which state was active.** A per-state controller only handles an event while its own state is active; triggering on any other controller silently returns `false`. Callers ended up tracking the active state themselves just to pick the right controller.
- **Sub-state lookups could be ambiguous.** Controllers are keyed by state id and event enum type. If the same sub-state enum is used under more than one parent state, looking up a sub-state controller throws an `ArgumentException` because there is no way to tell which parent's sub-state is meant.
- **It needed a type-heavy call.** `GetStateController<IStateController<EventIdT>, StateIdT, EventIdT>(stateId)` repeats information the state machine already has.

`IStateMachine<StateIdT>.StateController` removes all three problems: one call offers the event to every currently active state, innermost sub-state first, then machine-level events, and it allocates nothing per call.

### Migrating to the unified controller

Replace the lookup plus `TriggerEvent` with a single `TriggerEvent` on the state machine:

```csharp
// Before
var controller = stateMachine.StateControllerProvider
    .GetStateController<IStateController<GameEvent>, GameState, GameEvent>(GameState.Idle);
bool handled = controller.TriggerEvent(GameEvent.Start);

// After
bool handled = stateMachine.StateController.TriggerEvent(GameEvent.Start);
```

The same applies to sub-state and machine-level events. There is no separate lookup for sub-states; the event reaches the active sub-state automatically:

```csharp
// Before
var exploring = stateMachine.StateControllerProvider
    .GetStateController<IStateController<InGameEvent>, InGameState, InGameEvent>(InGameState.Exploring);
exploring.TriggerEvent(InGameEvent.Pause);

// After
stateMachine.StateController.TriggerEvent(InGameEvent.Pause);
```

If you kept controllers in fields or passed them around, pass the `IStateController` from `stateMachine.StateController` instead. It is a single object for the lifetime of the state machine.

### Behavior differences from the unified controller

Most code migrates without observable change, but the two paths are not identical:

| | Per-state controller (deprecated) | `IStateMachine.StateController` |
|---|---|---|
| Which states see the event | Only the one state the controller is bound to, and only while it is active | Every currently active state whose event enum matches, innermost first, then machine-level events |
| Return value | `true` if that one state handled it | `true` if any level handled it |
| Inactive state | Returns `false` | Not applicable, inactive states are never offered the event |
| Ambiguous sub-state ids | Lookup throws `ArgumentException` | Never ambiguous, the active hierarchy is walked |
| Allocation per call | None in the controller itself | None in the controller itself |

The one case that behaves differently in practice: with per-state controllers, a parent and its active sub-state that **share an event enum** only ran the handler of whichever controller you called. The unified controller offers the event to both (sub-state first) and both handlers run. If you relied on triggering only the parent's handler while the sub-state also registered the same event id, revisit that event design.

### Querying the Current Sub-State

`IStateController<EventIdT, SubStateIdT>.CurrentSubState` returns the active sub-state of a parent state (or the default sub-state when the parent is not running):

```csharp
var controller = stateMachine.StateControllerProvider
    .GetStateController<IStateController<AppEvent, InGameState>, AppState, AppEvent>(AppState.InGame);

InGameState currentSubState = controller.CurrentSubState;
```

**There is no replacement for this yet.** `IStateController` only triggers events. If you need the current sub-state outside the state machine, this deprecated path still works. Alternatively, track it from inside the state machine: record the id in the sub-state's `OnEnter`, or register an `IStateHandler<T>` for the sub-state types and record it in `OnEnterState`.

---

The sections below preserve the original documentation for the deprecated API.

### Retrieving a Controller

Use the `StateControllerProvider` on the built state machine:

```csharp
IStateMachine<GameState> stateMachine = /* ... build and start ... */;

// Get a controller for a specific state
var idleController = stateMachine.StateControllerProvider
    .GetStateController<IStateController<GameEvent>, GameState, GameEvent>(GameState.Idle);
```

`GetStateController<T, StateIdT, EventIdT>(stateId)` throws an `ArgumentException` when no controller exists for the state id, when none exists for that event enum type, or when the controller found does not implement `T`.

### Sub-State Controllers

Sub-state controllers are retrieved the same way, using the sub-state's own id and event type:

```csharp
var exploringController = stateMachine.StateControllerProvider
    .GetStateController<IStateController<InGameEvent>, InGameState, InGameEvent>(InGameState.Exploring);
```

If the same sub-state id and event type are registered under more than one parent state, that lookup is ambiguous and throws an `ArgumentException`. Use `IStateMachine<StateIdT>.StateController` in that case.

### Triggering Events

Call `TriggerEvent()` on the controller. It returns `true` if the event was handled:

```csharp
bool handled = idleController.TriggerEvent(GameEvent.Start);
```

Events are only handled if the associated state is currently active.

### Enumerating Controllers

You can retrieve all controllers for a given state enum type:

```csharp
var controllers = stateMachine.StateControllerProvider.GetStateControllers<GameState>();

foreach (var controllerInfo in controllers)
{
    GameState state = controllerInfo.State;
    object instance = controllerInfo.Instance;
    Type[] contractTypes = controllerInfo.ContractTypes;
}
```

Or retrieve all controllers regardless of state type:

```csharp
IEnumerable<BindInfo> allControllers = stateMachine.StateControllerProvider.StateControllers;
```

`StateControllers` lists the machine-level controller first (when the state machine was built with `StateMachineBuilder<StateIdT, EventIdT>`), followed by every state and sub-state controller.

### Suppressing the warning

If you need time to migrate, suppress CS0618 at the use site:

```csharp
#pragma warning disable CS0618 // Per-state controllers, see docs/DEPRECATED.md
var controller = stateMachine.StateControllerProvider
    .GetStateController<IStateController<GameEvent>, GameState, GameEvent>(GameState.Idle);
#pragma warning restore CS0618
```

Or for a whole project, in the `.csproj`:

```xml
<PropertyGroup>
  <NoWarn>$(NoWarn);CS0618</NoWarn>
</PropertyGroup>
```

Prefer the per-site form so that new uses of deprecated APIs still warn.
