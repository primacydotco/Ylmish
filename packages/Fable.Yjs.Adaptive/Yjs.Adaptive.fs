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

module Delta =
    let generate f start count =
        let indexes = Index.generate f start count
        let ops' = IndexListDelta.ofList indexes
        let index' =
            indexes
            |> List.tryLast
            |> Option.map fst
            |> Option.defaultValue start
        index', ops'

module Y =
    module TextEvent =
        let toAdaptive (delta : Y.Event.Delta ResizeArray) =
            let folder ((index, ops) : Index * IndexListDelta<char>) = function
            | Y.Event.Delta.Retain ret ->
                let index' = Index.increment index ret
                index', ops
            | Y.Event.Delta.Delete del ->
                let index', ops' = Delta.generate (fun _ j -> j, ElementOperation<char>.Remove) index (del - 1)
                index', IndexListDelta.combine ops ops'
            | Y.Event.Delta.Insert (U4.Case1 ins) ->
                let index', ops' = Delta.generate (fun i j -> j, ElementOperation<char>.Set (ins[i])) index (ins.Length - 1)
                index', IndexListDelta.combine ops ops'
            
            delta 
            |> Seq.fold folder (Index.zero, IndexListDelta.empty)
            |> snd

    module Text =
        let toAdaptive (text : Y.Text) : char clist =
            let text' : char clist = text.toString () :> _ seq |> clist
            // TODO something with these disposables
            // https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/WeakRef
            let mutable sentinel = false
            let d1 =
                // https://docs.yjs.dev/api/delta-format
                let f (e : Y.TextEvent) (tx : Y.Transaction) =
                    if sentinel then
                        sentinel <- false
                        () 
                    else
                    sentinel <- true
                    let ops = TextEvent.toAdaptive e.delta
                    transact (fun () -> text'.Perform ops)

                text.observe f
                {
                    new System.IDisposable with
                        member _.Dispose () = text.unobserve f
                }

            // Should there be an 'addmarkingcallback' for this?
            let d2 = text'.AddCallback(fun list delta ->
                if sentinel then
                    sentinel <- false
                else
                sentinel <- true
                Y.transact(Option.get text.doc, (fun tx -> (
                    ignore <| tx.meta.set (Some Sentinel.Singleton, Some ())
                    for (i, op) in delta do
                        let position = list |> IndexList.tryGetPosition i
                        match position, op with
                        | Some i, ElementOperation.Set ins ->
                            Fable.Core.JS.console.log ("adaptive insert at ", i)
                            text.insert (i, string ins)
                        | Some i, ElementOperation.Remove ->
                            Fable.Core.JS.console.log ("adaptive delete at ", i)
                            text.delete (i, 1)
                        | None _, _ -> ()
                )))
            )

            text'
