module RZ.Google.Auth.OAuth.LocalServer

open System
open RZ.Foundation.FSharp
open System.Net.Sockets
open System.Net
open FSharp.Control
open System.Text
open System.IO

[<Literal>]
let private DefaultClosePageResponse =
    """<html>
    <head><title>OAuth 2.0 Authentication Token Received</title></head>
    <body>
      Received verification code. You may now close this window.
      <script type='text/javascript'>
        // This doesn't work on every browser.
        window.setTimeout(function() {
            this.focus();
            window.opener = this;
            window.open('', '_self', ''); 
            window.close(); 
          }, 1000);
        //if (window.opener) { window.opener.checkToken(); }
      </script>
    </body>
</html>"""

[<Literal>]
let private LoopbackCallbackPath = "/authorize/"
let private CallbackUriTemplate127001 = sprintf "http://127.0.0.1:{0}%s" LoopbackCallbackPath

let private getRandomUnusedPort() =
    ``try`` (fun() -> 
                let listener = new TcpListener(IPAddress.Loopback, 0)
                try
                    listener.Start()
                    (listener.LocalEndpoint :?> IPEndPoint).Port
                finally
                    listener.Stop()
             )

[<Literal>]
let private NetworkReadBufferSize = 1024
type LocalServerCodeReceiver(port: int, closePageResponse: string) =

    let redirectUri = System.String.Format(CallbackUriTemplate127001, port.ToString())
    let listener = TcpListener(IPAddress.Loopback, port)

    let readLines (stream: StreamReader) =
        asyncSeq {
            while not stream.EndOfStream do
                let! line = stream.ReadLineAsync() |> Async.AwaitTask
                yield line
        }

    let validateAndGetParameters requestLine =
        let parts = (requestLine: string).Split(' ')
        if parts.Length <> 3 then invalidArg "requestLine" "Request line ill-formatted. Should be '<request-method> <request-path> HTTP/1.1'"

        let verb = parts.[0]
        if verb <> "GET" then invalidArg "requestLine" (sprintf "Expected 'GET' request, got '%s'" verb)

        let path = parts.[1]
        if not <| path.StartsWith LoopbackCallbackPath then invalidArg "requestLine" (sprintf "Expected request path to start '%s', got '%s'" LoopbackCallbackPath path)

        let pathParts = path.Split('?')
        if pathParts.Length = 1 then
            Map.empty
        else if pathParts.Length <> 2 then invalidArg "requestLine" (sprintf "Expected a single '?' in request path, got '%s'" path)
        else
            pathParts.[1].Split([|'&'|], StringSplitOptions.RemoveEmptyEntries)
            |> Seq.map (fun param ->
                let keyValue = param.Split('=')
                (WebUtility.UrlDecode keyValue.[0], WebUtility.UrlDecode <| if keyValue.Length = 1 then String.Empty else keyValue.[1])
            )
            |> Map.ofSeq

    interface IDisposable with
        member __.Dispose() = listener.Stop()

    member __.RedirectUri with get() = redirectUri

    member __.Start() = listener.Start()

    member __.Listen() =
        async {
            use! client = listener.AcceptTcpClientAsync() |> Async.AwaitTask
            printfn "Connection receivd!"
            let stream = client.GetStream()
            let reader = new StreamReader(stream)
            let! requestLine = reader.ReadLineAsync() |> Async.AwaitTask
            let requestParams = validateAndGetParameters requestLine
            do! reader |> readLines 
                       |> AsyncSeq.takeWhile (fun line -> line.Length > 0)
                       |> AsyncSeq.iter Console.WriteLine

            let writer = new StreamWriter(stream)
            do! writer.WriteAsync("HTTP/1.1 200 OK\r\n\r\n") |> Async.AwaitTask
            do! writer.WriteAsync(closePageResponse) |> Async.AwaitTask
            do! writer.FlushAsync() |> Async.AwaitTask
            return requestParams
        }

let createLocalServerCodeReceiverOnPort port = new LocalServerCodeReceiver(port, DefaultClosePageResponse)

let createLocalServerCodeReceiver = getRandomUnusedPort >> Try.map createLocalServerCodeReceiverOnPort

