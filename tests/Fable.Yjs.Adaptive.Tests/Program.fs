module Program

open Fable.Mocha

[<EntryPoint>]
let main args =
    Mocha.runTests <| testList "" [
        Text.tests
    ]