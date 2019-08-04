module internal RZ.Google.Common

open FSharp.Json

let jsonDeserializer<'T> = Json.deserializeEx<'T> <| JsonConfig.create(jsonFieldNaming = Json.snakeCase)

