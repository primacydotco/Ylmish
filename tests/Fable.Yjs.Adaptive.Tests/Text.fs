module Text

open Yjs.Adaptive

open Yjs
open FSharp.Data.Adaptive

open Fable.Mocha

let tests =
    // Test cases from https://docs.yjs.dev/api/delta-format
    testList "Text tests" [
        test "ytext to cval" {
            let ydoc = Y.Doc.Create ()
            let ytext  = ydoc.getText "x"

            let cval' = Y.Text.toAdaptive ytext

            ytext.insert (0, "abcdefghij")
            ytext.delete (1, 4)

            // Expect.equal (System.String.Concat cval') "afghij" "equal"
            Expect.equal (ytext.ToString()) "afghij" "equal"
        }
    ]