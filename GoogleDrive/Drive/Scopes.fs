module RZ.Google.Drive

[<RequireQualifiedAccess>]
module DriveScopes =
    /// <summary>See, edit, create, and delete all of your Google Drive files</summary>
    let Drive = "https://www.googleapis.com/auth/drive"

    /// <summary>View and manage its own configuration data in your Google Drive</summary>
    let DriveAppdata = "https://www.googleapis.com/auth/drive.appdata"

    /// <summary>View and manage Google Drive files and folders that you have opened or created with this app</summary>
    let DriveFile = "https://www.googleapis.com/auth/drive.file"

    /// <summary>View and manage metadata of files in your Google Drive</summary>
    let DriveMetadata = "https://www.googleapis.com/auth/drive.metadata"

    /// <summary>View metadata for files in your Google Drive</summary>
    let DriveMetadataReadonly = "https://www.googleapis.com/auth/drive.metadata.readonly"

    /// <summary>View the photos, videos and albums in your Google Photos</summary>
    let DrivePhotosReadonly = "https://www.googleapis.com/auth/drive.photos.readonly"

    /// <summary>See and download all your Google Drive files</summary>
    let DriveReadonly = "https://www.googleapis.com/auth/drive.readonly"

    /// <summary>Modify your Google Apps Script scripts' behavior</summary>
    let DriveScripts = "https://www.googleapis.com/auth/drive.scripts"