module Yjs.Adaptive.Program

open Fable.Mocha

[<EntryPoint>]
let main _ =
    Mocha.runTests <| testList "" [
        Index.tests
        Y.Delta.tests
    ]