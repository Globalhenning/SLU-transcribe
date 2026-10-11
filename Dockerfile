# syntax=docker/dockerfile:1

# ---- Build ----
# Bygger på värdens arkitektur och korskompilerar till målarkitekturen (amd64/arm64).
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /src

COPY src/SluTranscribe.Web/SluTranscribe.Web.csproj src/SluTranscribe.Web/
RUN dotnet restore src/SluTranscribe.Web/SluTranscribe.Web.csproj -a $TARGETARCH

COPY src/ src/
RUN dotnet publish src/SluTranscribe.Web/SluTranscribe.Web.csproj \
        -c Release -a $TARGETARCH --no-restore \
        -o /app/publish /p:UseAppHost=false \
    && mkdir -p /keys

# ---- Runtime ----
# Chiseled: minimal Ubuntu-image utan shell och pakethanterare, kör som icke-root.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS runtime
WORKDIR /app

COPY --from=build --chown=$APP_UID /keys /keys
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
    DataProtection__KeysPath=/keys

VOLUME /keys
EXPOSE 8080
USER $APP_UID

ENTRYPOINT ["dotnet", "SluTranscribe.Web.dll"]
