module Ylmish.Y.Text

open FSharp.Data.Adaptive
open Yjs

open Ylmish

#if FABLE_COMPILER
open Fable.Mocha
#else
open Expecto
#endif

let tests = testList "Y.Text" [
    test "ofAdaptive, atext_InsertAt()" {
        let atext = clist [ 'a'; 'b'; 'd' ]
        let ydoc = Y.Doc.Create ()
        let ytext = Y.Text.ofAdaptive atext
        let _ = ydoc.getMap("container").set("test", ytext)

        let _ = transact (fun () -> atext.InsertAt (2, 'c'))

        Expect.equal (System.String.Concat atext) "abcd" "equal"
        Expect.equal (ytext.toString()) "abcd" "equal"
    }

    test "ofAdaptive, ytext_insert()" {
        let atext = clist [ 'a'; 'b'; 'd' ]
        let ydoc = Y.Doc.Create ()
        let ytext = Y.Text.ofAdaptive atext
        let _ = ydoc.getMap("container").set("test", ytext)

        let _ = ytext.insert(2, "c")

        Expect.equal (System.String.Concat atext) "abcd" "equal"
        // Expect.equal (ytext.toString()) "abcd" "equal"
    }

    test "toAdaptive, ytext_insert()" {
        let ydoc = Y.Doc.Create ()
        let ytext = ydoc.getText "test"
        let _ = ytext.insert(0, "abd")
        let atext = Y.Text.toAdaptive ytext

        let _ = ytext.insert(2, "c")

        Expect.equal (System.String.Concat atext) "abcd" "equal"

    }
]