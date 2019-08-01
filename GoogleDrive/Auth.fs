namespace RZ.Google.Auth

open Newtonsoft.Json
open Newtonsoft.Json.Serialization
open System.IO
open RZ.Foundation.FSharp
open System.Collections.Generic
open System
open RZ.Google.Auth.OAuth.LocalServer

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

module Authentications =
    [<JsonObject(NamingStrategyType=typeof<SnakeCaseNamingStrategy>)>]
    type TokenResponse = {
        AccessToken: string
        TokenType: string
        Scope: string
        IdToken: string
        [<JsonConverter(typeof<OptionConverter<int64>>)>]
        ExpiresIn: int64 option
        [<JsonConverter(typeof<OptionConverter<string>>)>]
        RefreshToken: string option
    }

    [<JsonObject(NamingStrategyType=typeof<SnakeCaseNamingStrategy>)>]
    type AuthorizationRequestParams = {
        ResponseType: string
        ClientId: string
        RedirectUri: string
        Scope: string
        State: string

        AccessType: string
        Prompt: string
        LoginHint: string option
        IncludeGrantedScopes: string option
        Nonce: string
    }

    [<NoComparison; NoEquality>]
    type private GoogleAuthorizationCodeRequestUrl = {
        AuthorizationServerUrl: Uri
        QueryParams: AuthorizationRequestParams
        UserDefinedQueryParams: KeyValuePair<string,string> seq
    }

    open System.Diagnostics
    open System.Runtime.InteropServices
    open System.Text.RegularExpressions
    let private openBrowser url =
        let isOS = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform
        if isOS OSPlatform.Windows then
            let escape1 = Regex.Replace(url, @"(\\*)" + "\"", @"$1$1\" + "\"")
            let escape2 = Regex.Replace(escape1, @"(\\*)" + "\"", @"$1$1\" + "\"")
            ProcessStartInfo("cmd", sprintf """/c start "" "%s" """ escape2, CreateNoWindow = true)
                |> Process.Start
        else if isOS OSPlatform.Linux then
            Process.Start("xdg-open", url)
        else if isOS OSPlatform.OSX then
            Process.Start("open", url)
        else
            failwith "Cannot open browser. Unknown OS type!"

    [<Literal>]
    let private OidcAuthorizationUrl = "https://accounts.google.com/o/oauth2/v2/auth"

    let authorize userId (scopes: string seq) (clientSecrets: ClientSecrets) =
        async {
            use receiver = createLocalServerCodeReceiver().UnsafeTry()
            let uri = sprintf "%s?access_type=offline&response_type=code&client_id=%s&redirect_uri=%s&scope=%s" 
                                OidcAuthorizationUrl clientSecrets.ClientId (Uri.EscapeUriString receiver.RedirectUri) (Uri.EscapeUriString <| scopes.Join(" "))
            receiver.Start()
            openBrowser uri |> ignore
            return! receiver.Listen()
        }