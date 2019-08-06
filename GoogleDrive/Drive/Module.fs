module RZ.Google.Drive

open RZ.Google.Auth.Authentications
open FSharp.Data

[<Literal>]
let private BaseUri = "https://www.googleapis.com/drive/v3/"

[<RequireQualifiedAccess>]
module DriveScopes =
    /// <summary>See, edit, create, and delete all of your Google Drive files</summary>
    [<Literal>]
    let Drive = "https://www.googleapis.com/auth/drive"

    /// <summary>View and manage its own configuration data in your Google Drive</summary>
    [<Literal>]
    let DriveAppdata = "https://www.googleapis.com/auth/drive.appdata"

    /// <summary>View and manage Google Drive files and folders that you have opened or created with this app</summary>
    [<Literal>]
    let DriveFile = "https://www.googleapis.com/auth/drive.file"

    /// <summary>View and manage metadata of files in your Google Drive</summary>
    [<Literal>]
    let DriveMetadata = "https://www.googleapis.com/auth/drive.metadata"

    /// <summary>View metadata for files in your Google Drive</summary>
    [<Literal>]
    let DriveMetadataReadonly = "https://www.googleapis.com/auth/drive.metadata.readonly"

    /// <summary>View the photos, videos and albums in your Google Photos</summary>
    [<Literal>]
    let DrivePhotosReadonly = "https://www.googleapis.com/auth/drive.photos.readonly"

    /// <summary>See and download all your Google Drive files</summary>
    [<Literal>]
    let DriveReadonly = "https://www.googleapis.com/auth/drive.readonly"

    /// <summary>Modify your Google Apps Script scripts' behavior</summary>
    [<Literal>]
    let DriveScripts = "https://www.googleapis.com/auth/drive.scripts"

[<RequireQualifiedAccess>]
module TeamDrives =
    [<Literal>]
    let private RestPath = "teamdrives"

    let list (token: TokenResponse) =
        Http.AsyncRequestString(BaseUri + RestPath, httpMethod = "GET",
                headers = [ "Authorization", "Bearer " + token.AccessToken ] )
 