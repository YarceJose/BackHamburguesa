FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar solo los .csproj primero: el restore queda cacheado
COPY Domain/Domain.csproj                 Domain/
COPY Application/Application.csproj       Application/
COPY Infrastructure/Infrastructure.csproj Infrastructure/
COPY Api/Api.csproj                       Api/
RUN dotnet restore Api/Api.csproj

COPY . .
RUN dotnet publish Api/Api.csproj -c Release -o /app/publish --no-restore

# ---------- Runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# curl para el healthcheck y carpeta de logs con permisos para el usuario sin privilegios
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /app/logs && chown $APP_UID /app/logs

# No correr como root
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Api.dll"]