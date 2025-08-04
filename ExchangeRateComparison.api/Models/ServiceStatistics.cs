// Models/ServiceStatistics.cs


using System;
using System.Collections.Generic;

namespace ExchangeRateComparison.api.Models
{
    /// <summary>
    /// Estadísticas del servicio de tasas de cambio (Modelo de Dominio)
    /// </summary>
    public class ServiceStatistics
    {
        /// <summary>
        /// Total de solicitudes procesadas
        /// </summary>
        public long TotalRequests { get; set; }
    
        /// <summary>
        /// Solicitudes exitosas
        /// </summary>
        public long SuccessfulRequests { get; set; }
    
        /// <summary>
        /// Solicitudes fallidas
        /// </summary>
        public long FailedRequests { get; set; }
    
        /// <summary>
        /// Tiempo promedio de respuesta en milisegundos
        /// </summary>
        public double AverageResponseTimeMs { get; set; }
    
        /// <summary>
        /// Estadísticas por API
        /// </summary>
        public Dictionary<string, ApiStatistics> ApiStats { get; set; } = new();
    
        /// <summary>
        /// Tiempo de actividad del servicio
        /// </summary>
        public TimeSpan Uptime { get; set; }
    
        /// <summary>
        /// Última vez que se reiniciaron las estadísticas
        /// </summary>
        public DateTime LastReset { get; set; }
    
        /// <summary>
        /// Tasa de éxito general (calculada)
        /// </summary>
        public double SuccessRate => TotalRequests > 0 ? (double)SuccessfulRequests / TotalRequests * 100 : 0;
    }
}
