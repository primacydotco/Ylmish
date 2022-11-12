module Yjs.Adaptive.Program

open Fable.Mocha

[<EntryPoint>]
let main _ =
    Mocha.runTests <| testList "" [
        FSharp.Data.Adaptive.Codec.tests
        Index.tests
        Y.Delta.tests
        Y.Text.tests
    ]