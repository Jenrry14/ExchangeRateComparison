# 🏦 Exchange Rate Comparison API

API para comparar tasas de cambio y obtener la mejor oferta para clientes.

## 📋 Características

- **🔄 Comparación en tiempo real** de múltiples APIs de tasas de cambio
- **🚀 Procesamiento paralelo** para máximo rendimiento
- **🔐 Autenticación dinámica** por headers para cada API
- **⚙️ Configuración dinámica** - habilitar/deshabilitar APIs desde Swagger
- **🛡️ Resilencia** con reintentos automáticos y circuit breaker
- **📊 Estadísticas detalladas** de uso y rendimiento
- **🏥 Health checks** para monitoreo de APIs externas
- **📖 Documentación Swagger** completa
- **🐳 Docker ready** para fácil deployment

## 🏗️ Arquitectura

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Swagger UI    │    │  Exchange Rate   │    │   Mock APIs     │
│                 │───▶│   Controller     │───▶│   (API1/2/3)    │
│ (Authentication)│    │                  │    │                 │
└─────────────────┘    └──────────────────┘    └─────────────────┘
                              │
                              ▼
                       ┌──────────────────┐
                       │ Exchange Rate    │
                       │    Service       │
                       │                  │
                       └──────────────────┘
                              │
                ┌─────────────┼─────────────┐
                ▼             ▼             ▼
        ┌─────────────┐ ┌─────────────┐ ┌─────────────┐
        │ API1 Client │ │ API2 Client │ │ API3 Client │
        │   (JSON)    │ │   (XML)     │ │(JSON Nested)│
        └─────────────┘ └─────────────┘ └─────────────┘
                              │
                              ▼
                    ┌──────────────────┐
                    │ Admin Controller │
                    │ (Dynamic Config) │
                    └──────────────────┘
```

## 🚀 Inicio Rápido

### Prerequisitos

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (para ejecutar con contenedores)

### 🐳 Ejecutar con Docker (Recomendado)

```bash
# 1. Clonar repositorio
git clone <repository-url>

# 2. Navegar al directorio del proyecto
cd ExchangeRateComparison

# 3. Construir y ejecutar con Docker
docker-compose up --build

# 4. La aplicación estará disponible en:
# http://localhost:5055/index.html  (Swagger UI)
```

**💡 Comandos Docker útiles:**
```bash
# Detener la aplicación
docker-compose down

# Ver logs en tiempo real
docker logs exchange-rate-api -f

# Reconstruir después de cambios
docker-compose down
docker-compose up --build
```

### 🔧 Ejecutar localmente

```bash
# Restaurar dependencias
dotnet restore

# Ejecutar aplicación
dotnet run --project ExchangeRateComparison.api

# La aplicación estará disponible en:
# http://localhost:5055/index.html 
```

## 🔐 Autenticación

La API utiliza **autenticación dinámica por headers**. En Swagger UI:

### 1. Click en 🔒 **Authorize**

### 2. Ingresa las API Keys válidas:

| Header | Valor de ejemplo | APIs que acepta |
|--------|------------------|-----------------|
| `X-API1-Key` | `demo-api-key-1` | `demo-api-key-1`, `test-api-key-1`, `valid-key-1` |
| `X-API2-Key` | `demo-api-key-2` | `demo-api-key-2`, `test-api-key-2`, `valid-key-2` |
| `X-API3-Key` | `demo-api-key-3` | `demo-api-key-3`, `test-api-key-3`, `valid-key-3` |

### 3. Click **Authorize** y haz tus requests

## 🚫 **Ejemplo con autenticación inválida:**

Si envías API Keys incorrectas:

```json
curl --location 'http://localhost:5055/api/exchangerate/best-rate' \
--header 'Content-Type: application/json' \
--header 'X-API1-Key: key' \
--header 'X-API2-Key: key' \
--header 'X-API3-Key: key' \
--data '{
    "sourceCurrency": "USD",
    "targetCurrency": "EUR", 
    "amount": 100
  }'
