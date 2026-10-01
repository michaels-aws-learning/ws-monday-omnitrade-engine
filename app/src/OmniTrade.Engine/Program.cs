using System.Text.Json.Serialization;
using OmniTrade.Brokers.Alpaca;
using OmniTrade.Domain;
using OmniTrade.Engine.Hubs;
using OmniTrade.Engine.Services;

var builder = WebApplication.CreateBuilder(args);

// Structured JSON logs to stdout, which CloudWatch picks up from Fargate automatically
builder.Logging.ClearProviders();

builder.Logging.AddJsonConsole();

// Config comes from appsettings, then environment variables (Alpaca__KeyId, Alpaca__SecretKey, ...)
builder.Services.Configure<AlpacaOptions>(builder.Configuration.GetSection("Alpaca"));

builder.Services.AddSingleton<IBrokerAdapter, AlpacaBrokerAdapter>();

builder.Services.AddSignalR();

builder.Services.AddHealthChecks();

builder.Services.AddHostedService<EngineWorker>();

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

// Give the engine time to pause algos when ECS sends SIGTERM
builder.Services.Configure<HostOptions>(o => o.ShutdownTimeout = TimeSpan.FromSeconds(20));

var app = builder.Build();

app.MapHealthChecks("/health");

app.MapHub<BlotterHub>("/hubs/blotter");

app.UseHttpsRedirection();

app.UseRouting();

app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());

app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapGet("/", async context =>
    {
        await context.Response.WriteAsync("OmniTrade Engine is running.");
    });
});

app.Run();