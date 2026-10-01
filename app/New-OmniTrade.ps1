<#
.SYNOPSIS
    Scaffolds the OmniTrade .NET solution, projects, and container files into an existing repo.

.DESCRIPTION
    Run from the repo root (or pass -Path). Git and the infra/ CDK project are assumed to be
    set up already and are not touched. Existing root files (Dockerfile, compose, etc.) are
    skipped unless -Force is passed.

.EXAMPLE
    .\New-OmniTrade.ps1
    .\New-OmniTrade.ps1 -Path C:\dev\omnitrade
    .\New-OmniTrade.ps1 -Force

.NOTES
    Requires: .NET 10 SDK. Optional: Docker Desktop to build and run the containers.
#>
[CmdletBinding()]
param(
    [string]$Path = ".",
    [string]$Framework = "net10.0",
    [switch]$Force
)

$ErrorActionPreference = "Stop"

# ---------- helpers ----------

function Invoke-Native {
    param([Parameter(Mandatory)][string]$Command, [string[]]$Arguments = @())
    Write-Host "  > $Command $($Arguments -join ' ')" -ForegroundColor DarkGray
    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) { throw "Command failed ($LASTEXITCODE): $Command $($Arguments -join ' ')" }
}

function Write-ProjectFile {
    param(
        [Parameter(Mandatory)][string]$RelativePath,
        [Parameter(Mandatory)][string]$Content,
        [switch]$SkipIfExists
    )
    $full = Join-Path $script:Root $RelativePath
    if ($SkipIfExists -and (Test-Path $full) -and -not $Force) {
        Write-Host "  = $RelativePath (exists, skipped; use -Force to overwrite)" -ForegroundColor DarkYellow
        return
    }
    $dir = Split-Path $full -Parent
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
    # UTF-8 without BOM so Docker parses files cleanly
    [System.IO.File]::WriteAllText($full, $Content.TrimStart("`r", "`n"), [System.Text.UTF8Encoding]::new($false))
    Write-Host "  + $RelativePath" -ForegroundColor DarkGreen
}

function Write-Step([string]$Message) { Write-Host "`n== $Message" -ForegroundColor Cyan }

# ---------- prerequisites ----------

Write-Step "Checking prerequisites"
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "The .NET SDK was not found. Install the .NET 10 SDK from https://dotnet.microsoft.com/download"
}
$sdkMajor = $Framework -replace '^net(\d+)\..*$', '$1'
if (-not ((dotnet --list-sdks) -match "^$sdkMajor\.")) {
    throw "No .NET $sdkMajor SDK found. Install it, or pass -Framework to match an installed SDK (e.g. net9.0)."
}
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Warning "Docker not found. Scaffolding continues, but you will need Docker to build and run the images."
}

$script:Root = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Path)
if (-not (Test-Path $script:Root)) {
    throw "Target folder '$script:Root' does not exist. Run this from the repo root or pass -Path."
}
if (Get-ChildItem $script:Root -Filter "OmniTrade.sln*" -File) {
    throw "An OmniTrade solution already exists in '$script:Root'. Nothing to scaffold."
}
$projectDirs = "src/OmniTrade.Domain", "src/OmniTrade.Brokers.Alpaca", "src/OmniTrade.Engine", "tests/OmniTrade.Domain.Tests"
foreach ($dir in $projectDirs) {
    if (Test-Path (Join-Path $script:Root $dir)) { throw "'$dir' already exists. Remove it or scaffold into a different folder." }
}

