open Ylmish.Adaptive

let tests = [
   Codec.tests
]

#if FABLE_COMPILER
open Fable.Mocha

let all = testList "" tests

[<EntryPoint>]
let main args =
    Mocha.runTests all

#else
open Expecto

[<Tests>]
let all = testList "" tests

[<EntryPoint>]
let main args =
    runTestsWithArgs defaultConfig args all

#endif
