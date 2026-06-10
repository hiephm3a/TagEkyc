using System.Reflection;
using ApplicationMarker = TagEkyc.Application.AssemblyMarker;
using TagEkyc.Contracts;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.UseHttpsRedirection();

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    service = "TagEkyc.Api",
}));

app.MapGet("/build", () => Results.Ok(new
{
    service = "TagEkyc.Api",
    version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0",
    environment = app.Environment.EnvironmentName,
    applicationAssembly = typeof(ApplicationMarker).Assembly.GetName().Name,
}));

app.MapGet("/", () => Results.Ok(new SessionStatusPlaceholder(
    "skeleton",
    "STANDARD_EKYC_PROFILE",
    "CREATED",
    "NOT_AVAILABLE")));

app.Run();