```

**Response (503 Service Unavailable):**
```json
{
  "error": "Service temporarily unavailable",
  "details": "All APIs failed to provide exchange rates. Details: API1: Authentication failed - invalid credentials; API2: Authentication failed - invalid credentials; API3: Authentication failed - invalid credentials",
  "timestamp": "2025-08-04T16:30:00Z",
  "traceId": "00-abc123def456-789ghi012jkl-00"
}
```

## 📚 Endpoints Principales

### 💱 Exchange Rate Endpoints

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| `POST` | `/api/exchangerate/best-rate` | Obtiene la mejor tasa de cambio |
| `GET` | `/api/exchangerate/health` | Estado de salud de APIs |
| `GET` | `/api/exchangerate/statistics` | Estadísticas de uso |
| `GET` | `/api/exchangerate/currencies` | Divisas soportadas |

### ⚙️ Administration Endpoints (NUEVO)

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| `GET` | `/api/admin/apis/status` | Estado actual de todas las APIs |
| `PUT` | `/api/admin/apis/{apiName}/toggle` | Habilitar/deshabilitar una API específica |
| `PUT` | `/api/admin/apis/bulk-toggle` | Cambiar estado de múltiples APIs |
| `POST` | `/api/admin/statistics/reset` | Resetear estadísticas del servicio |

## ⚙️ Gestión Dinámica de APIs

### 🔍 Ver estado de todas las APIs

```bash
curl --location 'http://localhost:5055/api/admin/apis/status'
```

**Response:**
```json
{
  "apis": [
    {
      "name": "API1",
      "isEnabled": true,
      "url": "http://localhost:5055/api1",
      "isHealthy": true
    },
    {
      "name": "API2",
      "isEnabled": true,
      "url": "http://localhost:5055/api2",
      "isHealthy": true
    },
    {
      "name": "API3",
      "isEnabled": false,
      "url": "http://localhost:5055/api3",
      "isHealthy": false
    }
  ],
  "timestamp": "2025-08-04T16:30:00Z"
}
```

### 🔄 Deshabilitar una API específica

```bash
curl --location --request PUT 'http://localhost:5055/api/admin/apis/API1/toggle' \
--header 'Content-Type: application/json' \
--data '{
  "enabled": false
}'
```

**Response:**
```json
{
  "apiName": "API1",
  "enabled": false,
  "message": "API API1 has been disabled",
  "timestamp": "2025-08-04T16:30:00Z"
}
```

### 🔄 Cambiar múltiples APIs a la vez

```bash
curl --location --request PUT 'http://localhost:5055/api/admin/apis/bulk-toggle' \
--header 'Content-Type: application/json' \
--data '{
  "apis": [
    {
      "apiName": "API1",
      "enabled": false
    },
    {
      "apiName": "API2",
      "enabled": true
    },
    {
      "apiName": "API3",
      "enabled": true
    }
  ]
}'
```

**Response:**
```json
{
  "results": [
    {
      "apiName": "API1",
      "enabled": false,
      "success": true,
      "errorMessage": null
    },
    {
      "apiName": "API2", 
      "enabled": true,
      "success": true,
      "errorMessage": null
    },
    {
      "apiName": "API3",
      "enabled": true,
      "success": true,
      "errorMessage": null
    }
  ],
  "successCount": 3,
  "failureCount": 0,
  "timestamp": "2025-08-04T16:30:00Z"
}
```

### 📊 Resetear estadísticas

```bash
curl --location --request POST 'http://localhost:5055/api/admin/statistics/reset'
```

**Response:**
```json
{
  "message": "Statistics have been successfully reset",
  "timestamp": "2025-08-04T16:30:00Z"
}
```

## 💱 Ejemplo de Request Principal

```json
curl --location 'http://localhost:5055/api/exchangerate/best-rate' \
--header 'Content-Type: application/json' \
--header 'X-API1-Key: demo-api-key-1' \
--header 'X-API2-Key: demo-api-key-2' \
--header 'X-API3-Key: demo-api-key-3' \
--data '{
    "sourceCurrency": "USD",
    "targetCurrency": "EUR", 
    "amount": 100
  }'
```

### 📈 Ejemplo de Response

```json
{
  "bestOffer": {
    "apiName": "API1",
    "rate": 0.85,
    "convertedAmount": 85.0,
    "responseTimeMs": 245.7,
    "success": true,
    "timestamp": "2025-08-04T16:30:00Z"
  },
  "allResults": [
    {
      "apiName": "API1",
      "rate": 0.85,
      "convertedAmount": 85.0,
      "responseTimeMs": 245.7,
      "success": true
    },
    {
      "apiName": "API2", 
      "rate": 0.87,
      "convertedAmount": 87.0,
      "responseTimeMs": 312.1,
      "success": true
    }
  ],
  "successfulApis": 2,
  "totalApis": 3,
  "averageRate": 0.86,
  "successRate": 66.67,
  "totalProcessingTimeMs": 450.2,
  "processedAt": "2025-08-04T16:30:00Z"
}
```




## 🧪 Testing

```bash
./ExchangeRateComparison.test/generateReportCoverageTest.sh
```

### Health Check

```bash
curl http://localhost:5055/health
```

### Test directo de Mock APIs

```bash
# API1 (JSON)
curl -X POST http://localhost:5055/api1/exchange \
  -H "Content-Type: application/json" \
  -H "X-API-Key: demo-api-key-1" \
  -d '{"from": "USD", "to": "EUR", "value": 100}'

# API2 (XML)  
curl -X POST http://localhost:5055/api2/exchange \
  -H "Content-Type: application/xml" \
  -H "X-API-Key: demo-api-key-2" \
  -d '<XML><From>USD</From><To>EUR</To><Amount>100</Amount></XML>'

# API3 (JSON Anidado)
curl -X POST http://localhost:5055/api3/exchange \
  -H "Content-Type: application/json" \
  -H "X-API-Key: demo-api-key-3" \
  -d '{"exchange": {"sourceCurrency": "USD", "targetCurrency": "EUR", "quantity": 100}}'
```

## 🏗️ Arquitectura Técnica

### Tecnologías utilizadas:

- **.NET 8** - Framework principal
- **ASP.NET Core** - Web API
- **Polly** - Políticas de resilencia (retry, circuit breaker)
- **Swagger/OpenAPI** - Documentación
- **Docker** - Containerización
- **HttpClient** - Comunicación con APIs externas


