module Elmish

open System

open Elmish

// Based on https://github.com/fable-compiler/Fable.Expect/blob/97385282a3030fb35aadf9687810e182c9ad37f4/src/Fable.Expect/Expect.Elmish.fs
//
// MIT License
// 
// Copyright (c) 2021 Alfonso Garcia-Caro
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
module Program =

    type ElmishDispatcher<'Model, 'Msg> =
        inherit IDisposable
        abstract Model: 'Model
        abstract Dispatch: 'Msg -> unit

    let private disposeModel (model: obj) =
        match model with
        | :? IDisposable as disp -> disp.Dispose()
        | _ -> ()

    /// Returns a handler to retrieve the model and dispatch messages
    let testWith (arg: 'arg) (program: Program<'arg, 'model, 'msg, unit>) =
        let mutable model = Unchecked.defaultof<_>
        let mutable dispatch = Unchecked.defaultof<_>

        let setState model' dispatch' =
            model <- model'
            dispatch <- dispatch'

        Program.withSetState setState program
        |> Program.runWith arg

        { new ElmishDispatcher<'model, 'msg> with
            member _.Dispose() = disposeModel model
            member _.Model = model
            member _.Dispatch msg = dispatch msg
        }

    /// Returns a handler to retrieve the model and dispatch messages
    let test (program: Program<unit, 'model, 'msg, unit>) =
        testWith () program