Push-Location $script:Root
try {
    # ---------- solution and projects ----------

    Write-Step "Creating solution and projects in $script:Root"
    Invoke-Native dotnet @("new", "sln", "-n", "OmniTrade")
    $sln = (Get-ChildItem -Filter "OmniTrade.sln*" -File | Select-Object -First 1).Name

    Invoke-Native dotnet @("new", "classlib", "-n", "OmniTrade.Domain", "-o", "src/OmniTrade.Domain", "-f", $Framework)
    Invoke-Native dotnet @("new", "classlib", "-n", "OmniTrade.Brokers.Alpaca", "-o", "src/OmniTrade.Brokers.Alpaca", "-f", $Framework)
    Invoke-Native dotnet @("new", "web", "-n", "OmniTrade.Engine", "-o", "src/OmniTrade.Engine", "-f", $Framework)
    Invoke-Native dotnet @("new", "xunit", "-n", "OmniTrade.Domain.Tests", "-o", "tests/OmniTrade.Domain.Tests", "-f", $Framework)

    Remove-Item "src/OmniTrade.Domain/Class1.cs", "src/OmniTrade.Brokers.Alpaca/Class1.cs" -ErrorAction SilentlyContinue
    Remove-Item "tests/OmniTrade.Domain.Tests/UnitTest1.cs" -ErrorAction SilentlyContinue

    Invoke-Native dotnet @("sln", $sln, "add",
        "src/OmniTrade.Domain/OmniTrade.Domain.csproj",
        "src/OmniTrade.Brokers.Alpaca/OmniTrade.Brokers.Alpaca.csproj",
        "src/OmniTrade.Engine/OmniTrade.Engine.csproj",
        "tests/OmniTrade.Domain.Tests/OmniTrade.Domain.Tests.csproj")

    Write-Step "Wiring project references and packages"
    Invoke-Native dotnet @("add", "src/OmniTrade.Brokers.Alpaca", "reference", "src/OmniTrade.Domain")
    Invoke-Native dotnet @("add", "src/OmniTrade.Engine", "reference", "src/OmniTrade.Domain", "src/OmniTrade.Brokers.Alpaca")
    Invoke-Native dotnet @("add", "tests/OmniTrade.Domain.Tests", "reference", "src/OmniTrade.Domain")

    Invoke-Native dotnet @("add", "src/OmniTrade.Brokers.Alpaca", "package", "Alpaca.Markets")
    Invoke-Native dotnet @("add", "src/OmniTrade.Brokers.Alpaca", "package", "Microsoft.Extensions.Options")
    Invoke-Native dotnet @("add", "src/OmniTrade.Brokers.Alpaca", "package", "Microsoft.Extensions.Logging.Abstractions")

    # ---------- domain ----------

    Write-Step "Writing source files"
    Write-ProjectFile "src/OmniTrade.Domain/Orders.cs" @'
namespace OmniTrade.Domain;

public enum OrderSide { Buy, Sell }

public enum OrderType { Market, Limit }

public enum OrderStatus
{
    New,
    PendingNew,
    PartiallyFilled,
    Filled,
    PendingCancel,
    Canceled,
    Rejected
}

/// <summary>A child order sent to a broker. ClientOrderId links fills back to the parent.</summary>
public sealed record OrderRequest(
    string ClientOrderId,
    string Symbol,
    OrderSide Side,
    OrderType Type,
    decimal Quantity,
    decimal? LimitPrice = null);

/// <summary>A broker-neutral execution report (fill, partial fill, cancel, reject).</summary>
public sealed record ExecutionReport(
    string ClientOrderId,
    string? BrokerOrderId,
    OrderStatus Status,
    decimal FilledQuantity,
    decimal? AverageFillPrice,
    DateTimeOffset Timestamp);
'@

    Write-ProjectFile "src/OmniTrade.Domain/IBrokerAdapter.cs" @'
namespace OmniTrade.Domain;

/// <summary>
/// Broker-neutral boundary. Alpaca is one implementation; add a SimulatedBroker for tests and replay.
/// </summary>
public interface IBrokerAdapter
{
    string Name { get; }

    Task ConnectAsync(CancellationToken ct);

    Task SubmitAsync(OrderRequest order, CancellationToken ct);

    Task CancelAsync(string clientOrderId, CancellationToken ct);

    event Action<ExecutionReport>? ExecutionReceived;
}
'@

    # ---------- Alpaca adapter ----------

    Write-ProjectFile "src/OmniTrade.Brokers.Alpaca/AlpacaBrokerAdapter.cs" @'
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OmniTrade.Domain;

namespace OmniTrade.Brokers.Alpaca;

public sealed class AlpacaOptions
{
    public string KeyId { get; set; } = "";
    public string SecretKey { get; set; } = "";
    public bool Paper { get; set; } = true;
}

public sealed class AlpacaBrokerAdapter(
    IOptions<AlpacaOptions> options,
    ILogger<AlpacaBrokerAdapter> logger) : IBrokerAdapter
{
    private readonly AlpacaOptions _options = options.Value;

    public string Name => "Alpaca";

#pragma warning disable CS0067 // raised once the trade_updates stream is wired up
    public event Action<ExecutionReport>? ExecutionReceived;
#pragma warning restore CS0067

    public Task ConnectAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_options.KeyId) || string.IsNullOrWhiteSpace(_options.SecretKey))
        {
            logger.LogWarning("Alpaca keys are not configured; running without a broker connection");
            return Task.CompletedTask;
        }

        // TODO:
        // 1. Create the Alpaca trading client (paper environment when _options.Paper is true).
        // 2. Connect the streaming client and subscribe to trade_updates.
        // 3. Map each update to an ExecutionReport and raise ExecutionReceived.
        // 4. After any reconnect, pull open orders over REST and reconcile before resuming.
        logger.LogInformation("Alpaca adapter configured for {Mode} trading", _options.Paper ? "paper" : "live");
        return Task.CompletedTask;
    }

    public Task SubmitAsync(OrderRequest order, CancellationToken ct) =>
        throw new NotImplementedException("Map OrderRequest to an Alpaca order and submit it with ClientOrderId set.");

    public Task CancelAsync(string clientOrderId, CancellationToken ct) =>
        throw new NotImplementedException("Look up the Alpaca order by client order id and cancel it.");
}
'@

    # ---------- engine host ----------

    Write-ProjectFile "src/OmniTrade.Engine/Program.cs" @'
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

