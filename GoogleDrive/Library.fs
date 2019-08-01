namespace RZ.Google.Drive

open Newtonsoft.Json
open Newtonsoft.Json.Serialization
open System.IO
open RZ.Foundation.FSharp

[<JsonObject(NamingStrategyType=typeof<SnakeCaseNamingStrategy>)>]
type ClientSecrets = {
    ClientId: string
    ClientSecret: string
}

[<JsonObject(NamingStrategyType=typeof<SnakeCaseNamingStrategy>)>]
type GoogleClientSecrets = {
    [<JsonConverter(typeof<OptionConverter<ClientSecrets>>)>]
    Installed: ClientSecrets option

    [<JsonConverter(typeof<OptionConverter<ClientSecrets>>)>]
    Web: ClientSecrets option
}

module GoogleClientSecrets =
    let secrets gcs = gcs.Installed |> Option.orElse gcs.Web

    let loadFromString :string -> Try<GoogleClientSecrets> = Try.call JsonConvert.DeserializeObject<GoogleClientSecrets>
    let loadFromFile :string -> Try<GoogleClientSecrets> = Try.call File.ReadAllText >> Try.bind loadFromString
    
type GoogleClientSecrets with
    member self.secrets() = GoogleClientSecrets.secrets self