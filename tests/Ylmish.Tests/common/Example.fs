namespace Example

open Adaptify
open FSharp.Data.Adaptive

type Message =
    | SetPropA of string
    | SetPropB of string option
    | AddPropC of Submodel
    | RemPropC of Submodel
    | AddPropD of string
    | RemPropD of string
    | SetPropE of Submodel
    | SetPropF of Submodel option
and [<ModelType>] Model = {
    PropA : string
    PropB : string option
    PropC : IndexList<Submodel>
    PropD : IndexList<string>
    PropE : Submodel
    PropF : Submodel option
}
and [<ModelType>] Submodel = {
    Prop0 : string
}

module Model =
    let update msg model =
        match msg with
        | SetPropA value ->
            { model with PropA = value }
        | SetPropB value ->
            { model with PropB = value }
        | AddPropC item ->
            { model with PropC = model.PropC.Add item }
        | RemPropC item ->
            { model with PropC = model.PropC.Remove item }
        | AddPropD item ->
            { model with PropD = model.PropD.Add item }
        | RemPropD item ->
            { model with PropD = model.PropD.Remove item }
        | SetPropE value ->
            { model with PropE = value }
        | SetPropF value ->
            { model with PropF = value }

    let view _ _ = ()
