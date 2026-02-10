# ── Stage 1: Build ────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj first for layer-cached restore
COPY src/BasecampSocial.Api/BasecampSocial.Api.csproj src/BasecampSocial.Api/
RUN dotnet restore src/BasecampSocial.Api/BasecampSocial.Api.csproj

# Copy everything else and publish
COPY . .
WORKDIR /src/src/BasecampSocial.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

# ── Stage 2: Runtime ─────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Non-root user for security
RUN groupadd --system appgroup && useradd --system --gid appgroup --no-create-home appuser

COPY --from=build /app/publish .

# Expose HTTP port (Kestrel default)
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

USER appuser
ENTRYPOINT ["dotnet", "BasecampSocial.Api.dll"]
