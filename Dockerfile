FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the .csproj files first so the restore layer is cached
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

# curl for the healthcheck and a logs folder writable by the non-root user
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /app/logs /app/data && chown $APP_UID /app/logs /app/data

# Do not run as root
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Api.dll"]