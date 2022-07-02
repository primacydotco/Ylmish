module Yjs.Adaptive.Index

open Fable.Core.JS
open Fable.Core.JsInterop
open Fable.Core.Testing
open Fable.Mocha
open FSharp.Data.Adaptive
open Yjs

open Yjs.Adaptive

let tests = testList "Index" [
    test "generate Index.zero 0" {
        Expect.equal (Index.generate (fun _ j -> j) Index.zero 0) [ Index.zero ] ""
    }
    test "generate Index.zero 3" {
        Expect.equal (Index.generate (fun _ j -> j) Index.zero 3) [
            Index.zero
            Index.after (Index.zero)
            Index.after (Index.after Index.zero)
            Index.after (Index.after (Index.after Index.zero))
        ] ""
    }
    test "at 0" {
        Expect.equal (Index.at 0) (Index.zero) ""
    }
    test "at 3" {
        Expect.equal (Index.at 3) (
            Index.after (Index.after (Index.after Index.zero))
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
]