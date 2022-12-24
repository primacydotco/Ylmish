# Ylmish

Real-time, collaborative apps with a delightful programming model.

Here lie libraries for integrating [Yjs](https://github.com/yjs/yjs) and [Elmish](https://github.com/elmish/elmish), via [Fable](https://github.com/fable-compiler/fable) and [Adaptive](https://github.com/fsprojects/FSharp.Data.Adaptive).

> 😸 I like building apps with Elmish, I like sharing state with Yjs. Let's conjugate!
> 
> I want (select) changes to an Elmish model to propagate to a Yjs document, and changes to a Yjs document to reflect in the Elmish model.

## Background

### Data and communication

Our **app data** describes what is in memory while our app is running. Our **state data** describes what is persisted in browser storage and synchronized with peers.

Changes need to be communicated bi-directionally. That is, any changes made to **state data**, in Yjs, need to be made to **app data**, in Elmish, and (some) changes made to app data (such as through interactions with the app) need to be made to state data.

If we were to observe a running [**Elmish** loop](https://elmish.github.io/elmish/#dispatch-loop), we wouldn't see the operations being applied to the app's model. They're opaque to an observer because they're inside the `update` functions. Instead, we only have access to each successive `'model`, that is, the consequence of operations.

> For example, an Elmish `'model` may contain a list and an interaction may add two items into that list, but from the outside, we only see the new list, not two 'add' operations.

If we observe a **Yjs** document, we'd see all of the operations that occur and those are shared with peers for synchronization.

> For example, a Yjs `Y.Array` and an interaction may add two items to that array and from the outside, we can observe two add operations. (And we can combine all the operations to see the 'current' state of the array.)

We need to bridge these two worlds, that is, we need to be able to go from our changes represented as successive models to our changes represented as the operations themselves.

```
'model -> 'model -> 'operations
# which looks just like a classic differencing (diffing) function...
'document -> 'document -> 'delta
```

['Incremental computation'](https://github.com/fsprojects/Fabulous/issues/258#issue-391515540) has already been used where people want to build apps that use immutable data structures but performantly update a mutable DOM. Part of this work has been to [efficiently](https://github.com/fsharp/fslang-suggestions/issues/768) diff two models.

**Design**

We bridge Elmish and Yjs through an intermediate, incremental model using [FSharp.Data.Adaptive](https://github.com/fsprojects/FSharp.Data.Adaptive) (an fsharp implementation of incremental computing) and [Adaptify](https://github.com/krauthaufen/Adaptify) (to incrementalize an existing Elmish model). 

Successive Elmish models are used to update the incremental model. The (calculated) changes to the incremental model are observed and (the deltas) are applied to the Yjs document.

```
┌─Elmish─┐         ┌─Adaptive─────────┐         ┌─Yjs───┐
│        │ --[1]-> │                  │ --[2]-> │       │
│ Model  │         │ IncrementalModel │         │ Y.Doc │
│        │ <-[4]-- │                  | <-[3]-- │       │
└────────┘         └──────────────────┘         └───────┘
App data                                    State data

Using Ylmish.Program.withYlmish:
[1] Successive Elmish models are used to update the incremental model.
[2] Changes to the incremental model are observed and (the deltas) are applied to the Yjs document.
[3] Changes to the Yjs document are observed and the deltas are applied to the incremental model
[4] Changes to the incremental model are observed and a updated Elmish model is set for each.
```

### Schema and codec

Our **app schema** describes the structure of our app data, that is, what is in memory while our app is running. Our **state schema** describes the structure of our state data, that is, what is persisted in browser storage and synchronized with peers via the app's companion.

Our state schema _must_ be decoupled from our app schema because:

1. Our app schema will change over time as our app changes. The state schema must be protected from breaking changes to the app schema.
   So, we need an anti-corruption layer between the two schemas.
1. Only a subset of app data needs to be persisted.
   So, we need to be able to select what should be persisted.

If we have this decoupling, we need an explicit description of how our app schema translates to our state schema and vice versa. We need to be able to _encode_ that app data as state data and _decode_ state data into app data.

**Design**

We provide `Ylmish.Adaptive.Codec` for writing encoders and decoders. 

```
┌─Adaptive─────────────────────────────────┐
│ AdaptiveModel --[1]-> AMap<string, AVal> │
│                                          │
│ AVal<Model>   <-[2]-- AMap<string, AVal> |
└──────────────────────────────────────────┘
App schema                      State schema

Using Ylmish.Adaptive.Codec:
[1] .Encoder
[2] .Decoder
```


## TODO

1. IndexList starts at 1 is probably why tests are failing
1. We need get-or-insert semantics for nested Y types so our maps and arrays aren't overwritten by two clients initializing shared types.
   We could code around this it by only using top-level maps and arrays, representing nested types by name.
   For example, `Y.Doc.getMap('x.y.z')` to represent a map `z` inside a map `y` inside the top-level map `x`.
   Maybe the [key-value type] will support this?(https://github.com/yjs/yjs/issues/255).
1. Value could be erased
   ```fsharp
   [<Erase>]
   type Value =
   | Map of Y.Map<Value>
   | Array of Y.Map<Array>
   | Text of Y.Text
   | String of string
   | Number of ?
   | Object of obj
   ```
   https://github.com/fable-compiler/Fable/issues/2492
1. Investigate supporting [Ycs](https://github.com/yjs/ycs) or [Yrs](https://github.com/y-crdt/y-crdt (with a FFI binding) (in addition to [Yjs](https://github.com/yjs/yjs) (./src/Fable.Yjs)).
