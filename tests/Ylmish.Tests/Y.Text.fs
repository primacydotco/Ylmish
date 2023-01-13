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
    testList "ofAdaptive" [
        test "ofAdaptive (initialisation)" {
            let atext = clist [ 'a'; 'b'; 'd' ]
            let ydoc = Y.Doc.Create ()
            let ytext = Y.Text.ofAdaptive atext
            let _ = ydoc.getMap("container").set("test", ytext)

            Expect.equal (System.String.Concat atext) "abd" "atext doesn't equal expected value"
            Expect.equal (ytext.toString()) "abd" "ytext doesn't equal expected value"
        }

        test "ofAdaptive, atext_InsertAt(): given \"abd\", insert 'c' to give \"abcd\"" {
            let atext = clist [ 'a'; 'b'; 'd' ]
            let ydoc = Y.Doc.Create ()
            let ytext = Y.Text.ofAdaptive atext
            let _ = ydoc.getMap("container").set("test", ytext)

            let _ = transact (fun () -> atext.InsertAt (2, 'c'))

            Expect.equal (System.String.Concat atext) "abcd" "atext doesn't equal expected value"
            Expect.equal (ytext.toString()) "abcd" "ytext doesn't equal expected value"
        }

        test "ofAdaptive, ytext_insert(): given \"abd\", insert 'c' to give \"abcd\"" {
            let atext = clist [ 'a'; 'b'; 'd' ]
            let ydoc = Y.Doc.Create ()
            let ytext = Y.Text.ofAdaptive atext
            let _ = ydoc.getMap("container").set("test", ytext)

            let _ = ytext.insert(2, "c")

            Expect.equal (System.String.Concat atext) "abcd" "atext doesn't equal expected value"
            Expect.equal (ytext.toString()) "abcd" "ytext doesn't equal expected value"
        }
    ]

    testList "toAdaptive" [
        test "toAdaptive, (initialisation)" {
            let ydoc = Y.Doc.Create ()
            let ytext = ydoc.getText "test"
            let _ = ytext.insert(0, "abd")
            let atext = Y.Text.toAdaptive ytext

            Expect.equal (System.String.Concat atext) "abd" "atext doesn't equal expected value"
            Expect.equal (ytext.toString()) "abd" "ytext doesn't equal expected value"
        }

        test "toAdaptive, ytext_insert()" {
            let ydoc = Y.Doc.Create ()
            let ytext = ydoc.getText "test"
            let atext = Y.Text.toAdaptive ytext

            let _ = ytext.insert(0, "abd")
            let _ = ytext.insert(2, "c")

            Expect.equal (System.String.Concat atext) "abcd" "atext doesn't equal expected value"
            Expect.equal (ytext.toString()) "abcd" "ytext doesn't equal expected value"
        }
    ]
]
