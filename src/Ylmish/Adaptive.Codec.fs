module Ylmish.Adaptive.Codec

open FSharp.Data.Adaptive

type Validation<'Ok, 'Error> = Result<'Ok, 'Error list>

// From [FsToolkit.ErrorHandling](https://github.com/demystifyfp/FsToolkit.ErrorHandling/blob/master/src/FsToolkit.ErrorHandling/Validation.fs)
// MIT License
// 
// Copyright (c) 2018 DemystifyFP
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
[<RequireQualifiedAccess>]
module internal Validation =

    let inline ok (value: 'ok) : Validation<'ok, 'error> =
        Ok value

    let inline error (error: 'error) : Validation<'ok, 'error> =
        Error [ error ]

    let inline errors (errors: 'error list) : Validation<'ok, 'error> =
        Error errors

    let inline ofResult (result: Result<'ok, 'error>) : Validation<'ok, 'error> =
        Result.mapError List.singleton result

    let inline apply
        (applier: Validation<'okInput -> 'okOutput, 'error>)
        (input: Validation<'okInput, 'error>)
        : Validation<'okOutput, 'error> =
        match applier, input with
        | Ok f, Ok x ->
            Ok (f x)
        | Error errs, Ok _
        | Ok _, Error errs ->
            Error errs
        | Error errs1, Error errs2 ->
            Error (errs1 @ errs2)

    let inline map
        ([<InlineIfLambda>] mapper: 'okInput -> 'okOutput)
        (input: Validation<'okInput, 'error>)
        : Validation<'okOutput, 'error> =
        Result.map mapper input

    let inline zip
        (left: Validation<'left, 'error>)
        (right: Validation<'right, 'error>)
        : Validation<'left * 'right, 'error> =
        match left, right with
        | Ok x1res, Ok x2res ->
            Ok (x1res, x2res)
        | Error e, Ok _
        | Ok _, Error e ->
            Error e
        | Error e1, Error e2 ->
            Error (e1 @ e2)

[<RequireQualifiedAccess>]
type Kind =
    | Constant
    | Value
    | List
    | Map

[<RequireQualifiedAccess>]
type Element =
    | Constant of obj
    | Value of obj aval
    | List of Element alist
    | Map of amap<string, Element>
    with 
    member this.toKind () =
        match this with
        | Constant _ -> Kind.Constant
        | Value _ -> Kind.Value
        | List _ -> Kind.List
        | Map _ -> Kind.Map

type PathSegment = 
    | ObjectKey   of string
    | ArrayIndex  of int

type Path = PathSegment list

module Path =
    open System.Text

    let toString (path : Path) =
        let sb = StringBuilder ()
        for p in path do
            match p with
            | ObjectKey k ->
                ignore <| sb.Append $".%s{k}"
            | ArrayIndex i ->
                ignore <| sb.Append $"[%i{i}]"
        $"${sb.ToString ()}"

type Error =
    private
    | UnexpectedKind of {|
            Path : Path
            Actual : Kind
            Expected : Kind list 
        |}
    | UnexpectedType of {|
            Path : Path
            Actual : System.Type
            Expected : System.Type list 
        |}
    | MissingProperty of {| Path : Path |}

module Error =
    let print (error : Error) =
        match error with
        | UnexpectedKind e -> $"\
            {Path.toString e.Path} is of kind %A{e.Actual} but expected one of %A{e.Expected}"
        | UnexpectedType e -> $"\
            {Path.toString e.Path} is of type %A{e.Actual} but expected one of %A{e.Expected}"
        | MissingProperty e -> $"\
            {Path.toString e.Path} does not exist but was expected."

    let printAll (errors : Error list) =
        let sb = System.Text.StringBuilder ()
        ignore <| sb.Append "Failed to decode, because:"
        ignore <| sb.AppendLine ()
        for error in errors do
            ignore <| sb.Append $"- %s{print error}"
            ignore <| sb.AppendLine ()
        sb.ToString ()

type Decoded<'Result> = Validation<'Result, Error> aval
type Decoder<'Element, 'Result> = Path * 'Element -> Decoded<'Result>
type Decoder<'Result> = Decoder<Element, 'Result>

module Encode =
    let object (props : (string * Element) list) =
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
            
        Element.Map map
            
    let constant a =
        a |> box |> Element.Constant

    let value a =
        a |> AVal.map box |> Element.Value

    let list f a =
        a |> AList.map f |> Element.List

    let map f a =
        a |> AMap.map f |> Element.Map

module Decoded =
    let inline ofValidation (c : Validation<'a, Error>) : Decoded<'a> = AVal.constant c
    let inline ok (c : 'a) : Decoded<'a> = ofValidation <| Validation.ok c
    let inline error (e : Error) : Decoded<'a> = ofValidation <| Validation.error e
    let inline errors (e : Error list) : Decoded<'a> = ofValidation <| Validation.errors e
    let inline value (a : Decoded<'a>) = AVal.force a

    let inline map (f : 'a -> 'b) (a : Decoded<'a>) :  Decoded<'b> =
        AVal.map (Validation.map f) a

    let inline mapError f =
        AVal.map (Result.mapError f)

    let inline bind (f : 'a -> Decoded<'b>) (a : Decoded<'a>) : Decoded<'b> =
        AVal.bind (function Ok v -> f v | Error e -> errors e) a

    let traversei (f : int -> 'a -> Decoded<'b>) (source : 'a alist) : Decoded<'b alist> =

        let folder (i : int, state : Decoded<'b alist>) (next : 'a) =
            let result = adaptive {
                let! state = state
                let! next = f i next
                match state, next with
                | Ok state, Ok next ->
                    return Validation.ok <| AList.append state (AList.single next)
                | Error e, Ok _
                | Ok _, Error e ->
                    return Validation.errors e
                | Error e1, Error e2 ->
                    return Validation.errors (e1 @ e2)             
            }
            i + 1, result

        let zzz = AList.fold folder (0, AVal.constant <| Ok AList.empty) source
        let zzz = zzz |> AVal.map snd |> AVal.bind id // surely I can avoid this?
        zzz

    let flatten (decoded : Decoded<'a aval>) : Decoded<'a> = adaptive {
        match! decoded with
        | Ok value ->
            let! value = value
            return Ok value
        | Error e ->
            return Error e
    }

module Decode = 
    /// Lift a Validation<'a, Failure> to a Decoder<'a>.
    let ofValidation (result : Validation<'Result, Error>) : Decoder<_,'Result> =
        fun _ -> Decoded.ofValidation result

    /// Creates a Decoder<'a> from an 'a.
    let ok c : Decoder<_,_> =
        fun _ -> Decoded.ok c

    /// Creates a Decoder<_> from an error.
    let error e : Decoder<_,_> =
        fun _ -> Decoded.error e

    let map (f : 'a -> 'b) (a : Decoder<'a>) : Decoder<'b> =
        a >> Decoded.map f

    let bind (f : 'a -> Decoder<_,'b>) (a : Decoder<_,'a>) : Decoder<_,'b> =
        fun dep -> a dep |> Decoded.bind (fun x -> f x dep)

    let private _value (f : 'a -> Validation<'b, Error>) : Decoder<_, 'a> -> Decoder<_, 'b> =
        bind (f >> ofValidation)

    let optional f : Decoder<_> -> Decoder<_> =
        _value <| fun (path, element) ->
            match element with
            | Some el -> f (path, el) |> Validation.map Some
            | None    -> Ok None

    let required f : Decoder<_> -> Decoder<_> =
        _value <| fun (path, element) ->
            match element with
            | Some el -> f (path, el)
            | None    -> Validation.error <| MissingProperty {| Path = path |}
    
    let constant type' : Decoder<_,_> = fun (path, el) ->
        match el with
        | Element.Constant v when v.GetType () = type'->
            Decoded.ok v
        | Element.Constant v ->
            Decoded.error <| UnexpectedType {|
                Path = path
                Actual = v.GetType()
                Expected = [ type' ]
            |}
        | el ->
            Decoded.error <| UnexpectedKind {|
                Path = path
                Actual = el.toKind ()
                Expected = [ Kind.Constant ]
            |}

    [<RequiresExplicitTypeArguments>]
    let inline constant'<'a> : Decoder<'a> =
        constant (typeof<'a>)
        |> map (fun r -> r :?> 'a)

    let value type' : Decoder<_> = fun (path, el) ->
        match el with
        // // TOQUERY: it's never going to have a valid contenttype, right?
        // | Element.Value v when v.ContentType = type' ->
        //     Decoded.ok v
        // | Element.Value v ->
        //     Decoded.error <| UnexpectedType {|
        //         Path = path
        //         Actual = v.ContentType
        //         Expected = [ type' ]
        //     |}
        | Element.Value v ->
            ignore type'
            Decoded.ok v
        | el ->
            Decoded.error <| UnexpectedKind {|
                Path = path
                Actual = el.toKind ()
                Expected = [ Kind.Value ]
            |}

    [<RequiresExplicitTypeArguments>]
    let inline value'<'a> : Decoder<'a aval> =
        value (typeof<'a>)
        |> map (fun r -> r :?> 'a aval)

    let list (f : Decoder<_>) : Decoder<_> = fun (path, el) ->
        let f i el = f (ArrayIndex i :: path, el)
        match el with
        | Element.List v ->
            v |> Decoded.traversei f
        | el ->
            Decoded.error <| UnexpectedKind {|
                Path = path
                Actual = el.toKind ()
                Expected = [ Kind.List ]
            |}

    let key key (f : Decoder<_>) : Decoder<_> = fun (path, el) ->
        match el with
        | Element.Map v -> adaptive {
                let path = ObjectKey key :: path

                let! value = v |> AMap.tryFind key

                match value with
                | None -> 
                    return Validation.error <| MissingProperty {| Path = path |}
                | Some value ->
                    return! f (path, value)
            }
        | el ->
            Decoded.error <| UnexpectedKind {|
                Path = path
                Actual = el.toKind ()
                Expected = [ Kind.Map ]
            |}

    let run (decoder : Decoder<'input, 'a>) (input : 'input) : Decoded<'a> =
        decoder ([], input)

    type ObjectBuilder () =
        member _.Return x = ok x
        member _.Bind (m, f) = bind f m
        member _.ReturnFrom m = m
        member _.Zero () = ok ()
        member _.Run f = f

        member _.key (k : string, decoder : Decoder<aval<'a>>) =
            key k decoder
            >> Decoded.flatten
            // here we flatten the outer aval (changes to the mapping) with the inner aval (changes to the values)
            // does this matter?

        member _.key (k : string, decoder : Decoder<alist<'a>>) =
            key k decoder
            >> Decoded.map AList.toAVal
            >> Decoded.flatten

    let object = ObjectBuilder ()

// let (?) decode path = Decoder.key path decode