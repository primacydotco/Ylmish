module rec Ylmish.Y

open System

open FSharp.Data.Adaptive
open Yjs

open Ylmish.Adaptive
open Ylmish.Disposables

module A =

    /// A value inside an Adaptive element
    type Value =
        | String of string
        | Text of IndexList<char>

    /// An Adapative element.
    type Element = Ylmish.Adaptive.Codec.Element<Value>

module Y =
    module Y =
        // TODO move this type to Yjs Fable bindings
        // Y.Map, Y.Array only support these element types anyway
        // > Collection used to store key-value entries in an unordered manner. Keys are always represented as UTF-8 strings. Values can be any value type supported by Yrs: JSON-like primitives as well as shared data types.
        // https://github.com/y-crdt/y-crdt/blob/279cd56d7472fbb41af743ecf28f552024cabd65/yrs/src/types/map.rs#L15-L17
        // Null is a proper value
        // https://github.com/yjs/yjs/blob/9afc5cf61531f19d9caceb590002392ad393ed62/tests/y-map.tests.js#L106
        [<Fable.Core.Erase>]
        [<RequireQualifiedAccess>]
        type Element =
            | String of string
            | Array of Y.Array<Element option>
            | Map of Y.Map<Element option>

    /// A Y element.
    type Element = Y.Element

[<RequireQualifiedAccess>]
module Delta =

    open Fable.Core.JS //

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
                console.log("case 1")
                (index, 0, delta) :: []
            | [], Delta append empty delta ->
                console.log("case 2")
                let pos = getPosition index
                let toRetain = pos + 1 // +1 because the position is an index, but we need the count of items to retain
                console.log("case 2: pos", string pos)
                console.log("case 2: del", delta)
                console.log("case 2: ix", index)
                console.log("case 2: toRetain", toRetain)
                (index, pos + 1, delta) :: (index, pos, Y.Delta.Retain toRetain) :: []
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

