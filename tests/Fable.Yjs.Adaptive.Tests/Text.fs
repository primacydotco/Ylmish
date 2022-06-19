module Text

open Yjs.Adaptive

open Yjs
open FSharp.Data.Adaptive

open Fable.Mocha
open Fable.Core.JS
open Fable.Core.JsInterop

open Fable.Core.Testing

let private toIndexListDelta =
    List.map (fun (i, o) -> Index.at i, o)
    >> IndexListDelta.ofList

let private toIndexPositionLookup ls i =
    ls 
    |> List.map (fun (i, _) -> Index.at i, i)
    |> List.find (fun (x, _) -> x = i)
    |> snd

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
        test "toAdaptive ins 'abc'" {
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
        test "toAdaptive ret 0, ins 'abc'" {
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
        test "toAdaptive ret 2, ins 'abc'" {
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
        test "toAdaptive ret 2, del 2" {
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
        test "toAdaptive del 0" {
            let input = ResizeArray [
                jsOptions<Y.Event.Delta> (fun o ->
                    o.delete <- Some 0
                )
            ]
            let delta = Y.TextEvent.toAdaptive input
            Expect.equal (IndexListDelta.toList delta) [ ] ""
        }
        test "ofAdaptive ins 'abc'" {
            let input = [
                0, ElementOperation.Set 'a'
                1, ElementOperation.Set 'b'
                2, ElementOperation.Set 'c'
            ]
            let delta = Y.TextEvent.ofAdaptive (toIndexPositionLookup input) (toIndexListDelta input)
            Expect.equal (List.ofSeq delta) [
                Y.Event.Delta.Insert (!^ "abc")
            ] ""
        }
        test "ofAdaptive ret 2, ins 'abc', ret 2, ins 'efg'" {
            let input = [
                2, ElementOperation.Set 'a'
                3, ElementOperation.Set 'b'
                4, ElementOperation.Set 'c'
                7, ElementOperation.Set 'e'
                8, ElementOperation.Set 'f'
                9, ElementOperation.Set 'g'
            ]
            let delta = Y.TextEvent.ofAdaptive (toIndexPositionLookup input) (toIndexListDelta input)
            Expect.equal (List.ofSeq delta) [
                Y.Event.Delta.Retain 2
                Y.Event.Delta.Insert (!^ "abc")
                Y.Event.Delta.Retain 2
                Y.Event.Delta.Insert (!^ "efg")
            ] ""
        }
        test "ofAdaptive ret 2, del 2" {
            let input = [
                2, ElementOperation.Remove
                3, ElementOperation.Remove
            ]
            let delta = Y.TextEvent.ofAdaptive (toIndexPositionLookup input) (toIndexListDelta input)
            Expect.equal (List.ofSeq delta) [
                Y.Event.Delta.Retain 2
                Y.Event.Delta.Delete 2
            ] ""
        }
        test "ofAdaptive []" {
            let input = [
            ]
            let delta = Y.TextEvent.ofAdaptive (toIndexPositionLookup input) (toIndexListDelta input)
            Expect.equal (List.ofSeq delta) [
            ] ""
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