// Give the engine time to pause algos when ECS sends SIGTERM
builder.Services.Configure<HostOptions>(o => o.ShutdownTimeout = TimeSpan.FromSeconds(20));

var app = builder.Build();

app.MapHealthChecks("/health");
app.MapHub<BlotterHub>("/hubs/blotter");
app.MapGet("/", () => "OmniTrade.Engine");

app.Run();
'@

    Write-ProjectFile "src/OmniTrade.Engine/Hubs/BlotterHub.cs" @'
using Microsoft.AspNetCore.SignalR;

namespace OmniTrade.Engine.Hubs;

/// <summary>Pushes order and fill updates to the blotter UI.</summary>
public sealed class BlotterHub : Hub
{
}
'@

    Write-ProjectFile "src/OmniTrade.Engine/Services/EngineWorker.cs" @'
using OmniTrade.Domain;

namespace OmniTrade.Engine.Services;

/// <summary>
/// Long-running engine loop. Owns the broker connection for the life of the process.
/// </summary>
public sealed class EngineWorker(IBrokerAdapter broker, ILogger<EngineWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Engine starting with broker {Broker}", broker.Name);

        try
        {
            await broker.ConnectAsync(stoppingToken);
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // normal shutdown
        }
        finally
        {
            logger.LogInformation("Shutdown requested: pausing algos and halting new child orders");
            // TODO: pause algo schedules and decide whether to cancel open child orders
        }
    }
}
'@

    Write-ProjectFile "src/OmniTrade.Engine/appsettings.json" @'
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Alpaca": {
    "KeyId": "",
    "SecretKey": "",
    "Paper": true
  }
}
'@

    # ---------- tests ----------

    Write-ProjectFile "tests/OmniTrade.Domain.Tests/OrderRequestTests.cs" @'
