module Yjs.Adaptive

open FSharp.Data.Adaptive
open Yjs
open Fable.Core

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
open Fable.Core.JS
open Fable.Core.JsInterop

type Sentinel () =
    static member val Singleton = Sentinel ()

module Index =

    let private generator (count : int) (f : int -> Index -> 'T) (i : int, j : Index) : ('T * (int * Index)) option =
        if i > count then None else
        Some (f i j, (i + 1, Index.after j))

    let generate f start count =
        List.unfold (generator count f) (0, start)

    let increment start i =
        generate (fun _ i -> i) start i 
        |> List.tryLast
        |> Option.defaultValue start

    let at = increment Index.zero

module Y =
    module Delta =
        module ToAdaptive =
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

        module OfAdaptive =
            let private (|Delta|) append empty (op : ElementOperation<'a>) =
                match op with
                | ElementOperation.Set c -> Y.Delta.Insert (append empty c)
                | ElementOperation.Remove -> Y.Delta.Delete 1

            let folder getPosition (append : 'b -> 'a -> 'b) empty (state : (Index * int * Y.Delta<'b>) list) (index : Index, op : ElementOperation<'a>) =
                match state, op with
                | [], Delta append empty delta
                    when index = Index.zero ->
                    (index, 0, delta) :: []
                | [], Delta append empty delta ->
                    let pos = getPosition index + 1
                    (index, pos, delta) :: (index, pos, Y.Delta.Retain pos) :: []
                | (prevIndex, prevPos, Y.Delta.Insert (ins)) :: rest, ElementOperation.Set c
                    when index = Index.after prevIndex ->
                    (index, prevPos + 1, Y.Delta.Insert (append ins c)) :: rest
                | (prevIndex, prevPos, Y.Delta.Delete (del)) :: rest, ElementOperation.Remove
                    when index = Index.after prevIndex ->
                    (index, prevPos + 1, Y.Delta.Delete (del + 1)) :: rest
                | (prevIndex, prevPos, prevDelta) :: rest, Delta append empty delta
                    when index = Index.after prevIndex ->
                    (index, prevPos + 1, delta) :: (prevIndex, prevPos, prevDelta) :: rest
                | (prevIndex, prevPos, prevDelta) :: rest, Delta append empty delta ->
                    let pos = getPosition index
                    let ret = pos - prevPos
                    (index, pos, delta) :: (index, pos, Y.Delta.Retain ret) :: (prevIndex, prevPos, prevDelta) :: rest

        let ofAdaptive folder (delta : IndexListDelta<'a>) : ResizeArray<Y.Delta<'b>> =      
            delta
            |> IndexListDelta.toList
            |> List.fold folder []
            |> List.map (fun (_,_,x) -> x)
            |> List.rev
            |> ResizeArray

    module Text =
        module Delta =
            let toAdaptive delta =
                let folder = Delta.ToAdaptive.folder (fun (str : string) i -> str[i]) (fun str -> str.Length)
                Delta.toAdaptive folder delta

            let ofAdaptive list delta =
                let folder =
                    Delta.OfAdaptive.folder
                        (fun i -> IndexList.tryGetPosition i list |> Option.get)
                        (fun a b -> a + System.Char.ToString b)
                        ""
                Delta.ofAdaptive folder delta

        // let ofAdaptive (atext : char clist) : Y.Text =
        //     let initial = string atext
        //     let ytext = Y.Text.Create (initial)
        //     let ysub =
        //         let f (e : Y.Text.Event) (tx : Y.Transaction) =
                    


    // module Text =
    //     let toAdaptive (text : Y.Text) : char clist =
    //         let text' : char clist = text.toString () :> _ seq |> clist
    //         // TODO something with these disposables
    //         // https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/WeakRef
    //         let mutable sentinel = false
    //         let d1 =
    //             // https://docs.yjs.dev/api/delta-format
    //             let f (e : Y.TextEvent) (tx : Y.Transaction) =
    //                 if sentinel then
    //                     sentinel <- false
    //                     () 
    //                 else
    //                 sentinel <- true
    //                 let delta' = TextEvent.toAdaptive e.delta
    //                 transact (fun () -> text'.Perform delta')

    //             text.observe f
    //             {
    //                 new System.IDisposable with
    //                     member _.Dispose () = text.unobserve f
    //             }

    //         let d2 = text'.AddCallback(fun list delta ->
    //             if sentinel then
    //                 sentinel <- false
    //             else
    //             sentinel <- true
    //             let delta' = TextEvent.ofAdaptive (fun i -> list.TryGetPosition i |> Option.get) delta
    //             text.applyDelta delta'
    //         )

    //         text'
