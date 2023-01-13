module Ylmish.Adaptive.Index

open FSharp.Data.Adaptive

open Ylmish.Adaptive

#if FABLE_COMPILER
open Fable.Mocha
#else
open Expecto
#endif

let tests = testList "Adaptive.Index" [
    test "generate Index_zero 0" {
        Expect.equal (Index.generate (fun _ j -> j) Index.zero 0) [ Index.zero ] ""
    }
    test "generate Index_zero 3" {
        Expect.equal (Index.generate (fun _ j -> j) Index.zero 3) [
            Index.zero
            Index.after (Index.zero)
            Index.after (Index.after Index.zero)
            Index.after (Index.after (Index.after Index.zero))
        ] ""
    }
    test "at 0" {
        Expect.equal (Index.at 0) (Index.after Index.zero) ""
    }
    test "at 3" {
        Expect.equal (Index.at 3) (
            Index.after (Index.after (Index.after (Index.after Index.zero)))
        ) ""
    }
    test "increment 0" {
        Expect.equal (Index.increment (Index.at 3) 0) (Index.at 3) ""
    }
    test "increment 3" {
        Expect.equal (Index.increment Index.zero 3) (
            Index.after(Index.after(Index.after(Index.zero)))
        ) ""
    }
    test "at 0 equals IndexList_tryFind" {
        let ls = IndexList.ofList [ 'a'; 'b'; 'c' ]
        Expect.equal (ls.[0]) 'a' ""
        Expect.equal (ls.TryFind 'a' |> Option.get) (Index.after Index.zero) ""
        Expect.equal (ls.TryFind 'a' |> Option.get) (Index.at 0) ""
    }
]