[<RequireQualifiedAccess>]
module Text =
    // TODO this should be private but right now there's tests for Delta.ofAdaptive/Delta.toAdaptive via the text implementation.
    module Impl =
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

    // TODO are we using the right callbacks with Adaptive?
    //  https://docs.yjs.dev/api/shared-types/y.array#observing-changes-y.arrayevent
    //  https://github.com/fsprojects/FSharp.Data.Adaptive/commit/b2b8f7a7a5194762b294a461c03b498be0db38d0
    //  https://github.com/krauthaufen/RemoteAdaptive/blob/master/Program.fs
    //  https://github.com/krauthaufen/RemoteAdaptive/blob/master/Utilities.fs
    //  https://discord.com/channels/611129394764840960/624645480219148299/954458318418612354
    //  > zaoa — 03/19/2022
    //  > Can I ask why this way is prefered over AddCallback ?
    //  > krauthaufen — 03/19/2022
    //  > okay, so adaptive is a so-called push/pull implementation which uses a marking-phase for eagerly marking all affected things dirty and has a separate evaluation phase conceptually. When adding a callback that evaluates things you basically "fuse" both phases and the marking needs to execute user-code (like mapping functions, etc.)
    //  > This isn't really a problem until you have dynamic dependency graphs in which case execution-order is not easy to determine anymore when a user changes multiple inputs at once. There are very sophisticated ways to solve that but (since eager evaluation wasn't really important yet) we opted for a straight-forward way of dealing with that which will, in some scenarios, perform rather slow
    //  > however you might as well use it when your task isn't very performance-critical
    //  > zaoa — 03/19/2022
    //  > So how does the AddMarkingCallback approach differ?
    //  > krauthaufen — 03/19/2022
    //  > basically the callback is very cheap and no internal value gets updated during the marking-phase that way
    //  > all the heavy-lifting is offloaded to a thread where execution order is simply dictated by the call-stack
    //  > it's also a way to allow for clean batch-changes
    //  > things like
    //  > transact (fun () -> input.Add 10)
    //  > transact (fun () -> input.Add 11)
    //  > transact (fun () -> input.Add 12)
    //  > I think in the "event-world" this is called debouncing
    //  > we could arguably hide this implementation behind a combinator AList.observe : action : (State<'a> -> Delta<'a> -> unit) -> list : alist<'a> -> IDisposable
    // Alternatively, a different kind of wrapping:
    //  https://github.com/krauthaufen/Fable.Elmish.Adaptive/blob/master/src/Fable.React.Adaptive/AdaptiveHelpers.fs
    let attach (atext : char clist) (ytext : Y.Text) : IDisposable =
        let mutable sentinel = false
        let d1 =
            // https://docs.yjs.dev/api/delta-format
            let f (e : Y.Text.Event) (_ : Y.Transaction) =
                if sentinel then
                    sentinel <- false
                    () 
                else
                sentinel <- true
                let delta' = Impl.toAdaptive e.delta
                transact (fun () -> atext.Perform delta')

            ytext.observe f
            {
                new System.IDisposable with
                    member _.Dispose () = ytext.unobserve f
            }

        let d2 = atext.AddCallback(fun list delta ->
            if sentinel then
                sentinel <- false
            else
            sentinel <- true
            let delta' = Impl.ofAdaptive list delta
            //match ytext.doc with
            //| None ->
            //    console.warn ($"\
            //        Y.Text was not added to a Y.Doc so changes to char clist won't be applied.\n\
            //        %A{list} %A{delta}")
            //    sentinel <- false
            //| Some doc ->
            //    let delta' = Delta.ofAdaptive list delta
            //    console.log ("got delta", delta')
            //    doc.transact (fun tx ->
            //        let _ = tx.meta.set ("sentinel", Sentinel.Singleton)
            //        ytext.applyDelta delta'
            //    )
            ytext.applyDelta delta'
        )

        new CompositeDisposable (d1, d2)

    let ofAdaptive (atext : char clist) : Y.Text =
        let initial = System.String.Concat(atext)
        let ytext = Y.Text.Create (initial)
        // TODO something with these disposables
        //  At first I thought we want something like
        //  https://learn.microsoft.com/en-us/dotnet/api/system.runtime.compilerservices.conditionalweaktable-2?view=net-7.0
        //  where when the subject is cleaned-up, our subscription is disposed.
        //  But then I realised the subject would never be cleaned-up _because_ of our subscription. 
        //  Is this useful?
        //  https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/WeakRef
        let _ = attach atext ytext
        ytext
                
    let toAdaptive (ytext : Y.Text) : char clist =
        let atext : char clist = ytext.toString () :> _ seq |> clist
        let _ = attach atext ytext
        atext

[<RequireQualifiedAccess>]
module Array =
    module Impl =
        let toAdaptive delta =
            let folder = Delta.ToAdaptive.folder (fun (items : Array<Y.Element option>) i -> items[i] |> Option.map Element.toAdaptive) (fun items -> items.Count)
            Delta.toAdaptive folder delta

        let ofAdaptive list delta =
            let folder =
                Delta.OfAdaptive.folder
                    (fun i ->
                        // NOTE: this is the same as Text.Impl.ofAdaptive.
                        // See comments there.
                        let list', _ = IndexList.applyDelta list delta
                        let index =
                            IndexList.tryGetPosition i list'
                            |> Option.orElse (IndexList.tryGetPosition i list)
                            |> Option.get
                        index)
                    (fun (a : Yjs.Array<_>) b -> a.Add(b); a)
                    Array.empty
            Delta.ofAdaptive folder delta

    let attach (alist : clist<A.Element option>) (yarray : Y.Array<Y.Element option>) : IDisposable =
        let mutable sentinel = false
        let d1 =
            // https://docs.yjs.dev/api/delta-format
            let f (e : Y.Array.Event<Y.Element option>) (_ : Y.Transaction) =
                if sentinel then
                    sentinel <- false
                    () 
                else
                sentinel <- true
                let delta' = Impl.toAdaptive e.delta
                transact (fun () -> alist.Perform delta')

            yarray.observe f
            {
                new System.IDisposable with
                    member _.Dispose () = yarray.unobserve f
            }

        let d2 = alist.AddCallback(fun list delta ->
            if sentinel then
                sentinel <- false
            else
            sentinel <- true
            let delta' = Impl.ofAdaptive list delta
            //match ytext.doc with
            //| None ->
            //    console.warn ($"\
            //        Y.Text was not added to a Y.Doc so changes to char clist won't be applied.\n\
            //        %A{list} %A{delta}")
            //    sentinel <- false
            //| Some doc ->
            //    let delta' = Delta.ofAdaptive list delta
            //    console.log ("got delta", delta')
            //    doc.transact (fun tx ->
            //        let _ = tx.meta.set ("sentinel", Sentinel.Singleton)
            //        ytext.applyDelta delta'
            //    )
            failwith "yarray.applyDelta does not exist so we need to implement our own here."
            //yarray.applyDelta delta'
        )

        new CompositeDisposable (d1, d2)

    let toAdaptive (yarray : Y.Array<Y.Element option>) : clist<A.Element option> =
        let alist =
            yarray
            |> Seq.map (Option.map Element.toAdaptive)
            |> clist
        let _ = attach alist yarray
        alist

    let ofAdaptive (alist : alist<A.Element option>) : Y.Array<Y.Element option> =
        let yarray = Y.Array.Create ()
        do AList.force alist
            |> IndexList.map (Option.map Element.ofAdaptive)
            |> IndexList.toArray
            |> yarray.push
        yarray

module Map =
    let toAdaptive (ymap : Y.Map<Y.Element>) : amap<string, A.Element> =
        failwith "not implemented"

    let ofAdaptive (amap : amap<string, A.Element>) : Y.Map<Y.Element> =
        failwith "not impl"

module Element =
    let toAdaptive (yelement : Y.Element) : A.Element =
        match yelement with
        | Y.Element.Array yarray -> A.Element.AList <| Array.toAdaptive yarray

    let ofAdaptive (aelement : A.Element) : Y.Element =
        match aelement with
        | A.Element.AList alist -> Y.Element.Array <| Array.ofAdaptive alist