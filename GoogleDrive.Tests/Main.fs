open RZ.Foundation.FSharp
open RZ.Google.Auth
open RZ.Google.Auth.Authentications
open RZ.Google.Drive
open RZ.Google.Auth.OAuth.LocalServer

let testNormalAuthenticate() =
    let secret = GoogleClientSecrets.loadFromFile @"C:\Workspace\private\drivesync\credentials.json"
                    |> Try.map (GoogleClientSecrets.secrets >> Option.get)
                    |> Try.unsafeTry

    authorize "user" [DriveScopes.Drive] secret
    |> Async.RunSynchronously
    |> ignore

// test payload: http://127.0.0.1:58611/authorize/?code=4/lgF6AuMVj1exracIayVeDT_y9URPmsJRx8yT7eTqp1UTvPqs5HqimGcZzgoUiULvyjKEpIV-JGyobI60V33UK1Q&scope=https://www.googleapis.com/auth/drive
let testLocalServer() =
    use receiver = createLocalServerCodeReceiverOnPort 58611
    printfn "Server is listening at %s" receiver.RedirectUri

    receiver.Start()

    receiver.Listen()
    |> Async.RunSynchronously
    |> printfn "Result: %A"

testLocalServer()

