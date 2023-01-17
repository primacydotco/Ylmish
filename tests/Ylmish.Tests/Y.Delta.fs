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
open System

let private getFirstNIndicesUsingIndexAt n =
    [ for i in 0..n - 1 do Index.at i ]

let private getFirstNIndicesUsingInit n =
    let chars = Array.init n (fun i -> char ((int 'A') + i))
    let list = clist(chars)

    [ for i in 0..n - 1 do list.TryGetIndex(i).Value ]

let private getFirstNIndicesUsingInsertAt n =
    let list = clist()
    [ for i in 0..n - 1 do list.InsertAt(i, char ((int 'A') + i)) ]

/// Convenience function to convert (index: int, op) pairs into an IndexList of placeholders and
/// an IndexListDelta<char>.
let private toIndexListDelta list =
    let placeholders: IndexList<char> =
        if List.isEmpty list then
            IndexList.empty
        else
            let maxIndex =
                list
                |> List.maxBy fst
                |> fst

            maxIndex
            |> List.unfold (fun i ->
                if i < 0 then
                  None
                else
                  Some ((string (maxIndex - i))[0], i - 1)
            )
            |> IndexList.ofList

    let delta: IndexListDelta<char> =
        list
        |> List.map (fun (p, op) -> (placeholders.TryGetIndex p).Value, op)
        |> IndexListDelta.ofList

    placeholders, delta

