using TagEkyc.Infrastructure.RawExport;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
var options = RawIngressBrokerOptions.Read(builder.Configuration);
// Explicit listen is the only endpoint; configured additional Kestrel endpoints
// could otherwise silently add a public listener alongside the private socket.
if (builder.Configuration.GetSection("Kestrel:Endpoints").GetChildren().Any())
    throw new InvalidOperationException("RAW_INGRESS_BROKER_LISTENER_INVALID");
builder.WebHost.ConfigureKestrel(server =>
{
    server.AddServerHeader = false;
    server.Listen(options.ListenAddress, options.BaseUri.Port);
    server.Limits.MaxRequestBodySize = 16384;
    server.Limits.RequestHeadersTimeout = TimeSpan.FromMilliseconds(options.RequestTimeoutMilliseconds);
});
// Qualified database/profile/key services must be explicitly composed. Their
// absence stays 503; this host never installs synthetic providers by default.
builder.Services.AddTagEkycRawIngressBrokerTransport(options);
var app = builder.Build();
app.Run(async context =>
{
    var request = context.Request;
    var facts = new RawIngressBrokerNetworkRequest(context.Connection.RemoteIpAddress,
        context.Connection.LocalIpAddress, context.Connection.LocalPort, request.Host.Value,
        request.Method, request.Path.Value ?? "", request.QueryString.HasValue, request.ContentType ?? "",
        request.ContentLength, request.Headers.ContainsKey("Content-Encoding") || request.Headers.ContainsKey("Transfer-Encoding"),
        request.Headers.Host.Count != 1 || request.Headers["Content-Length"].Count != 1 || request.Headers.ContentType.Count != 1);
    var reply = await context.RequestServices.GetRequiredService<RawIngressBrokerTransport>()
        .HandleAsync(facts, request.Body, context.RequestAborted);
    context.Response.StatusCode = reply.StatusCode;
    context.Response.ContentLength = reply.Body.Length;
    if (reply.StatusCode == 200) context.Response.ContentType = "application/json";
    if (reply.Body.Length != 0) await context.Response.Body.WriteAsync(reply.Body, context.RequestAborted);
});
await app.RunAsync();
