# ── 1) Build Stage ───────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["DiffDetector.csproj", "."]
RUN dotnet restore "./DiffDetector.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "DiffDetector.csproj" -c $BUILD_CONFIGURATION -o /app/build

# ── 2) Publish Stage ─────────────────────────────────────────
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "DiffDetector.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# ── 3) Runtime Stage ─────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DiffDetector.dll"]
