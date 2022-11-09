module Yjs.Adaptive.Elmish

module Sample =
    open FSharp.Data.Adaptive
    open FSharp.Data.Traceable

    // [<ModelType>]
    type Thing =
        {
            name  : string
            value : int
        }

    // [<ModelType>]
    type Model =
        {
            foo : int
            bar : string
            things : IndexList<Thing>
        }

    [<System.Diagnostics.CodeAnalysis.SuppressMessage("NameConventions", "*")>]
    type AdaptiveThing(value : Thing) =
        let _name_ = FSharp.Data.Adaptive.cval(value.name)
        let _value_ = FSharp.Data.Adaptive.cval(value.value)
        let mutable __value = value
        let __adaptive = FSharp.Data.Adaptive.AVal.custom((fun (token : FSharp.Data.Adaptive.AdaptiveToken) -> __value))
        static member Create(value : Thing) = AdaptiveThing(value)
        // static member Unpersist = Adaptify.Unpersist.create (fun (value : Thing) -> AdaptiveThing(value)) (fun (adaptive : AdaptiveThing) (value : Thing) -> adaptive.Update(value))
        member __.Update(value : Thing) =
            if Microsoft.FSharp.Core.Operators.not((FSharp.Data.Adaptive.ShallowEqualityComparer<Thing>.ShallowEquals(value, __value))) then
                __value <- value
                __adaptive.MarkOutdated()
                _name_.Value <- value.name
                _value_.Value <- value.value
        member __.Current = __adaptive
        member __.name = _name_ :> FSharp.Data.Adaptive.aval<Microsoft.FSharp.Core.string>
        member __.value = _value_ :> FSharp.Data.Adaptive.aval<Microsoft.FSharp.Core.int>

    [<System.Diagnostics.CodeAnalysis.SuppressMessage("NameConventions", "*")>]
    type AdaptiveModel(value : Model) =
        let _foo_ = FSharp.Data.Adaptive.cval(value.foo)
        let _bar_ = FSharp.Data.Adaptive.cval(value.bar)
        let _things_ =
            let inline __arg2 (m : AdaptiveThing) (v : Thing) =
                m.Update(v)
                m
            FSharp.Data.Traceable.ChangeableModelList(value.things, (fun (v : Thing) -> AdaptiveThing(v)), __arg2, (fun (m : AdaptiveThing) -> m))
        let mutable __value = value
        let __adaptive = FSharp.Data.Adaptive.AVal.custom((fun (token : FSharp.Data.Adaptive.AdaptiveToken) -> __value))
        static member Create(value : Model) = AdaptiveModel(value)
        // static member Unpersist = Adaptify.Unpersist.create (fun (value : Model) -> AdaptiveModel(value)) (fun (adaptive : AdaptiveModel) (value : Model) -> adaptive.Update(value))
        member __.Update(value : Model) =
            if Microsoft.FSharp.Core.Operators.not((FSharp.Data.Adaptive.ShallowEqualityComparer<Model>.ShallowEquals(value, __value))) then
                __value <- value
                __adaptive.MarkOutdated()
                _foo_.Value <- value.foo
                _bar_.Value <- value.bar
                _things_.Update(value.things)
        member __.Current = __adaptive
        member __.foo = _foo_ :> FSharp.Data.Adaptive.aval<Microsoft.FSharp.Core.int>
        member __.bar = _bar_ :> FSharp.Data.Adaptive.aval<Microsoft.FSharp.Core.string>
        member __.things = _things_ :> FSharp.Data.Adaptive.alist<AdaptiveThing>

module Codec = 
    open Yjs

    let (|Text|_|) (obj : obj) =
        if typeof<Y.Text>.IsInstanceOfType(obj)
        then Some (obj :?> Y.Text)
        else None

    let (|Value|_|) (obj : obj) =
        if typeof<Y.AbstractType>.IsInstanceOfType(obj)
        then Some (obj :?> Y.AbstractType)
        else None

    let (|Map|_|) (obj : obj) =
        if typeof<Y.Map<obj>>.IsInstanceOfType(obj)
        then Some (obj :?> Y.Map<obj>)
        else None

