# RZ Google API

## Authentication
Suppose you already have `credentials.json` file. To get authorized token for Google Drive, as an example, you can just load the secret and pass it to `authorize` function.

```
open RZ.Google.Auth
open RZ.Google.Auth.Authentications
open RZ.Google.Drive

let token =
    GoogleClientSecrets.loadFromFile @"C:\SAMPLE_PATH\credentials.json"
    |> authorize [DriveScopes.Drive]

printfn "Authorized token: %A" token
```

## Team Drives API
### List shared drives

To call Google APIs, you need to pass `TokenResponse` from Authentication section in API

```
// assume, let token: TokenResponse = authorize scopes secrets

open RZ.Google.Drive

printfn "Drives %s" (TeamDrives.list token)
```