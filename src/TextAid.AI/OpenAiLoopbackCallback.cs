using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TextAid.AI;

/// <summary>Receives a single OAuth callback on IPv4 loopback without Windows HTTP.sys registration.</summary>
public sealed class OpenAiLoopbackCallback : IDisposable
{
    private readonly TcpListener listener = new(IPAddress.Loopback, 0);
    public string RedirectUri { get; }

    public OpenAiLoopbackCallback()
    {
        listener.Start(4);
        RedirectUri = $"http://127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}/auth/callback";
    }

    /// <summary>Ignores unrelated browser requests and bounds each connection as well as the entire attempt.</summary>
    public async Task<Uri> ReceiveAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            using var connection = await listener.AcceptTcpClientAsync(cancellationToken);
            using var requestTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            requestTimeout.CancelAfter(TimeSpan.FromSeconds(5));
            try
            {
                using var stream = connection.GetStream();
                var request = new List<byte>();
                var buffer = new byte[1];
                while (request.Count < 16384 && await stream.ReadAsync(buffer, requestTimeout.Token) != 0)
                {
                    request.Add(buffer[0]);
                    var count = request.Count;
                    if (count >= 4 && request[count - 4] == 13 && request[count - 3] == 10 && request[count - 2] == 13 && request[count - 1] == 10) break;
                }
                var headers = Encoding.ASCII.GetString(request.ToArray()).Split("\r\n");
                var firstLine = headers[0].Split(' ');
                var host = new Uri(RedirectUri).Authority;
                var valid = firstLine.Length == 3 && firstLine[0] == "GET" && firstLine[2] == "HTTP/1.1" &&
                    firstLine[1].StartsWith("/auth/callback?", StringComparison.Ordinal) &&
                    headers.Count(line => line.StartsWith("Host:", StringComparison.OrdinalIgnoreCase)) == 1 &&
                    headers.Any(line => line.Equals("Host: " + host, StringComparison.OrdinalIgnoreCase)) &&
                    headers[^1] == "";
                var body = valid ? "Browser callback received. This does not yet confirm sign-in. Return to the same TextAid window and check the connection result while the app validates and saves the session." : "Invalid callback request.";
                var bytes = Encoding.UTF8.GetBytes(body);
                var response = Encoding.ASCII.GetBytes($"HTTP/1.1 {(valid ? "200 OK" : "400 Bad Request")}\r\nContent-Type: text/plain; charset=utf-8\r\nContent-Length: {bytes.Length}\r\nCache-Control: no-store\r\nReferrer-Policy: no-referrer\r\nConnection: close\r\n\r\n");
                await stream.WriteAsync(response, requestTimeout.Token);
                await stream.WriteAsync(bytes, requestTimeout.Token);
                if (valid) return new Uri("http://" + host + firstLine[1]);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { }
            catch (IOException) { }
        }
    }

    public void Dispose() => listener.Stop();
}
