# Ylmish

Real-time, collaborative apps with a delightful programming model.

Here lie libraries for integrating [Yjs](https://github.com/yjs/yjs) and [Elmish](https://github.com/elmish/elmish), via [Fable](https://github.com/fable-compiler/fable) and [Adaptive](https://github.com/fsprojects/FSharp.Data.Adaptive).

> 😸 I like building apps with Elmish, I like sharing state with Yjs. Let's conjugate!
> 
> I want (select) changes to an Elmish model to propagate to a Yjs document, and changes to a Yjs document to reflect in the Elmish model.



## Background

### Data and flow

Changes need to flow bi-directionally. That is, any changes made to **state data**, in Yjs, need to be made to **app data**, in Elmish, and (some) changes made to app data (such as through interactions with the app) need to be made to state data.

If we were to observe a running [**Elmish** loop](https://elmish.github.io/elmish/#dispatch-loop), we wouldn't see the operations being applied to the app's model. They're opaque to an observer because they're inside the `update` functions. Instead, we only have access to each successive `'model`, that is, the consequence of operations.

> For example, an Elmish `'model` may contain a list and an interaction may add two items into that list, but from the outside, we only see the new list, not two 'add' operations.

If we observe a **Yjs** document, we'd see all of the operations that occur and those are shared with peers for synchronization.

> For example, a Yjs `Y.Array` and an interaction may add two items to that array and from the outside, we can observe two add operations. (And we can combine all the operations to see the 'current' state of the array.)

**Design**

We need to bridge these two worlds, that is, we need to be able to go from our changes represented as success models to our changes represented as the operations themselves.

```
'model -> 'model -> 'operations
# which looks just like a classic differencing (diffing) function...
'document -> 'document -> 'delta
```

['Incremental computation'](https://github.com/fsprojects/Fabulous/issues/258#issue-391515540) has already been used where people want to build apps that use immutable data structures but performantly update a mutable DOM. Part of this work has been to [efficiently](https://github.com/fsharp/fslang-suggestions/issues/768) diff two models.

This led to [FSharp.Data.Adaptive](https://github.com/fsprojects/FSharp.Data.Adaptive), an fsharp implementation of incremental computing and our choice to bridge these two worlds.

```
┌─Elmish─┐    ┌─Adaptive──────┐    ┌─Yjs───┐
│        │ -> │               │ -> │       │
│ Model  │    │ AdaptiveModel │    │ Y.Doc │
│        │ <- │               | <- │       │
└────────┘    └───────────────┘    └───────┘
```

### Schema and codec

Our **app schema** describes the structure of our app data, that is, what is in memory while our app is running. Our **state schema** describes our state data, that is, what is persisted in browser storage and synchronized with peers via the app's companion.

Our state schema _must_ be decoupled from our app schema because:

1. Our app schema will change over time as our app changes. The state schema must be protected from breaking changes to the app schema.
   So, we need an anti-corruption layer between the two schemas.
1. Only a subset of app data needs to be persisted.
   So, we need to be able to select what should be persisted.

If we have this decoupling, we need an explicit description of how our app schema translates to our state schema and vice versa. We need to be able to _encode_ that app data as state data and _decode_ state data into app data.

**Design**

```
┌─Elmish─┐    ┌─Adaptive────────────────────────────┐    ┌─Yjs───┐
│        │ -> │ AdaptiveModel -> AMap<string, AVal> │ -> │       │
│ Model  │    │                                     │    │ Y.Doc │
│        │ <- │ AVal<Model>   <- AMap<string, AVal> | <- │       │
└────────┘    └─────────────────────────────────────┘    └───────┘
App schema    App schema shape   State schema shape    State schema
              └─────────────────────────────────────┘
                             codec
```


## TODO

1. IndexList starts at 1 is probably why tests are failing
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
