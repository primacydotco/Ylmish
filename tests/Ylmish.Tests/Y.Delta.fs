module Ylmish.Y.Delta

open FSharp.Data.Adaptive
open Yjs

open Ylmish.Adaptive
open Ylmish

#if FABLE_COMPILER
open Fable.Mocha
#else
open Expecto
#endif

open Fable.Core.JS // console.log

let private toIndexListDelta list =
    let delta = 
        list
        |> List.map (fun (i, o) -> Index.at (i + 1), o)
        |> IndexListDelta.ofList
    let placeholders =
        if list.IsEmpty
        then IndexList.empty 
        else 
            list
            |> List.maxBy (fun (i, _) -> i)
            |> fst
            |> List.unfold (fun i ->
                if i < 0 then None else Some (('_'), i - 1))
            |> IndexList.ofList
    placeholders, delta

// Test cases from https://docs.yjs.dev/api/delta-format
// https://quilljs.com/docs/delta/#playground
let tests = testList "Y.Delta" [
     test "toAdaptive ins 'abc'" {
         let input = ResizeArray [
             Y.Delta.Insert "abc"
         ]
         let delta = Y.Text.Impl.toAdaptive input
         Expect.equal (IndexListDelta.toList delta) [
             (Index.at 0, ElementOperation<char>.Set 'a')
             (Index.at 1, ElementOperation<char>.Set 'b')
             (Index.at 2, ElementOperation<char>.Set 'c')
         ] ""
     }
     test "toAdaptive ret 0, ins 'abc'" {
         let input = ResizeArray [
             Y.Delta.Retain 0
             Y.Delta.Insert "abc"
         ]
         let delta = Y.Text.Impl.toAdaptive input
         Expect.equal (IndexListDelta.toList delta) [
             (Index.at 0, ElementOperation.Set 'a')
             (Index.at 1, ElementOperation.Set 'b')
             (Index.at 2, ElementOperation.Set 'c')
         ] ""
     }
     test "toAdaptive ret 2, ins 'abc'" {
         let input = ResizeArray [
             Y.Delta.Retain 2
             Y.Delta.Insert "abc"
         ]
         let delta = Y.Text.Impl.toAdaptive input
         Expect.equal (IndexListDelta.toList delta) [
             (Index.at 2, ElementOperation.Set 'a')
             (Index.at 3, ElementOperation.Set 'b')
             (Index.at 4, ElementOperation.Set 'c')
         ] ""
     }
     test "toAdaptive ret 2, del 2" {
         let input = ResizeArray [
             Y.Delta.Retain 2
             Y.Delta.Delete 2
         ]
         let delta = Y.Text.Impl.toAdaptive input
         Expect.equal (IndexListDelta.toList delta) [
             (Index.at 2, ElementOperation.Remove)
             (Index.at 3, ElementOperation.Remove)
         ] ""
     }
     test "toAdaptive del 0" {
         let input = ResizeArray [
             Y.Delta.Delete 0
         ]
         let delta = Y.Text.Impl.toAdaptive input
         Expect.equal (IndexListDelta.toList delta) [ ] ""
     }
     test "ofAdaptive ins 'abc'" {
         let input = [
             0, ElementOperation.Set 'a'
             1, ElementOperation.Set 'b'
             2, ElementOperation.Set 'c'
         ]
         let list, delta = toIndexListDelta input
         let delta = Y.Text.Impl.ofAdaptive list delta
         Expect.equal (List.ofSeq delta) [
             Y.Delta.Insert "abc"
         ] ""
     }
     test "ofAdaptive ret 2, ins 'abc', ret 2, ins 'efg'" {
         let input = [
             // 0
             // 1
             2, ElementOperation.Set 'a'
             3, ElementOperation.Set 'b'
             4, ElementOperation.Set 'c'
             // 5
             // 6
             7, ElementOperation.Set 'e'
             8, ElementOperation.Set 'f'
             9, ElementOperation.Set 'g'
         ]
         let list, delta = toIndexListDelta input
         let asdf, y = delta |> IndexListDelta.toList |> List.head
         console.log ($"HEY %A{(asdf,y)}")
         console.log ($"HEY %A{list.TryGetPosition asdf}")
         let delta = Y.Text.Impl.ofAdaptive list delta
         Expect.equal (List.ofSeq delta) [
             Y.Delta.Retain 2
             Y.Delta.Insert ("abc")
             Y.Delta.Retain 2
             Y.Delta.Insert ("efg")
         ] ""
     }
     test "ofAdaptive ret 2, del 2" {
         let input = [
             2, ElementOperation.Remove
             3, ElementOperation.Remove
         ]
         let list, delta = toIndexListDelta input
         let delta = Y.Text.Impl.ofAdaptive list delta
         Expect.equal (List.ofSeq delta) [
             Y.Delta.Retain 2
             Y.Delta.Delete 2
         ] ""
     }
     test "ofAdaptive []" {
         let input = []
         let list, delta = toIndexListDelta input
         let delta = Y.Text.Impl.ofAdaptive list delta
         Expect.equal (List.ofSeq delta) [] ""
     }
     test "ofAdaptive ins 'abc', ret 2, del 2" {
         let input = [
             0, ElementOperation.Set 'a'
             1, ElementOperation.Set 'b'
             2, ElementOperation.Set 'c'
             5, ElementOperation.Remove
             6, ElementOperation.Remove
         ]
         let list, delta = toIndexListDelta input
         let delta = Y.Text.Impl.ofAdaptive list delta
         Expect.equal (List.ofSeq delta) [
             Y.Delta.Insert ("abc")
             Y.Delta.Retain 2
             Y.Delta.Delete 2
         ] ""
     }
 ]