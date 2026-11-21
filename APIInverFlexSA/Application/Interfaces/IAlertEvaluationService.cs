using APIInverFlexSA.Domain.Entities;

namespace APIInverFlexSA.Application.Interfaces;

/// -----------------------------------------------------------------------------------------------------------
/// <summary>
/// Evalúa si el precio actual activa una alerta configurada.
/// </summary>
/// -----------------------------------------------------------------------------------------------------------
public interface IAlertEvaluationService
{
    #region Métodos Públicos

    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Evalúa y marca la alerta cuando se cumple su condición.
    /// </summary>
    /// <param name="alert">Alerta que se evaluará.</param>
    /// <param name="currentPrice">Precio actual del instrumento.</param>
    /// <returns><see langword="true"/> si la alerta se activa.</returns>
    /// -----------------------------------------------------------------------------------------------------------
    bool Evaluate(PriceAlert alert, decimal currentPrice);

    #endregion
}
