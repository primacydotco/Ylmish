module Yjs.Adaptive.Y.Text

open Fable.Core.JS
open Fable.Core.JsInterop
open Fable.Core.Testing
open Fable.Mocha
open FSharp.Data.Adaptive
open Yjs

open Yjs.Adaptive


let tests = testList "Y.Text" [
    // test "ofAdaptive, atext.InsertAt()" {
    //     let atext = clist [ 'a'; 'b'; 'd' ]
    //     let ydoc = Y.Doc.Create ()
    //     let ytext = Y.Text.ofAdaptive atext
    //     let _ = ydoc.getMap("container").set("test", ytext)

    //     let _ = transact (fun () -> atext.InsertAt (2, 'c'))

    //     Expect.equal (System.String.Concat atext) "abcd" "equal"
    //     Expect.equal (ytext.toString()) "abcd" "equal"
    // }

    test "ofAdaptive, ytext.insert()" {
        let atext = clist [ 'a'; 'b'; 'd' ]
        let ydoc = Y.Doc.Create ()
        let ytext = Y.Text.ofAdaptive atext
        let _ = ydoc.getMap("container").set("test", ytext)

        let _ = ytext.insert(2, "c")

        Expect.equal (System.String.Concat atext) "abcd" "equal"
        // Expect.equal (ytext.toString()) "abcd" "equal"
    }
]