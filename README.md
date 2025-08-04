# 🏦 Exchange Rate Comparison API

Una API  para comparar tasas de cambio de múltiples API y obtener la mejor oferta para clientes.

## 📋 Características

- **🔄 Comparación en tiempo real** de múltiples APIs de tasas de cambio
- **🚀 Procesamiento paralelo** para máximo rendimiento
- **🔐 Autenticación dinámica** por headers para cada API
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
# http://localhost:5055
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
# https://localhost:5055 (o el puerto mostrado en consola)
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
POST /api/exchangerate/best-rate
Headers:
  X-API1-Key: clave-incorrecta
  X-API2-Key: invalid-key  
  X-API3-Key: wrong-key
  Content-Type: application/json

{
  "sourceCurrency": "USD",
  "targetCurrency": "EUR",
  "amount": 100
}
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

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| `POST` | `/api/exchangerate/best-rate` | Obtiene la mejor tasa de cambio |
| `GET` | `/api/exchangerate/health` | Estado de salud de APIs |
| `GET` | `/api/exchangerate/statistics` | Estadísticas de uso |
| `GET` | `/api/exchangerate/currencies` | Divisas soportadas |

### 💱 Ejemplo de Request

```json
POST /api/exchangerate/best-rate
Headers:
  X-API1-Key: demo-api-key-1
  X-API2-Key: demo-api-key-2  
  X-API3-Key: demo-api-key-3
  Content-Type: application/json

{
  "sourceCurrency": "USD",
  "targetCurrency": "EUR",
  "amount": 100
}
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

./EchangeRateComparison.test/generateReportCoverageTest.sh


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

## 📊 Monitoreo

### Estadísticas disponibles:

- **Total de requests** procesados
- **Tasa de éxito** por API
- **Tiempo promedio de respuesta**
- **Conteo de mejores ofertas** por API
- **Uptime** del servicio

### Health checks:

- ✅ Estado del servicio principal
- ✅ Conectividad con APIs externas
- ✅ Tiempo de respuesta promedio
- ✅ Última respuesta exitosa

## 🏗️ Arquitectura Técnica

### Tecnologías utilizadas:

- **.NET 8** - Framework principal
- **ASP.NET Core** - Web API
- **Polly** - Políticas de resilencia (retry, circuit breaker)
- **Swagger/OpenAPI** - Documentación
- **Docker** - Containerización
- **Serilog** - Logging estructurado

### Patrones implementados:

- **🎯 Strategy Pattern** - Clientes intercambiables de APIs
- **🔄 Circuit Breaker** - Protección contra fallos en cascada  
- **⚡ Retry Pattern** - Reintentos automáticos con backoff
- **📊 Observer Pattern** - Estadísticas y monitoreo
- **🧩 Dependency Injection** - Inversión de control

## 🐛 Troubleshooting

### Problema: "Request timeout"
```bash
# Verificar URLs en configuración
curl http://localhost:8080/api1/health

# Aumentar timeout en docker-compose.yml
ExchangeRateApis__Api1__TimeoutSeconds=30
```

### Problema: "Authentication failed"
```bash
# Verificar API keys en headers
# API1: demo-api-key-1, test-api-key-1, valid-key-1
# API2: demo-api-key-2, test-api-key-2, valid-key-2  
# API3: demo-api-key-3, test-api-key-3, valid-key-3
```

### Problema: "All APIs failed"
```bash
# Verificar health check
curl http://localhost:8080/api/exchangerate/health

# Ver logs del contenedor
docker logs exchange-rate-api
```

## 🤝 Contribuir

1. Fork el proyecto
2. Crea tu feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit tus cambios (`git commit -m 'Add some AmazingFeature'`)
4. Push al branch (`git push origin feature/AmazingFeature`)
5. Abre un Pull Request

## 📄 Licencia

Este proyecto está bajo la licencia MIT. Ver `LICENSE` para más detalles.

## 👥 Contacto

**Exchange Rate Team**  
📧 Email: exchangerate@banreservas.com

---

⭐ **¡Dale una estrella si este proyecto te ayudó!** ⭐