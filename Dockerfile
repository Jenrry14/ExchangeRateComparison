# Dockerfile

# Usar la imagen base de .NET 8 SDK para build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Establecer directorio de trabajo
WORKDIR /src

# Copiar archivo de proyecto y restaurar dependencias
COPY ["ExchangeRateComparison.api/ExchangeRateComparison.api.csproj", "ExchangeRateComparison.api/"]
RUN dotnet restore "ExchangeRateComparison.api/ExchangeRateComparison.api.csproj"

# Copiar el resto del código y compilar
COPY . .
WORKDIR "/src/ExchangeRateComparison.api"
RUN dotnet build "ExchangeRateComparison.api.csproj" -c Release -o /app/build

# Publicar la aplicación
FROM build AS publish
RUN dotnet publish "ExchangeRateComparison.api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Usar imagen runtime de .NET 8 para producción
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Instalar curl para health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copiar archivos publicados
COPY --from=publish /app/publish .

# Crear usuario no-root para seguridad
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

# Exponer puerto
EXPOSE 5055

# Variables de entorno
ENV ASPNETCORE_URLS=http://+:5055  
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:5055/health || exit 1  

# Punto de entrada
ENTRYPOINT ["dotnet", "ExchangeRateComparison.api.dll"]