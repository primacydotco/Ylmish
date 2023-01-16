module Ylmish.Adaptive.Codec

open FSharp.Data.Adaptive
open Hedgehog

open Ylmish.Adaptive.Codec

#if FABLE_COMPILER
open Fable.Mocha
#else
open Expecto
#endif
// Generated by [Adaptify](https://github.com/krauthaufen/Adaptify)
module private Example =

    // [<ModelType>]
    type Thing =
        {
            name  : string
            value : int
        }

    module Thing =
        let gen = gen {
            let! name = Gen.string (Range.linear 0 255) Gen.alphaNum
            let! value = Gen.int32 (Range.linearBounded ())
            return {
                name = name
                value = value
            }
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
        let __adaptive = FSharp.Data.Adaptive.AVal.custom((fun (_ : FSharp.Data.Adaptive.AdaptiveToken) -> __value))
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
        let __adaptive = FSharp.Data.Adaptive.AVal.custom((fun (_ : FSharp.Data.Adaptive.AdaptiveToken) -> __value))
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

    // Written by hand.
    module Codec =
        module Things =
            let encode (athing : AdaptiveThing) = Encode.object [
                "name", athing.name |> Encode.value id
                "value", athing.value |> Encode.value string
            ]

            let decode : Decoder<_,Thing> = Decode.object {
                let! name = Decode.object.required "name" Decode.value
                let! value = Decode.object.required "value" Decode.Int32.tryParse
                return {
                    name = name
                    value = value
                }
            }

        let encode (amodel : AdaptiveModel) = Encode.object [
            "bar",  amodel.bar |> Encode.value id
            "foo", amodel.foo |> Encode.value string
            "things", amodel.things |> Encode.list Things.encode
        ]

        let decode : Decoder<_,_> = Decode.object {
            let! things = Decode.object.required "things" (Decode.list.required Things.decode)
            let! foo = Decode.object.required "foo" Decode.Int32.tryParse
            let! bar = Decode.object.required "bar" Decode.value
            return {
                things = things
                foo = foo
                bar = bar
            }
        }


module private Decode =
    let inline force decoder encoded =
        Decode.run decoder encoded
        |> Decoded.mapError Error.printAll
        |> AVal.force
        |> function
        | Ok value -> value
        | Error e -> invalidOp e

let tests = testList "Ylmish.Adaptive.Codec" [
    // Currently failing:
    //
    // https://github.com/fable-compiler/Fable/issues/3328
    //
    // Tracking issue:
    //
    // https://github.com/primacydotco/Ylmish/issues/10
    //
    test "roundtrips" {
       let example : Example.Thing = {
           name = "Example Thing"
           value = 42
       }
       let actual =
           example
           |> Example.AdaptiveThing
           |> Example.Codec.Things.encode
           |> Decode.force Example.Codec.Things.decode

       Expect.equal actual example ""
    }

    testCase "basic updates work" <| fun _ -> Property.check <| property {
        let! model = Example.Thing.gen |> Gen.map Example.AdaptiveThing
        let! updates = Example.Thing.gen |> Gen.list (Range.linear 0 5)
        for update in updates do
            model.Update update
            let _ = AVal.force model.Current
            ()
    }
    testCase "Decoded_traversei" <| fun _ -> Property.check <| property {
        let! items =
            Example.Thing.gen
            |> Gen.list (Range.linear 0 10)
            |> Gen.map AList.ofList

        let actual =
            items
            |> Decoded.traversei (fun _ x -> AVal.constant <| Ok x.name)
            |> AVal.force
            |> function
            | Ok r -> r
            | Error e -> invalidOp $"%A{e}"

        let expected =
            items
            |> AList.map (fun i -> i.name)
            |> AList.force

        Expect.equal (actual) (expected) ""
    }

    // Currently failing:
    //
    // https://github.com/fsprojects/FSharp.Data.Adaptive/issues/108
    //
    // Tracking issue:
    //
    // https://github.com/fsprojects/FSharp.Data.Adaptive/issues/108
    //
    //testCase "roundtrips updates" <| fun _ -> Property.check <| property {
    //    let! model = Example.Thing.gen |> Gen.map Example.AdaptiveThing
    //    let model' =
    //        model
    //        |> Example.Codec.Things.encode
    //        |> Decode.run Example.Codec.Things.decode
    //        |> Decoded.mapError Error.printAll
    //        |> AVal.map (function
    //        | Ok r -> r
    //        | Error e -> invalidOp e)

    //    let! updates = Example.Thing.gen |> Gen.list (Range.linear 0 100)

    //    for update in updates do
    //        model.Update update

    //        let value1 = AVal.force model'
    //        let value2 = AVal.force model.Current

    //        Expect.equal value1 value2 ""
    //}
]
