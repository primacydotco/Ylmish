[<RequireQualifiedAccess>]
module Ylmish.Adaptive.Index

open FSharp.Data.Adaptive

let private generator (count : int) (f : int -> Index -> 'T) (i : int, j : Index) : ('T * (int * Index)) option =
    if i > count then None else
    Some (f i j, (i + 1, Index.after j))

let generate f start count =
    List.unfold (generator count f) (0, start)

let increment start i =
    generate (fun _ i -> i) start i
    |> List.tryLast
    |> Option.defaultValue start

let at =
    // Index.zero seems to be roughly equivalent to a normal index of -1 i.e. it is the index which points before the 1st item.
    //
    // Therefore, the index of the 1st item should be after Index.zero.
    increment (Index.after Index.zero)
