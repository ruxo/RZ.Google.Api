module RZ.Google.Auth.OAuth.LocalServer

open System
open RZ.Foundation.FSharp
open System.Net.Sockets
open System.Net
open FSharp.Control
open System.Text

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
type LocalServerCodeReceiver(port: int, closePageResponse) =

    let redirectUri = System.String.Format(CallbackUriTemplate127001, port.ToString())
    let listener = TcpListener(IPAddress.Loopback, port)

    let rec readStream buffer (stream: NetworkStream) =
        asyncSeq {
            let! size = stream.ReadAsync(buffer, 0, buffer.Length) |> Async.AwaitTask
            if size > 0 then
                for i in 0 .. size-1 do
                    yield char(buffer.[i])
                yield! readStream buffer stream
        }

    let readLines (stringSeq: AsyncSeq<char>) =
        asyncSeq {
            let sb = StringBuilder()
            for c in stringSeq do
                sb.Append(c) |> ignore
                if c = '\n' && sb.Length > 0 && sb.[sb.Length-2] = '\r' then
                    yield sb.ToString()
                    sb.Clear() |> ignore
            if sb.Length > 0 then
                yield sb.ToString()
        }

    interface IDisposable with
        member __.Dispose() = listener.Stop()

    member __.RedirectUri with get() = redirectUri

    member __.Start() = listener.Start()

    member __.Listen() =
        async {
            use! client = listener.AcceptTcpClientAsync() |> Async.AwaitTask
            let stream = client.GetStream()
            let buffer = Array.zeroCreate NetworkReadBufferSize
            let! content = stream |> readStream buffer |> readLines |> AsyncSeq.toArrayAsync
            for s in content do
                Console.Write(s)
            return content
        }

let createLocalServerCodeReceiver() =
    getRandomUnusedPort() |> Try.map (fun port -> new LocalServerCodeReceiver(port, DefaultClosePageResponse))