﻿module GoogleDrive.Tests

open Xunit
open RZ.Google.Auth
open RZ.Foundation.FSharp
open FluentAssertions

[<Fact>]
let ``Try deserialize ClientSecrets`` () =
    let s = """{ "web": { "client_id": "CLIENT ID", "client_secret": "CLIENT SECRET" } }"""
    let json = GoogleClientSecrets.loadFromString(s)

    json.Web.IsSome.Should().BeTrue(null) |> ignore

    let secret = json.Web.Get()
    secret.ClientId.Should().Be("CLIENT ID", null) |> ignore
    secret.ClientSecret.Should().Be("CLIENT SECRET", null) |> ignore

    let otherSecret = json |> GoogleClientSecrets.secrets |> Option.get
    otherSecret.Should().Be(secret, null) |> ignore