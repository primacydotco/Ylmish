module internal Ylmish.Disposables

open System

type CompositeDisposable (disposables : IDisposable seq) =
    new ([<ParamArray>] disposables : IDisposable array) =
        new CompositeDisposable (disposables :> IDisposable seq)
    interface IDisposable with
        member _.Dispose () =
            for disposable in disposables do
                disposable.Dispose ()

