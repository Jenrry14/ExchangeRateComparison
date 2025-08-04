// Services/Interfaces/IExchangeRateClient.cs

using System.Threading;
using System.Threading.Tasks;
using ExchangeRateComparison.api.Models.ExchangeRate;

namespace ExchangeRateComparison.api.Services.Interfaces;

/// <summary>
/// Interfaz para clientes de APIs externas de tasas de cambio
/// </summary>
public interface IExchangeRateClient
{
    /// <summary>
    /// Nombre identificador de la API
    /// </summary>
    string ApiName { get; }
    
    /// <summary>
    /// Indica si el cliente está habilitado
    /// </summary>
    bool IsEnabled { get; }
    
    /// <summary>
    /// Obtiene la tasa de cambio de la API externa
    /// </summary>
    /// <param name="request">Solicitud con los datos de conversión</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Respuesta con la tasa de cambio o error</returns>
    Task<ExchangeResponse> GetExchangeRateAsync(ExchangeRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Verifica si la API está disponible
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>True si la API responde correctamente</returns>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}