// Encode should go from an aval -> yval
module Encode =
    open FSharp.Data.Adaptive
    open Yjs

    [<RequireQualifiedAccess>]
    type Encoding =
        | Text of Y.Text
        | Value of Y.AbstractType

    // type Encoder<'a> = ()
    [<RequireQualifiedAccess>]
    type Encoder =
        | Text of (Y.Text -> unit)
        | Value of (Y.AbstractType -> unit)
        | Array of (Y.Array<obj> -> unit)
        | Map of (Y.Map<obj> -> unit)

    let object (props : (string * (Encoder)) list) =
        Encoder.Map <| fun init ->
        for (key, encode) in props do
            match encode, init.get key with
            | Encoder.Text encode, Some (Codec.Text value) ->
                encode value
            | Encoder.Text encode, None ->
                let value = Y.Text.Create ()
                encode value
                ignore <| init.set (key, value)
            // | Encoder.Value encode, Some (Codec.Value value) ->
            //     encode value
            // | Encoder.Value encode, None ->
            //     let value = Y.AbstractType.Create ()
            //     encode value
            //     ignore <| init.set (key, value)
            ()

    let value (a : 'a aval) = Encoder.Value <| fun (init : Y.AbstractType) ->
        let boop = a.addc
    let text (a : char alist) = Encoder.Text <| fun (init : Y.Text) -> ()

    let array (item : 'a -> Encoder) (a : 'a alist) =
        Encoder.Array <| fun (init : Y.Array<obj>) ->
        ()


    // Run


    let from (key, doc : Y.Doc) = function
        | Encoder.Text encode ->
            encode <| doc.getText key
        | Encoder.Map encode ->
            encode <| doc.getMap key
        | Encoder.Array encode ->
            encode <| doc.getArray key



// module Decode =
//     open Yjs

//     [<RequireQualifiedAccess>]
//     type PathSegment = 
//         | ObjectKey   of string
//         | ArrayIndex  of int
//         override x.ToString () =
//             match x with
//             | ObjectKey  k -> k
//             | ArrayIndex i -> sprintf "[%i]" i

//     type Path = PathSegment list

//     [<RequireQualifiedAccess>]
//     type Element =
//         | Map of Y.Map<obj>
//         | Array of Y.Array<obj>
//         | Text of Y.Text
//         | Value of obj

//     module Element =
        
//         type Kind =
//             | MapKind
//             | ArrayKind
//             | TextKind

//         let kind (element : Element) = 
//             match element with
//             | Element.Map _ -> MapKind
//             | Element.Array _ -> ArrayKind
//             | Element.Text _ -> TextKind

//     type DecodingError =
//         | NotConvertible of {| Path : Path; Actual : Element.Kind; Expected : Element.Kind list |}
//         | MissingProperty of {| Path : Path |}

//     type Decoder<'a, 'out> =  Path * 'a -> Result<'out, DecodingError>
//     // type ValueDecoder<'out> = Path * Element -> Result<'out, DecodingError>

//     module Decode =
//         open FSharp.Data.Adaptive

//         let root : Decoder<_,_> = Ok

//         let inline run (nav : Decoder<_,_>) dep = nav dep

//         /// Creates a Decoder<'a> from an 'a.
//         let ok c : Decoder<_,_> = fun _ -> Ok c
        
//         /// Creates a Decode<_> from an error.
//         let error e : Decoder<_,_> = fun _ -> Error e

//         let from (element : Element) : Decoder<_,_> = ok ([], element)

//         let fromArray (a : Y.Array<obj>) = from (Element.Array a)

//         let fromMap (a : Y.Map<obj>) = from (Element.Map a)

//         /// Lift a Result<'a, Failure> to a Decode<'a>.
//         let ofResult (result : Result<'a, DecodingError>) : Decoder<_,_> =
//             fun _ -> result

//         let bind (f : 'a -> Decoder<_,'b>) (a : Decoder<_,'a>): Decoder<_,'b> =
//             fun dep -> a dep |> Result.bind (fun s -> f s dep)

//         let map (f : 'a -> 'b) (a : Decoder<_,'a>) : Decoder<_, 'b> =
//             a >> Result.map f

//         let value (f : 'a -> Result<'b, DecodingError>) : Decoder<_, 'a> -> Decoder<_,'b> =
//             bind (f >> ofResult)

//         let optional f : Decoder<_,_> -> Decoder<_,_> =
//             value <| fun (path, element) ->
//                 match element with
//                 | Some el -> f (path, el) |> Result.map Some
//                 | None    -> Ok None

//         let required f : Decoder<_,_> -> Decoder<_,_> =
//             value <| fun (path, element) ->
//                 match element with
//                 | Some el -> f (path, el)
//                 | None    -> Error <| MissingProperty {| Path = path |}

//         let text : Decoder<Element, char clist> = fun (path, element) ->
//             match element with
//             | Element.Text text -> Ok ()
//             | _ -> Error <| NotConvertible {|
//                 Path = path
//                 Actual = Element.kind element
//                 Expected = [ Element.TextKind ] |}

//         let object (_ : Decoder<_,'a>) : Decoder<_,cmap<string, 'a>> = fun (path, element) ->
//             match element with
//             | Element.Map map ->
//                 ()
//             | element -> Error <| NotConvertible {|
//                     Path = path
//                     Actual = Element.kind element
//                     Expected = [ Element.ArrayKind ]
//                 |}

//         let array (item : Decoder<Element,'a>) : Decoder<Element, 'a clist> = fun (path, element) ->
//             match element with
//             | Element.Array array ->
//                 ()
//             | element -> Error <| NotConvertible {|
//                     Path = path
//                     Actual = Element.kind element
//                     Expected = [ Element.ArrayKind ]
//                 |}
        
//         let string : Decoder<Element, string> = fun (path, element) ->
//             match element with
//             | Element.Value (:? string as value) -> Ok value
//             | _ -> Error <| NotConvertible {|
//                 Path = path
//                 Actual = Element.kind element
//                 Expected = [ Element.TextKind ] |}

//         // Integral support like...
//         // https://github.com/thoth-org/Thoth.Json/blob/main/src/Decode.fs#L160
                
//         let key (_ : string) : Decoder<_,_> -> Decoder<_,_> =
//             failwith "Not impl"

//     type DecodeBuilder() =
//         member _.Return x = Decode.ok x
//         member _.Bind (m, f) = Decode.bind f m
//         member _.ReturnFrom m = m
//         member _.Zero() = Decode.ok ()
//         member _.Run f = f


//     let decode = DecodeBuilder()

//     let (?) decode path = Decode.key path decode



// module ExampleDecoding =
//     open Decode
//     open Yjs
//     open FSharp.Data.Adaptive

//     module V1 =
//         type Thing = {
//             name  : string cval
//         }

//         type Model = {
//             foo : int cval
//             bar : string cval
//             things : Thing clist
//         }

//         module Thing =
//             let decode : Decoder<Element, Thing> = decode {
//                 let! model = Decode.fromMap <| doc.getMap "model"
//             }

//         let decode : Decoder<Element,Model> = decode {
//             let! bar = Decode.root?bar |> Decode.required Decode.string
//             let! things = Decode.root?things |> Decode.required (Decode.array Thing.decode)
//             return {
//                 foo = cval 1
//                 bar = bar
//                 things = things
//             }
//         }
    
// module ExampleDecoding2 =
//     open FSharp.Data.Adaptive
    
//     let bloop (map : Yjs.Y.Map<obj>) : cmap<string, obj> =
//         ()

//     let bloop2 (map : cmap<string, obj>) =
//         // each time the map changes, emit a new T
//         // we could just break this into map -> T
//         // then invoking that on each callback..
//         map.AddCallback(fun init delta -> 
//             let idk = delta |> HashMapDelta.toArray |> Array.head
//             ())
// module Decode =
//     open Yjs
//     open FSharp.Data.Adaptive

//     type Decoder<'a> = Y.AbstractType -> 'a

//     let string (value : Y.AbstractType) =
//         match value with
//         | Codec.Value value ->
//             value.ToString ()

//     type Getter (map : Y.Map<obj>) =
//         member _.Field key decoder =
//             match map.get key with
//             | Some value -> decoder (value :?> Y.AbstractType) // but also just values
//             | None -> invalidOp "TODO"

//     let object (builder : Getter -> 'a) (obj : Y.AbstractType) =
//         match obj with
//         | Codec.Map map ->
//             let getter = Getter map
//             builder getter

// module Observe =
//     open Yjs

//     type Observer<'a> = ('a -> unit) -> Y.AbstractType -> unit //idisposable

//     let ofDecoder (decoder : Decode.Decoder<'a>) : Observer<'a> =
//         fun update obj -> obj.observe (fun _ _ -> update <| decoder obj)

//     let string = ofDecoder Decode.string
//     // let object = ofDecoder << Decode.object

//     type OGetter (update, map : Y.Map<obj>) =
//         member _.Field key (observer : Observer<'a>) =
//             match map.get key with
//             | Some value ->
//                 observer update (value :?> Y.AbstractType)
//             | None -> invalidOp "TODO"
//     let object (builder : OGetter -> 'a) : Observer<'a> =
//         fun update obj ->
//             match obj with
//             | Codec.Map map ->
//                 map.observe (fun _ _ ->
//                     let getter = OGetter (update, map)
//                     update <| builder getter
//                 )


// Decode should go from yval -> model
// module ExampleDecode =
//     open Yjs
//     let decode (init : Sample.Model) =

//         // root.observe (fun _ _ -> update <| fun get -> {
//         //     bar = get.Field "bar" Decode.string
//         //     foo = ""
//         //     things = []
//         // })
//         Observe.object <| fun get -> {
//             init with
//                 bar = get.Field "bar" Observe.string
//         }

open FSharp.Data.Adaptive


[<RequireQualifiedAccess>]
type AKind =
    | Constant
    | Value
    | List
    | Map

[<RequireQualifiedAccess>]
type AElement =
    | Constant of obj
    | Value of obj aval
    | List of AElement alist
    | Map of amap<string, AElement>
    with 
    member this.toKind () =
        match this with
        | Constant _ -> AKind.Constant
        | Value _ -> AKind.Value
        | List _ -> AKind.List
        | Map _ -> AKind.Map



module AEncode =
    open FSharp.Data.Adaptive

    // type Encoding =
    //     | Value of obj aval

    // want to go to an amap because the binding to yjs can then use deltas instead of replace-all


    let object (props : (string * AElement) list) =
        let map = cmap ()
        for (key, el) in props do
            // // TODO: Unwrap avals.
            // // I'm not sure if we can represent higher order avals in Yjs so we'll need to turn a amap<_,aval<'a>> into an amap<_,'a>.
            // // Needs to recurse (in case it's an aval<aval<'a>>).
            // let el =
            //     match el with
            //     | Element.Value v ->
            //         let _ = v.AddWeakCallback(fun v -> ignore <| map.Add (key, Element.Constant v))
            //         Element.Constant v.Current
            //     | v -> v

            ignore <| map.Add (key, el) 
            
        AElement.Map map
            
    let constant a =
        a |> box |> AElement.Constant

    let value a =
        a |> AVal.map box |> AElement.Value

    let list f a =
        a |> AList.map f |> AElement.List

    let map f a =
        a |> AMap.map f |> AElement.Map

module ADecode =
    open FSharp.Data.Adaptive

    type PathSegment = 
        | ObjectKey   of string
        | ArrayIndex  of int
        override x.ToString () =
            match x with
            | ObjectKey  k -> k
            | ArrayIndex i -> sprintf "[%i]" i

    type Path = PathSegment list

    type Error =
        | NotConvertible of {|
            Path : Path
            Kind: {| Actual : AKind; Expected : AKind list |}
            Type: {| Actual : System.Type; Expected : System.Type list |} option
            |}
        | MissingProperty of {| Path : Path |}

    /// the outer aval represents the encoding itself changing
    /// which could happen if another client upgrades and publishes its changes

    // this would mean the decoding would have to return an aval.
    // what could/would the application do with it? get the intial value and then complain on subsequent changes


    type Decoded<'Result> = Result<'Result, Error> aval

    module Decoded =
        let inline ofResult c : Decoded<'a> = AVal.constant c
        let inline ok c : Decoded<'a> = ofResult <| Ok c
        let inline error e : Decoded<'a> = ofResult <| Error e

        let inline map (f : 'a -> 'b) (a : Decoded<'a>) :  Decoded<'b> =
            AVal.map (Result.map f) a

        let inline bind (f : 'a -> Decoded<'b>) (a : Decoded<'a>) : Decoded<'b> =
            AVal.bind (function Ok v -> f v | Error e -> error e) a

        let traverse (f : 'a -> Decoded<'b>) (source : 'a alist) : Decoded<'b alist> =

            let folder (state : aval<Result<'b alist, Error>>) (next : 'a) = adaptive {
                let! state = state
                let! next = f next
                match state, next with
                | Ok state, Ok next ->
                    return Ok <| AList.append state (AList.single next) 
            }

            let zzz = AList.fold folder (AVal.constant <| Ok AList.empty) source // <----
            let zzz = AVal.bind id zzz
            zzz

    // Something like Reader<Aval<Result<T>> sandwich
    type Decoder<'Element, 'Result> = Path * 'Element -> Decoded<'Result>
    type Decoder<'Result> = Decoder<AElement, 'Result>


    module Decoder = 
        /// Lift a Result<'a, Failure> to a Decoder<'a>.
        let inline ofResult (result : Result<'Result, Error>) : Decoder<_,'Result> =
            fun _ -> Decoded.ofResult result

        /// Creates a Decoder<'a> from an 'a.
        let inline ok c : Decoder<_,_> =
            fun _ -> Decoded.ok c

        /// Creates a Decoder<_> from an error.
        let inline error e : Decoder<_,_> =
            fun _ -> Decoded.error e

        let map (f : 'a -> 'b) (a : Decoder<'a>) : Decoder<'b> =
            a >> Decoded.map f

        let bind (f : 'a -> Decoder<_,'b>) (a : Decoder<_,'a>) : Decoder<_,'b> =
            fun dep -> a dep |> Decoded.bind (fun x -> f x dep)

        let private value (f : 'a -> Result<'b, Error>) : Decoder<_, 'a> -> Decoder<_, 'b> =
            bind (f >> ofResult)

        let optional f : Decoder<_> -> Decoder<_> =
            value <| fun (path, element) ->
                match element with
                | Some el -> f (path, el) |> Result.map Some
                | None    -> Ok None

        let required f : Decoder<_> -> Decoder<_> =
            value <| fun (path, element) ->
                match element with
                | Some el -> f (path, el)
                | None    -> Error <| MissingProperty {| Path = path |}

    // let map' (f : Decoder<_,_>) : Decoder<_> -> Decoder<_> =
    //     value <| fun (path, element) ->
    //         match element with
    //         | AElement.Map el ->
    //             f (path, el)
    //         | el -> Error <| NotConvertible {|
    //             Path = path
    //             Kind = {| 
    //                 Actual = el.toKind ()
    //                 Expected = [ AKind.Map ] |}
    //             Type = None
    //         |}

        
        let constant type' : Decoder<_,_> = fun (path, el) ->
            match el with
            | AElement.Constant v when v.GetType () = type'->
                Decoded.ok v
            | AElement.Constant v ->
                Decoded.error <| NotConvertible {|
                Path = path
                Kind = {|
                    Actual = el.toKind ()
                    Expected = [ AKind.Constant ] |}
                Type = Some {|
                    Actual = v.GetType()
                    Expected = [ type' ]
                |} |}
            | el ->
                Decoded.error <| NotConvertible {|
                Path = path
                Kind = {|
                    Actual = el.toKind ()
                    Expected = [ AKind.Constant ] |}
                Type = None |}

        [<RequiresExplicitTypeArguments>]
        let inline constant'<'a> : Decoder<'a> =
            constant (typeof<'a>)
            |> map (fun r -> r :?> 'a)

        let value type' : Decoder<_,_> = fun (path, el) ->
            match el with
            | AElement.Value v when v.ContentType = type' ->
                Decoded.ok v
            | AElement.Value v ->
                Decoded.error <| NotConvertible {|
                Path = path
                Kind = {|
                    Actual = el.toKind ()
                    Expected = [ AKind.Constant ] |}
                Type = Some {|
                    Actual = v.ContentType
                    Expected = [ type' ]
                |} |}
            | el ->
                Decoded.error <| NotConvertible {|
                Path = path
                Kind = {|
                    Actual = el.toKind ()
                    Expected = [ AKind.Constant ] |}
                Type = None |}

        let list f : Decoder<_,_> = fun (path, el) ->
            match el with
            | AElement.List v ->
                v |> Decoded.traverse f /// just did this, idk what next

        let key key (f : Decoder<_,_>) : Decoder<_, _> = fun (path, el : amap<string, AElement>) -> adaptive {
            let path = ObjectKey key :: path

            let! value = el |> AMap.tryFind key

            match value with
            | None ->
                return Error <| MissingProperty {| Path = path |}
            | Some value ->
                return! f (path, value)
        }

    // let key k (f : Decoder<_,_>) (path, el : amap<string, AElement>) =


    //         let path = ObjectKey k :: path

    //         let value =
    //             AMap.tryFind k el
    //             |> AVal.map (function
    //             | Some v -> Ok v
    //             | None -> Error <| MissingProperty {| Path = path |})
    //             // |> AVal.bind (function
    //             // | Ok (AElement.Value x) -> x
    //             // | Ok (AElement.Constant x) -> AVal.constant x)
    //             |> AVal.map (Result.bind (fun x -> f (path, x)))

    //         AVal.force value
    //         |> Result.map (fun _ ->
    //             // Any subsequent errors will be runtime errors.
    //             value
    //             |> AVal.map (function
    //             | Ok v -> v
    //             // TODO: Print runtime error nicely
    //             | Error e -> invalidOp $"%A{e}"))

    type DecodeBuilder() =
        member _.Return x = Decoder.ok x
        member _.Bind (m, f) = Decoder.bind f m
        member _.ReturnFrom m = m
        member _.Zero () = Decoder.ok ()
        member _.Run f = f

    let decode = DecodeBuilder()

    let (?) decode path = Decoder.key path decode
         

module ExampleEncode =
    open FSharp.Data.Adaptive

    module Things =
        let encode (athing : Sample.AdaptiveThing) =
            AEncode.object [
                "name", AEncode.value athing.name
            ]

    let encode (amodel : Sample.AdaptiveModel) =
        AEncode.object [
            "foo", AEncode.value amodel.foo
            "bar", AEncode.value amodel.bar
            "things", AEncode.list Things.encode amodel.things
        ]


    let someFirstStep (amodel : Sample.AdaptiveModel) : cmap<string, obj> =
        ()

module ExampleDecode =
    open ADecode

    let decode = decode {
        let! asdf = Decoder.key "name" Decoder.constant'<string>
        return asdf
    }

type Navigable<'model, 'msg> =
    | Set of 'model
    | UserMsg of 'msg

module Program =
    open Elmish 


    let toStateful (program : Program<'a,Sample.Model,'msg,'view>) =
        let mutable amodel : Sample.AdaptiveModel = Unchecked.defaultof<_>

        let update userUpdate msg model =
            match msg with
            | Set m ->
                do amodel.Update m
                m, Cmd.none
            | UserMsg userMsg ->
                let m, c = userUpdate userMsg model
                let c = c |> Cmd.map UserMsg
                do amodel.Update m
                m, c

        let subs userSubscribe model =
            Cmd.batch [
                userSubscribe model |> Cmd.map UserMsg 
            ]

        let init userInit () =
            let m, c = userInit ()
            do amodel <- Sample.AdaptiveModel.Create m
            let c = c |> Cmd.map UserMsg
            m, c

        let setState userSetState model dispatch =
            userSetState model (UserMsg >> dispatch)

        let view userView model dispatch =
            userView model (UserMsg >> dispatch)
        
        program
        |> Program.map init update view setState subs