// Test cases from https://docs.yjs.dev/api/delta-format
// https://quilljs.com/docs/delta/#playground
let tests = testList "Y.Delta" [
    testList "Index.at" [
        test "Index.at and clist initialisation return the same indices" {
            let n = 5
            let indexAt = getFirstNIndicesUsingIndexAt n
            let init = getFirstNIndicesUsingInit n
            Expect.equal indexAt init "Index.at returned different indices to clist initialisation"
        }

        test "Index.at and clist.InsertAt return the same indices" {
            let n = 5
            let indexAt = getFirstNIndicesUsingIndexAt n
            let insertAt = getFirstNIndicesUsingInsertAt n
            Expect.equal indexAt insertAt "Index.at returned different indices to clist.InsertAt"
        }
    ]

    testList "applyYDelta" [
        test "applyYDelta given empty clist, ins \"abc\" should give \"abc\"" {
            let input = ResizeArray [
                Y.Delta.Insert "abc"
            ]

            let clist = clist()
            Y.Text.Impl.applyYDelta input clist

            Expect.equal (System.String.Concat clist) "abc" $"{nameof clist} doesn't equal expected value"
        }
        test "applyYDelta given empty clist, given ret 0, ins \"abc\" should give \"abc\"" {
            let input = ResizeArray [
                Y.Delta.Retain 0
                Y.Delta.Insert "abc"
            ]

            let clist = clist()
            Y.Text.Impl.applyYDelta input clist

            Expect.equal (System.String.Concat clist) "abc" $"{nameof clist} doesn't equal expected value"
        }
        test "applyYDelta given \"XY\", ret 2, ins \"abc\" should give \"XYabc\"" {
            let input = ResizeArray [
                Y.Delta.Retain 2
                Y.Delta.Insert "abc"
            ]

            let clist = clist("XY")
            Y.Text.Impl.applyYDelta input clist

            Expect.equal (System.String.Concat clist) "XYabc" $"{nameof clist} doesn't equal expected value"
        }
        test "applyYDelta given \"XYZ\", ret 2, ins \"abc\" should give \"XYabcZ\"" {
            let input = ResizeArray [
                Y.Delta.Retain 2
                Y.Delta.Insert "abc"
            ]

            let clist = clist("XYZ")
            Y.Text.Impl.applyYDelta input clist

            Expect.equal (System.String.Concat clist) "XYabcZ" $"{nameof clist} doesn't equal expected value"
        }
        test "applyYDelta ret 2, del 2" {
            let input = ResizeArray [
                Y.Delta.Retain 2
                Y.Delta.Delete 2
            ]

            let clist = clist("abXYcd")
            Y.Text.Impl.applyYDelta input clist

            Expect.equal (System.String.Concat clist) "abcd" $"{nameof clist} doesn't equal expected value"
        }
        test "applyYDelta ret 2, del 2, then ret 3, insert 3" {
            let input = ResizeArray [
                Y.Delta.Retain 2
                Y.Delta.Delete 2
            ]

            let list = clist("abXYcd")
            Y.Text.Impl.applyYDelta input list

            Expect.equal (System.String.Concat list) "abcd" $"{nameof list} doesn't equal expected value"

            let input = ResizeArray [
                Y.Delta.Retain 3
                Y.Delta.Insert "123"
            ]

            Y.Text.Impl.applyYDelta input list

            Expect.equal (System.String.Concat list) "abc123d" $"{nameof clist} doesn't equal expected value"
        }
        test "applyYDelta del 0" {
            let input = ResizeArray [
                Y.Delta.Delete 0
            ]

            let clist = clist("abcd")
            Y.Text.Impl.applyYDelta input clist

            Expect.equal (System.String.Concat clist) "abcd" $"{nameof clist} doesn't equal expected value"
        }
    ]

    testList "applyAdaptiveDelta" [
        test "applyAdaptiveDelta ins 'abc'" {
            let input = [
                0, ElementOperation.Set 'a'
                1, ElementOperation.Set 'b'
                2, ElementOperation.Set 'c'
            ]
            let list, delta = toIndexListDelta input

            let ydoc = Y.Doc.Create ()
            let ytext = ydoc.getText "test"

            Y.Text.Impl.applyAdaptiveDelta list delta ytext

            Expect.equal (ytext.toString()) "abc" "ytext doesn't equal expected value"
        }
        test "applyAdaptiveDelta ret 2, ins 'abc', ret 2, ins 'efg'" {
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

            let ydoc = Y.Doc.Create ()
            let ytext = ydoc.getText "test"
            let _ = ytext.insert(0, "0123456789")

            Y.Text.Impl.applyAdaptiveDelta list delta ytext

            Expect.equal (ytext.toString()) "01abc23efg456789" "ytext doesn't equal expected value"
        }
        test "applyAdaptiveDelta del 2" {
            let input = [
                0, ElementOperation.Remove
                1, ElementOperation.Remove
            ]
            let list, delta = toIndexListDelta input

            let ydoc = Y.Doc.Create ()
            let ytext = ydoc.getText "test"
            let _ = ytext.insert(0, "0123456789")

            Y.Text.Impl.applyAdaptiveDelta list delta ytext

            Expect.equal (ytext.toString()) "23456789" "ytext doesn't equal expected value"
        }
        test "applyAdaptiveDelta ret 1, del 2" {
            let input = [
                // 0
                1, ElementOperation.Remove
                2, ElementOperation.Remove
            ]
            let list, delta = toIndexListDelta input

            let ydoc = Y.Doc.Create ()
            let ytext = ydoc.getText "test"
            let _ = ytext.insert(0, "0123456789")

            Y.Text.Impl.applyAdaptiveDelta list delta ytext

            Expect.equal (ytext.toString()) "03456789" "ytext doesn't equal expected value"
        }
        test "applyAdaptiveDelta ret 2, del 2" {
            let input = [
                // 0
                // 1
                2, ElementOperation.Remove
                3, ElementOperation.Remove
            ]
            let list, delta = toIndexListDelta input

            let ydoc = Y.Doc.Create ()
            let ytext = ydoc.getText "test"
            let _ = ytext.insert(0, "0123456789")

            Y.Text.Impl.applyAdaptiveDelta list delta ytext

            Expect.equal (ytext.toString()) "01456789" "ytext doesn't equal expected value"
        }
        test "applyAdaptiveDelta []" {
            let input = []
            let list, delta = toIndexListDelta input

            let ydoc = Y.Doc.Create ()
            let ytext = ydoc.getText "test"
            let _ = ytext.insert(0, "abc")

            Y.Text.Impl.applyAdaptiveDelta list delta ytext

            Expect.equal (ytext.toString()) "abc" "ytext doesn't equal expected value"
        }
        test "applyAdaptiveDelta ins 'abc', ret 2, del 2" {
            let input = [
                0, ElementOperation.Set 'a'
                1, ElementOperation.Set 'b'
                2, ElementOperation.Set 'c'
                // 3
                // 4
                5, ElementOperation.Remove
                6, ElementOperation.Remove
            ]
            let list, delta = toIndexListDelta input

            let ydoc = Y.Doc.Create ()
            let ytext = ydoc.getText "test"
            let _ = ytext.insert(0, "0123456789")

            Y.Text.Impl.applyAdaptiveDelta list delta ytext

            Expect.equal (ytext.toString()) "abc01456789" "ytext doesn't equal expected value"
        }
        test "applyAdaptiveDelta del 2, insert 'efg'" {
            let input = [
                0, ElementOperation.Remove
                1, ElementOperation.Remove
                2, ElementOperation.Set 'a'
                3, ElementOperation.Set 'b'
                4, ElementOperation.Set 'c'
            ]
            let list, delta = toIndexListDelta input

            let ydoc = Y.Doc.Create ()
            let ytext = ydoc.getText "test"
            let _ = ytext.insert(0, "0123456789")

            Y.Text.Impl.applyAdaptiveDelta list delta ytext

            Expect.equal (ytext.toString()) "abc23456789" "ytext doesn't equal expected value"
        }
        test "applyAdaptiveDelta ins 'abc', ret 2, del 2, insert 'efg'" {
            let input = [
                0, ElementOperation.Set 'a'
                1, ElementOperation.Set 'b'
                2, ElementOperation.Set 'c'
                // 3
                // 4
                5, ElementOperation.Remove
                6, ElementOperation.Remove
                7, ElementOperation.Set 'e'
                8, ElementOperation.Set 'f'
                9, ElementOperation.Set 'g'
            ]
            let list, delta = toIndexListDelta input

            let ydoc = Y.Doc.Create ()
            let ytext = ydoc.getText "test"
            let _ = ytext.insert(0, "0123456789")

            Y.Text.Impl.applyAdaptiveDelta list delta ytext

            Expect.equal (ytext.toString()) "abc01efg456789" "ytext doesn't equal expected value"
        }
        test "applyAdaptiveDelta given \"abd\", insert 'c' between the 'b' and the 'd'" {
            let input = [
                2, ElementOperation.Set 'c'
            ]
            let list, delta = toIndexListDelta input

            let ydoc = Y.Doc.Create ()
            let ytext = ydoc.getText "test"
            let _ = ytext.insert(0, "abd")

            Y.Text.Impl.applyAdaptiveDelta list delta ytext

            Expect.equal (ytext.toString()) "abcd" "ytext doesn't equal expected value"
        }
    ]
 ]
