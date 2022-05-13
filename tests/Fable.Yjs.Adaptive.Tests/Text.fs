module Text

open Yjs.Adaptive

open Yjs
open FSharp.Data.Adaptive

open Fable.Mocha

let tests =
    testList "Text tests" [
        test "ytext to cval" {
            let ydoc = Y.Doc.Create ()
            let ytext  = ydoc.getText "x"

            let cval' = Y.Text.toAdaptive ytext

            ytext.insert (0, "cat")

            Expect.equal (AVal.force cval') "cat" "equal"
        }
    ]