using OmniTrade.Domain;

namespace OmniTrade.Domain.Tests;

public class OrderRequestTests
{
    [Fact]
    public void Limit_order_keeps_its_price()
    {
        var order = new OrderRequest("c-1", "AAPL", OrderSide.Buy, OrderType.Limit, 100m, 190.25m);

        Assert.Equal(190.25m, order.LimitPrice);
    }
}
'@

    # ---------- containers ----------

    Write-Step "Writing container files"
    Write-ProjectFile "Dockerfile" -SkipIfExists @'
# Build on the host's native architecture, cross-compile for the target (arm64 for Graviton Fargate)
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /src

# Restore first so the package layer is cached between code changes
COPY src/OmniTrade.Domain/OmniTrade.Domain.csproj src/OmniTrade.Domain/
COPY src/OmniTrade.Brokers.Alpaca/OmniTrade.Brokers.Alpaca.csproj src/OmniTrade.Brokers.Alpaca/
COPY src/OmniTrade.Engine/OmniTrade.Engine.csproj src/OmniTrade.Engine/
RUN dotnet restore src/OmniTrade.Engine/OmniTrade.Engine.csproj -a $TARGETARCH

COPY src/ src/
RUN dotnet publish src/OmniTrade.Engine/OmniTrade.Engine.csproj -c Release -a $TARGETARCH --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "OmniTrade.Engine.dll"]
'@

    Write-ProjectFile ".dockerignore" -SkipIfExists @'
**/bin/
**/obj/
**/.vs/
**/.vscode/
**/*.user
**/node_modules/
.git/
infra/
tests/
.env*
docker-compose*.yml
README.md
'@

    Write-ProjectFile "docker-compose.yml" -SkipIfExists @'
services:
  engine:
    build: .
    ports:
      - "8080:8080"
    env_file: .env.local
    environment:
      ConnectionStrings__Postgres: "Host=db;Database=omnitrade;Username=postgres;Password=dev"
    depends_on:
      - db

  db:
    image: postgres:17
    environment:
      POSTGRES_PASSWORD: dev
      POSTGRES_DB: omnitrade
    ports:
      - "5432:5432"
    volumes:
      - pgdata:/var/lib/postgresql/data

volumes:
  pgdata:
'@

    Write-ProjectFile ".env.local.example" -SkipIfExists @'
# Copy to .env.local and fill in your Alpaca paper trading keys. Never commit .env.local.
Alpaca__KeyId=
Alpaca__SecretKey=
Alpaca__Paper=true
'@

    if (-not (Test-Path ".env.local")) {
        Copy-Item ".env.local.example" ".env.local"
        Write-Host "  + .env.local (from example)" -ForegroundColor DarkGreen
    }

    # Make sure local secrets never get committed, without touching the rest of the existing .gitignore
    if (Test-Path ".gitignore") {
        if (-not (Select-String -Path ".gitignore" -Pattern '^\s*\.env\.local\s*$' -Quiet)) {
            Add-Content ".gitignore" "`n# Local secrets`n.env.local"
            Write-Host "  ~ .gitignore (added .env.local)" -ForegroundColor DarkGreen
        }
    }
    else {
        Write-Warning "No .gitignore found at the repo root. Add .env.local, bin/, and obj/ to it before committing."
    }

    # ---------- verify ----------

    Write-Step "Building and testing the solution"
    Invoke-Native dotnet @("build", $sln, "--nologo", "-v", "quiet")
    Invoke-Native dotnet @("test", $sln, "--nologo", "-v", "quiet")

    Write-Host "`nOmniTrade scaffolded at $script:Root" -ForegroundColor Green
    Write-Host "Next: add your Alpaca paper keys to .env.local, then run 'docker compose up --build'." -ForegroundColor Green
}
finally {
    Pop-Location
}
