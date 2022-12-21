[<RequireQualifiedAccess>]
module Ylmish.Y.Text

open FSharp.Data.Adaptive
open Yjs

open Fable.Core.JsInterop // for `delta?sentinel`
open Fable.Core.JS // for `console.log`

type private Sentinel () =
    static member val Singleton = Sentinel ()

module Delta =
    let toAdaptive delta =
        let folder = Delta.ToAdaptive.folder (fun (str : string) i -> str[i]) (fun str -> str.Length)
        Delta.toAdaptive folder delta

    let ofAdaptive list delta =
        let folder =
            Delta.OfAdaptive.folder
                (fun i ->
                    // This seems like a silly way to go about it, but
                    // Remove
                    //   list, delta   = IndexList [_; _; _; _], IndexListDelta [Rem(0x3/0x4); Rem(0x7/0x8)]
                    //   list', delta' = IndexList [_; _]      , IndexListDelta [Rem(0x3/0x4); Rem(0x7/0x8)]
                    //   IndexList.tryGetPosition i list  = Some _
                    //   IndexList.tryGetPosition i list' = None
                    // Insert 
                    //   list, delta   = IndexList [a; b; d]   , IndexListDelta [[0xD/0x10]<-c]
                    //   list', delta' = IndexList [a; b; c; d], IndexListDelta [[0xD/0x10]<-c]
                    //   IndexList.tryGetPosition i list  = None
                    //   IndexList.tryGetPosition i list' = Some _
                    let list', _ = IndexList.applyDelta list delta
                    let index =
                        IndexList.tryGetPosition i list'
                        |> Option.orElse (IndexList.tryGetPosition i list)
                        |> Option.get
                    index)
                (fun a b -> a + System.Char.ToString b)
                ""
        Delta.ofAdaptive folder delta

let ofAdaptive (atext : char clist) : Y.Text =
    let initial = System.String.Concat(atext)
    let ytext = Y.Text.Create (initial)
    let mutable lock = false
    let _ = ytext.observe (fun event _ ->
        console.log ("YTEXT start", lock)
        if lock then lock <- false else
        lock <- true
        console.log ("YTEXT callback")
        let delta' = Delta.toAdaptive event.delta
        delta'?sentinel <- Sentinel.Singleton
        transact (fun () -> atext.Perform delta')
    )
    let _ = atext.AddWeakCallback(fun list delta ->
        console.log ("ATEXT start", lock)
        if lock then lock <- false else
        lock <- true
        console.log ("ATEXT callback")
        if delta?sentinel = Sentinel.Singleton then () else
        match ytext.doc with
        | None ->
            console.warn ($"\
                Y.Text was not added to a Y.Doc so changes to char clist won't be applied.\n\
                %A{list} %A{delta}")
            lock <- false
        | Some doc ->
            let delta' = Delta.ofAdaptive list delta
            console.log ("got delta", delta')
            doc.transact (fun tx ->
                let _ = tx.meta.set ("sentinel", Sentinel.Singleton)
                ytext.applyDelta delta'
            )
    )
    ytext
                


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
