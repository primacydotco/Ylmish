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

let at = increment Index.zero
