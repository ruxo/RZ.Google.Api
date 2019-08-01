module RZ.Foundation.FSharp

open Newtonsoft.Json
open System

[<NoComparison; CustomEquality>]
[<DefaultAugmentation(false)>]
type ApiResult<'T when 'T: equality> =
    | Success of 'T
    | Failure of exn

    member self.IsSuccess
        with get() =
            match self with
            | Success _ -> true
            | Failure _ -> false

    member self.IsFailure
        with get() =
            match self with
            | Success _ -> false
            | Failure _ -> true

    member self.Equals(other: ApiResult<'T>) =
        match (self, other) with
        | (Success x, Success y) -> x = y
        | (Failure x, Failure y) -> x.ToString() = y.ToString()
        | _ -> false

    override self.Equals other =
        match other with
        | :? ApiResult<'T> as otherInstance -> self.Equals(otherInstance)
        | _ -> false

    override self.GetHashCode() =
        match self with
        | Success data -> data.GetHashCode()
        | Failure ex -> ex.GetHashCode()

module ApiResult =
    let inline isSuccess (result: ApiResult<'a>) = result.IsSuccess
    let inline isFailure (result: ApiResult<'a>) = result.IsFailure

    let get = function
    | Success v -> v
    | Failure ex -> raise <| InvalidOperationException("Cannot retrieve value from ApiResult's failure state", ex)

type ApiResult<'T when 'T: equality> with
    member inline self.Get() = ApiResult.get self

// --------------- TRY ---------------
[<NoComparison; NoEquality>]
type Try<'T when 'T: equality> = Runnable of (unit -> 'T)

module Try =
    let ``try`` (Runnable f) =
        try
            Success <| f()
        with 
        | ex -> Failure ex

    let call f x = Runnable (fun () -> f x)
    let map g (Runnable f) = Runnable <| (f >> g)
    let bind g (Runnable f) = Runnable <| fun() -> let (Runnable v) = g(f()) in v()

type Try<'T when 'T: equality> with
    member inline self.Try() = Try.``try`` self

let inline ``try`` f = Runnable f

// ----------------- Newtonsoft, OPTION CONVERTER -----------------
type OptionConverter<'T>() =
    inherit JsonConverter()

    override __.CanConvert objectType = objectType = typeof<Option<'T>>

    override __.ReadJson(reader, _, _, serializer) =
        if reader.TokenType = JsonToken.Null
            then None
            else Some (serializer.Deserialize<'T> reader)
        :> obj

    override __.WriteJson(writer, value, serializer) =
        match value with
        | :? Option<'T> as opt ->
            match opt with
            | Some v -> serializer.Serialize(writer, v, typeof<'T>)
            | None -> serializer.Serialize(writer, null)
        | _ -> failwithf "OptionConverter: invalid value (Type = %s) as option" (value.GetType().Name)

// --------------- OPTION ---------------
type Option<'a> with
    member opt.Get() = Option.get opt

