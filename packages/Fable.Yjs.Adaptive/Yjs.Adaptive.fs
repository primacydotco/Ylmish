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
type Sentinel () =
    static member val Singleton = Sentinel ()

module Y =
    module Text =
        let toAdaptive (text : Y.Text) : char clist =
            let text' : char clist = text.toString () :> _ seq |> clist
            // TODO something with these disposables
            // https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/WeakRef

            let d1 =
                // https://docs.yjs.dev/api/delta-format
                let f (e : Y.TextEvent) (tx : Y.Transaction) =
                    let folder ((index, ops) : Index * IndexListDelta<char> ) = function
                        | Y.Event.Delta.Retain ret ->
                            Fable.Core.JS.console.log ("yjs retain ", ret)
                            let index' =
                                [ 0..ret ]
                                |> List.skip 1
                                |> List.fold (fun s _ -> Index.after s) index
                            index', ops
                        | Y.Event.Delta.Delete del ->
                            Fable.Core.JS.console.log ("yjs delete ", del)
                            let indexes =
                                [ 0..del ]
                                |> List.scan (fun s x -> Index.after s) index
                                |> List.skip 2 // skip '0' and initial state returned by scan
                            let ops' =
                                indexes
                                |> List.map (fun i -> i, ElementOperation<char>.Remove)
                                |> IndexListDelta.ofList
                            let index' =
                                indexes
                                |> List.tryLast
                                |> Option.defaultValue index
                            Fable.Core.JS.console.log ("yjs built ops ", $"%A{IndexListDelta.toList ops'}")
                            index', IndexListDelta.combine ops ops'
                        | Y.Event.Delta.Insert (U4.Case1 ins) ->
                            let indexes =
                                [ 0..ins.Length ]
                                |> List.scan (fun s x -> Index.after s) index
                                |> List.skip 2
                            let ops' =
                                indexes
                                |> List.mapi (fun charIndex opIndex -> opIndex, ElementOperation<char>.Set (ins[charIndex]))
                                |> IndexListDelta.ofList
                            let index' =
                                indexes
                                |> List.tryLast
                                |> Option.defaultValue index
                            index', IndexListDelta.combine ops ops'

                    if tx.meta.has (Some Sentinel.Singleton)
                    then ()
                    else
                        let _, ops = e.delta |> Seq.fold folder (Index.zero, IndexListDelta.empty)
                        transact (fun () -> text'.Perform ops)

                text.observe f
                {
                    new System.IDisposable with
                        member _.Dispose () = text.unobserve f
                }

            // Should there be an 'addmarkingcallback' for this?
            let d2 = text'.AddCallback(fun list delta ->
                Y.transact(Option.get text.doc, (fun tx -> (
                    ignore <| tx.meta.set (Some Sentinel.Singleton, Some ())
                    for (i, op) in delta do
                        let position = list |> IndexList.tryGetPosition i
                        match position, op with
                        | Some i, ElementOperation.Set ins ->
                            text.insert (i, string ins)
                        | Some i, ElementOperation.Remove ->
                            Fable.Core.JS.console.log ("adaptive delete at ", i)
                            text.delete (i, 1)
                        | None _, _ -> ()
                )))
            )

            text'
