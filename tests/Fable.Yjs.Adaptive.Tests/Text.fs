module Text

open Yjs.Adaptive

open Yjs
open FSharp.Data.Adaptive

open Fable.Mocha
open Fable.Core.JS
open Fable.Core.JsInterop

open Fable.Core.Testing

let tests =
    // Test cases from https://docs.yjs.dev/api/delta-format
    // https://quilljs.com/docs/delta/#playground
    testList "Text tests" [
        test "Index.generate Index.zero 0" {
            Expect.equal (Index.generate (fun _ j -> j) Index.zero 0) [ Index.zero ] ""
        }
        test "Index.generate Index.zero 3" {
            Expect.equal (Index.generate (fun _ j -> j) Index.zero 3) [
                Index.zero
                Index.after (Index.zero)
                Index.after (Index.after Index.zero)
                Index.after (Index.after (Index.after Index.zero))
            ] ""
        }
        test "Index.at 0" {
            Expect.equal (Index.at 0) (Index.zero) ""
        }
        test "Index.at 3" {
            Expect.equal (Index.at 3) (
                Index.after (Index.after (Index.after Index.zero))
            ) ""
        }
        test "Index.increment 0" {
            Expect.equal (Index.increment (Index.at 3) 0) (Index.at 3) ""
        }
        test "Index.increment 3" {
            Expect.equal (Index.increment Index.zero 3) (
                Index.after(Index.after(Index.after(Index.zero)))
            ) ""
        }
        test "ins 'abc'" {
            let input = ResizeArray [
                jsOptions<Y.Event.Delta> (fun o ->
                    o.insert <- Some (!^ "abc")
                )
            ]
            let delta = Y.TextEvent.toAdaptive input
            Expect.equal (IndexListDelta.toList delta) [
                (Index.at 0, ElementOperation.Set 'a')
                (Index.at 1, ElementOperation.Set 'b')
                (Index.at 2, ElementOperation.Set 'c')
            ] ""
        }
        test "ret 0, ins 'abc'" {
            let input = ResizeArray [
                jsOptions<Y.Event.Delta> (fun o ->
                    o.retain <- Some 0
                )
                jsOptions<Y.Event.Delta> (fun o ->
                    o.insert <- Some (!^ "abc")
                )
            ]
            let delta = Y.TextEvent.toAdaptive input
            Expect.equal (IndexListDelta.toList delta) [
                (Index.at 0, ElementOperation.Set 'a')
                (Index.at 1, ElementOperation.Set 'b')
                (Index.at 2, ElementOperation.Set 'c')
            ] ""
        }
        test "ret 2, ins 'abc'" {
            let input = ResizeArray [
                jsOptions<Y.Event.Delta> (fun o ->
                    o.retain <- Some 2
                )
                jsOptions<Y.Event.Delta> (fun o ->
                    o.insert <- Some (!^ "abc")
                )
            ]
            let delta = Y.TextEvent.toAdaptive input
            Expect.equal (IndexListDelta.toList delta) [
                (Index.at 2, ElementOperation.Set 'a')
                (Index.at 3, ElementOperation.Set 'b')
                (Index.at 4, ElementOperation.Set 'c')
            ] ""
        }
        test "ret 2, del 2" {
            let input = ResizeArray [
                jsOptions<Y.Event.Delta> (fun o ->
                    o.retain <- Some 2
                )
                jsOptions<Y.Event.Delta> (fun o ->
                    o.delete <- Some 2
                )
            ]
            let delta = Y.TextEvent.toAdaptive input
            Expect.equal (IndexListDelta.toList delta) [
                (Index.at 2, ElementOperation.Remove)
                (Index.at 3, ElementOperation.Remove)
            ] ""
        }
        test "del 0" {
            let input = ResizeArray [
                jsOptions<Y.Event.Delta> (fun o ->
                    o.delete <- Some 0
                )
            ]
            let delta = Y.TextEvent.toAdaptive input
            Expect.equal (IndexListDelta.toList delta) [ ] ""
        }
        // test "ytext to cval" {
        //     let ydoc = Y.Doc.Create ()
        //     let ytext  = ydoc.getText "x"

        //     let cval' = Y.Text.toAdaptive ytext

        //     ytext.insert (0, "abc")
        //     ytext.delete (1, 1)
        //     // ytext.insert (1, "c")


        //     Expect.equal (System.String.Concat cval') "ac" "equal"
        //     // Expect.equal (ytext.ToString()) "afghijcool" "equal"
        // }
    ]