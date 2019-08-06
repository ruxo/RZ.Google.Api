namespace RZ.Google.Auth

open System.IO
open RZ.Foundation.FSharp
open System
open RZ.Google.Auth.OAuth.LocalServer
open FSharp.Data
open RZ.Google.Common

type ClientSecrets = {
    ClientId: string
    ClientSecret: string
}

type GoogleClientSecrets = {
    Installed: ClientSecrets option
    Web: ClientSecrets option
}

module GoogleClientSecrets =
    let secrets gcs = gcs.Installed |> Option.orElse gcs.Web

    let loadFromString secretContent = 
        let secrets = secretContent |> jsonDeserializer<GoogleClientSecrets>
        if secrets.Installed.IsSome || secrets.Web.IsSome
            then secrets
            else invalidArg "secretContent" "Invalid secret keys content"

    let loadFromFile = File.ReadAllText >> loadFromString >> secrets >> Option.get
    
type GoogleClientSecrets with
    member self.secrets() = GoogleClientSecrets.secrets self

module Authentications =
    type TokenResponse = {
        AccessToken: string
        TokenType: string
        Scope: string
        ExpiresIn: int64
        RefreshToken: string option
    }

    type AuthorizationError = {
        Error: string
        ErrorDescription: string
        ErrorUri: string
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
    [<Literal>]
    let private OidcTokenUrl = "https://oauth2.googleapis.com/token"

    let private retrieveAuthorizationCode (scopes: string seq) (clientSecrets: ClientSecrets) =
        async {
            use receiver = createLocalServerCodeReceiver().UnsafeTry()
            let uri = sprintf "%s?access_type=offline&response_type=code&client_id=%s&redirect_uri=%s&scope=%s" 
                                OidcAuthorizationUrl clientSecrets.ClientId (Uri.EscapeUriString receiver.RedirectUri) (Uri.EscapeUriString <| scopes.Join(" "))
            receiver.Start()
            openBrowser uri |> ignore
            let! oauthResponse = receiver.Listen()
            if oauthResponse.ContainsKey "code"
                then return Ok (oauthResponse.["code"], receiver.RedirectUri)
                else return Error <| { Error=oauthResponse.["error"]
                                       ErrorDescription=oauthResponse.["error_description"]
                                       ErrorUri=oauthResponse.["error_uri"] }
        }

    let private exchangeCodeForToken (scopes: string seq) (clientSecrets: ClientSecrets) code redirectUri =
        let payload = [
            "grant_type", "authorization_code"
            "scope", scopes.Join(" ")
            "code", (code: string)
            "redirect_uri", (redirectUri: string)
            "client_id", clientSecrets.ClientId
            "client_secret", clientSecrets.ClientSecret
        ]
        async {
            let! response = Http.AsyncRequestString(OidcTokenUrl, body = FormValues payload)
            return response |> jsonDeserializer<TokenResponse>
        }

    exception AuthenticationError of AuthorizationError

    let authorize scopes clientSecrets =
        async {
            let! authCode = retrieveAuthorizationCode scopes clientSecrets
            match authCode with
            | Ok (code, redirectUri) -> return! exchangeCodeForToken scopes clientSecrets code redirectUri
            | Error err -> return raise <| AuthenticationError err
        }