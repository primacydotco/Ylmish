// https://docs.yjs.dev/api/shared-types/y.array#observing-changes-y.arrayevent
// https://github.com/fsprojects/FSharp.Data.Adaptive/commit/b2b8f7a7a5194762b294a461c03b498be0db38d0
// https://github.com/krauthaufen/RemoteAdaptive/blob/master/Program.fs
// https://github.com/krauthaufen/RemoteAdaptive/blob/master/Utilities.fs
// https://discord.com/channels/611129394764840960/624645480219148299/954458318418612354
// zaoa — 03/19/2022
// Can I ask why this way is prefered over AddCallback ?
// krauthaufen — 03/19/2022
// okay, so adaptive is a so-called push/pull implementation which uses a marking-phase for eagerly marking all affected things dirty and has a separate evaluation phase conceptually. When adding a callback that evaluates things you basically "fuse" both phases and the marking needs to execute user-code (like mapping functions, etc.)
// This isn't really a problem until you have dynamic dependency graphs in which case execution-order is not easy to determine anymore when a user changes multiple inputs at once. There are very sophisticated ways to solve that but (since eager evaluation wasn't really important yet) we opted for a straight-forward way of dealing with that which will, in some scenarios, perform rather slow
// however you might as well use it when your task isn't very performance-critical
// zaoa — 03/19/2022
// So how does the AddMarkingCallback approach differ?
// krauthaufen — 03/19/2022
// basically the callback is very cheap and no internal value gets updated during the marking-phase that way
// all the heavy-lifting is offloaded to a thread where execution order is simply dictated by the call-stack
// it's also a way to allow for clean batch-changes
// things like
// transact (fun () -> input.Add 10)
// transact (fun () -> input.Add 11)
// transact (fun () -> input.Add 12)

// I think in the "event-world" this is called debouncing
// we could arguably hide this implementation behind a combinator AList.observe : action : (State<'a> -> Delta<'a> -> unit) -> list : alist<'a> -> IDisposable

// https://github.com/krauthaufen/Fable.Elmish.Adaptive


// A different kind of wrapping
// https://github.com/krauthaufen/Fable.Elmish.Adaptive/blob/master/src/Fable.React.Adaptive/AdaptiveHelpers.fs


[<RequireQualifiedAccess>]
module Ylmish.Y.Delta

open FSharp.Data.Adaptive
open Yjs

open Ylmish.Adaptive

open Fable.Core.JS //

module internal ToAdaptive =
    let private generate f start count =
        let indexes = Index.generate f start count
        let ops' = IndexListDelta.ofList indexes
        let index' =
            indexes
            |> List.tryLast
            |> Option.map fst
            |> Option.defaultValue start
        index', ops'

    let folder getItem getCount ((index, ops) : Index * IndexListDelta<'b>) delta =
        match delta with
        | Y.Delta.Retain ret ->
            let index' = Index.increment index ret
            index', ops
        | Y.Delta.Delete del ->
            let index', ops' = generate (fun _ j -> j, ElementOperation<'b>.Remove) index (del - 1)
            index', IndexListDelta.combine ops ops'
        | Y.Delta.Insert (ins) ->
            let index', ops' = generate (fun i j -> j, ElementOperation<'b>.Set (getItem ins i)) index (getCount ins - 1)
            index', IndexListDelta.combine ops ops'

let toAdaptive folder (delta : Y.Delta<'a> ResizeArray ) : IndexListDelta<'b> =
    delta 
    |> Seq.fold folder (Index.zero, IndexListDelta.empty)
    |> snd

module internal OfAdaptive =
    let private (|Delta|) append empty (op : ElementOperation<'a>) =
        match op with
        | ElementOperation.Set c -> Y.Delta.Insert (append empty c)
        | ElementOperation.Remove -> Y.Delta.Delete 1

    let folder getPosition (append : 'b -> 'a -> 'b) empty (state : (Index * int * Y.Delta<'b>) list) (index : Index, op : ElementOperation<'a>) =
        match state, op with
        | [], Delta append empty delta
            when index = Index.zero ->
            console.log("case 1")
            (index, 0, delta) :: []
        | [], Delta append empty delta ->
            console.log("case 2")
            let pos = getPosition index// + 1
            console.log("case 2: pos", string pos)
            console.log("case 2: del", delta)
            console.log("case 2: ix", index)
            (index, pos + 1, delta) :: (index, pos, Y.Delta.Retain (pos (*+ 1 *))) :: []
        | (prevIndex, prevPos, Y.Delta.Insert (ins)) :: rest, ElementOperation.Set c
            when index = Index.after prevIndex ->
            console.log("case 3")
            (index, prevPos + 1, Y.Delta.Insert (append ins c)) :: rest
        | (prevIndex, prevPos, Y.Delta.Delete (del)) :: rest, ElementOperation.Remove
            when index = Index.after prevIndex ->
            console.log("case 4")
            (index, prevPos + 1, Y.Delta.Delete (del + 1)) :: rest
        | (prevIndex, prevPos, prevDelta) :: rest, Delta append empty delta
            when index = Index.after prevIndex ->
            console.log("case 5")
            (index, prevPos + 1, delta) :: (prevIndex, prevPos, prevDelta) :: rest
        | (prevIndex, prevPos, prevDelta) :: rest, Delta append empty delta ->
            console.log("case 6")
            let pos = getPosition index
            let ret = pos - prevPos
            console.log ("rest", rest)
            console.log ("prevDelta", prevDelta)
            console.log ("prevPos", prevPos)
            console.log ("pos " + string pos)
            console.log ("ret " + string ret)
            (index, pos, delta) :: (index, pos, Y.Delta.Retain ret) :: (prevIndex, prevPos, prevDelta) :: rest

let ofAdaptive folder (delta : IndexListDelta<'a>) : ResizeArray<Y.Delta<'b>> =      
    delta
    |> IndexListDelta.toList
    |> List.fold folder []
    |> List.map (fun (_,_,x) -> x)
    |> List.rev
    |> ResizeArray