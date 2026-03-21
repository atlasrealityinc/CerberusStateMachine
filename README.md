# CerberusStateMachine

A fluent, type-safe state machine framework for .NET. Cerberus provides a builder API for defining states, events, and transitions with full support for hierarchical sub-states, custom lifecycle handlers, and dependency injection. Targets .NET Standard 2.0.

## Table of Contents

- [Features](#features)
- [Quick Start](#quick-start)
- [Defining States](#defining-states)
- [Building a State Machine](#building-a-state-machine)
- [Events and Transitions](#events-and-transitions)
- [Hierarchical State Machines (Sub-States)](#hierarchical-state-machines-sub-states)
- [State Handlers](#state-handlers)
- [State Controllers](#state-controllers)
- [Dependency Injection](#dependency-injection)

## Features

- **Fluent Builder API** - Define your state machine with a clean, chainable syntax
- **Type-Safe** - States, events, and transitions are all strongly typed using enums and generics
- **Event-Driven Transitions** - State changes are triggered through events with rich context
- **Hierarchical Sub-States** - Nest state machines within states to any depth
- **State Handlers** - Observe state lifecycle events with custom handlers
- **Dependency Injection** - Plug in your own IoC container for state and handler resolution

## Quick Start

```csharp
using Cerberus;
using Cerberus.Builder;

// 1. Define your state and event enums
public enum GameState { Idle, Playing, GameOver }
public enum GameEvent { Start, Lose }

// 2. Define your state classes
public class IdleState : State
{
    public override void OnEnter() => Console.WriteLine("Waiting to start...");
    public override void OnExit() => Console.WriteLine("Let's go!");
}

public class PlayingState : State
{
    public override void OnEnter() => Console.WriteLine("Game started!");
}

public class GameOverState : State
{
    public override void OnEnter() => Console.WriteLine("Game over!");
}

// 3. Build the state machine
IStateMachine<GameState> stateMachine = new StateMachineBuilder<GameState, GameEvent>()
    .State<IdleState, GameEvent>(GameState.Idle)
        .AddEvent(GameEvent.Start, e => e.ChangeState(GameState.Playing))
        .End()
    .State<PlayingState, GameEvent>(GameState.Playing)
        .AddEvent(GameEvent.Lose, e => e.ChangeState(GameState.GameOver))
        .End()
    .State<GameOverState, GameEvent>(GameState.GameOver)
        .AddEvent(GameEvent.Start, e => e.ChangeState(GameState.Playing))
        .End()
    .Build();

// 4. Start the state machine (enters the first defined state)
stateMachine.Start();

// 5. Trigger events via state controllers
var controller = stateMachine.StateControllerProvider
    .GetStateController<IStateController<GameEvent>, GameState, GameEvent>(GameState.Idle);

controller.TriggerEvent(GameEvent.Start); // Transitions from Idle -> Playing
```

## Defining States

States represent the individual behaviors of your state machine. Each state class implements lifecycle methods that are called when the state is entered or exited.

### Using the `IState` Interface

Implement `IState` directly for full control:

```csharp
public class MyState : IState
{
    public void OnEnter()
    {
        // Called when this state becomes active
    }

    public void OnExit()
    {
        // Called when leaving this state
    }
}
```

### Using the `State` Base Class

The `State` abstract class provides a convenient base with virtual methods, so you only need to override what you need:

```csharp
public class IdleState : State
{
    public override void OnEnter()
    {
        // Only override what you need
    }
}
```

### Typed States with Return Values

For states that need to return data from their lifecycle methods, use the generic base classes:

```csharp
// Same return type for both OnEnter and OnExit
public class LoadingState : State<float>
{
    public override float OnEnter()
    {
        return 0f; // initial progress
    }

    public override float OnExit()
    {
        return 1f; // completed
    }
}

// Different return types for OnEnter and OnExit
public class ProcessingState : State<string, int>
{
    public override string OnEnter()
    {
        return "started";
    }

    public override int OnExit()
    {
        return 42;
    }
}
```

## Building a State Machine

State machines are constructed using the fluent builder. You define states, wire up events, and then call `Build()` to produce the state machine.

### Basic Structure

```csharp
IStateMachine<MyState> stateMachine = new StateMachineBuilder<MyState, MyEvent>()
    .State<SomeState, MyEvent>(MyState.First)
        // ... configure events ...
        .End()
    .State<AnotherState, MyEvent>(MyState.Second)
        // ... configure events ...
        .End()
    .Build();

stateMachine.Start();
```

The first state added to the builder becomes the **default state** -- it is entered automatically when `Start()` is called.

### Builder Types

There are two builder variants depending on whether you need machine-level events:

- `StateMachineBuilder<StateIdT>` - For state machines that only use state-level events
- `StateMachineBuilder<StateIdT, EventIdT>` - Adds support for machine-level events (see [Machine-Level Events](#machine-level-events))

## Events and Transitions

State transitions in Cerberus are driven by events. You register event handlers that receive a context object, and call `ChangeState()` within them to trigger transitions.

### State-Level Events

Register events on individual states using `AddEvent()`. The handler receives an `IStateEvent<StateT, StateIdT>` context:

```csharp
new StateMachineBuilder<CharacterState, CharacterEvent>()
    .State<IdleState, CharacterEvent>(CharacterState.Idle)
        .AddEvent(CharacterEvent.Move, e =>
        {
            e.ChangeState(CharacterState.Running);
        })
        .AddEvent(CharacterEvent.Jump, e =>
        {
            e.ChangeState(CharacterState.Jumping);
        })
        .End()
    // ...
    .Build();
```

State-level events are only handled when that specific state is active.

### Event Context

The `IStateEvent<StateT, StateIdT>` passed to your event handler provides:

| Property / Method | Description |
|---|---|
| `StateInstance` | The current state object instance |
| `PreviousStateId` | The state that was active before this one |
| `ChangeState(stateId)` | Triggers a transition to another state |

This lets you write conditional transitions, access state data, or inspect where the state machine came from:

```csharp
.AddEvent(CharacterEvent.Land, e =>
{
    if (e.PreviousStateId == CharacterState.Jumping)
    {
        e.ChangeState(CharacterState.Landing);
    }
    else
    {
        e.ChangeState(CharacterState.Idle);
    }
})
```

### Machine-Level Events

Use `StateMachineBuilder<StateIdT, EventIdT>` to register events at the machine level. These are handled regardless of the current state:

```csharp
new StateMachineBuilder<GameState, GameEvent>()
    .State<PlayingState, GameEvent>(GameState.Playing)
        // ... state events ...
        .End()
    .State<PausedState, GameEvent>(GameState.Paused)
        .End()
    .AddEvent(GameEvent.Reset, e =>
    {
        // This can be triggered from any state
        e.ChangeState(GameState.Playing);
    })
    .Build();
```

Machine-level event handlers receive a `StateMachineEvent<StateIdT>` which provides `ChangeState()`.

## Hierarchical State Machines (Sub-States)

States can contain their own nested state machines. This is useful for modeling complex behaviors where a high-level state has its own internal states and transitions.

### Defining Sub-States

Use the three-type-parameter overload `.State<StateT, EventIdT, SubStateIdT>(stateId)` to declare a state with sub-states:

```csharp
public enum AppState { Menu, InGame }
public enum AppEvent { Play, Quit }
public enum InGameState { Exploring, Fighting, Paused }
public enum InGameEvent { EncounterEnemy, Win, Pause, Resume }

new StateMachineBuilder<AppState, AppEvent>()
    .State<MenuState, AppEvent>(AppState.Menu)
        .AddEvent(AppEvent.Play, e => e.ChangeState(AppState.InGame))
        .End()
    .State<InGameScreenState, AppEvent, InGameState>(AppState.InGame)
        .AddEvent(AppEvent.Quit, e => e.ChangeState(AppState.Menu))

        // Define sub-states within the parent
        .State<ExploringState, InGameEvent>(InGameState.Exploring)
            .AddEvent(InGameEvent.EncounterEnemy, e => e.ChangeState(InGameState.Fighting))
            .AddEvent(InGameEvent.Pause, e => e.ChangeState(InGameState.Paused))
            .End()
        .State<FightingState, InGameEvent>(InGameState.Fighting)
            .AddEvent(InGameEvent.Win, e => e.ChangeState(InGameState.Exploring))
            .End()
        .State<PausedState, InGameEvent>(InGameState.Paused)
            .AddEvent(InGameEvent.Resume, e => e.ChangeState(InGameState.Exploring))
            .End()

        .End()
    .Build();
```

When a parent state is entered, its **first defined sub-state** is automatically entered. In the example above, entering `AppState.InGame` will automatically enter `InGameState.Exploring`.

Sub-states can transition between each other without exiting the parent state. Exiting the parent state automatically cleans up the active sub-state.

### Multi-Level Nesting

Sub-states can have their own sub-states, allowing arbitrarily deep hierarchies:

```csharp
.State<ParentState, ParentEvent, ChildState>(ParentState.Main)
    .State<ChildA, ChildEvent, GrandchildState>(ChildState.A)
        .State<GrandchildX, GrandchildEvent>(GrandchildState.X)
            .End()
        .End()
    .End()
```

### Querying the Current Sub-State

When retrieving a state controller for a state with sub-states, use `IStateController<EventIdT, SubStateIdT>` to access the current sub-state:

```csharp
var controller = stateMachine.StateControllerProvider
    .GetStateController<IStateController<AppEvent, InGameState>, AppState, AppEvent>(AppState.InGame);

InGameState currentSubState = controller.CurrentSubState;
```

## State Handlers

State handlers let you observe state lifecycle transitions. They are called whenever a state is entered or exited, and are useful for cross-cutting concerns like logging, analytics, or resource management.

### Defining a Handler

Implement the `IStateHandler<StateT>` interface:

```csharp
public class LoggingHandler : IStateHandler<IState>
{
    public void OnEnterState(IState stateInstance)
    {
        Console.WriteLine($"Entered: {stateInstance.GetType().Name}");
    }

    public void OnExitState(IState stateInstance)
    {
        Console.WriteLine($"Exited: {stateInstance.GetType().Name}");
    }
}
```

The generic parameter `StateT` controls which states this handler applies to. Using `IState` applies it to all states. You can use a more specific type to target only certain states:

```csharp
// Only handles states that implement IMovementState
public class MovementHandler : IStateHandler<IMovementState>
{
    public void OnEnterState(IMovementState stateInstance)
    {
        stateInstance.StartMovement();
    }

    public void OnExitState(IMovementState stateInstance)
    {
        stateInstance.StopMovement();
    }
}
```

### Registering Handlers

Register handlers on the builder with `AddStateHandler<StateT, HandlerT>()`:

```csharp
new StateMachineBuilder<MyState>()
    .AddStateHandler<IState, LoggingHandler>()
    .AddStateHandler<IMovementState, MovementHandler>()
    .State<IdleState, MyEvent>(MyState.Idle)
        .End()
    // ...
    .Build();
```

## State Controllers

After building and starting a state machine, you interact with it at runtime through **state controllers**. Controllers let you trigger events on specific states from outside the state machine.

### Retrieving a Controller

Use the `StateControllerProvider` on the built state machine:

```csharp
IStateMachine<GameState> stateMachine = /* ... build and start ... */;

// Get a controller for a specific state
var idleController = stateMachine.StateControllerProvider
    .GetStateController<IStateController<GameEvent>, GameState, GameEvent>(GameState.Idle);
```

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

## Dependency Injection

By default, Cerberus creates state and handler instances using `Activator.CreateInstance`. To integrate with your own IoC container, implement `IStateMachineContainer` and pass it to the builder.

### Implementing `IStateMachineContainer`

```csharp
using Cerberus.IoC;

public class MyContainer : IStateMachineContainer
{
    private readonly IServiceProvider _serviceProvider;

    public MyContainer(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public T Resolve<T>()
    {
        return (T)_serviceProvider.GetService(typeof(T));
    }

    public object Resolve(Type type)
    {
        return _serviceProvider.GetService(type);
    }
}
```

### Using a Custom Container

Pass your container to the builder constructor:

```csharp
var container = new MyContainer(serviceProvider);

var stateMachine = new StateMachineBuilder<GameState, GameEvent>(container)
    .State<IdleState, GameEvent>(GameState.Idle)
        .End()
    .Build();
```

All state classes and state handlers will be resolved through your container, allowing constructor injection and lifetime management.
