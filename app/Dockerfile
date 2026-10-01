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