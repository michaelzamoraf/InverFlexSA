namespace APIInverFlexSA.Application.Exceptions;

/// -----------------------------------------------------------------------------------------------------------
/// <summary>
/// Indica que no existe una cotización vigente ni una cotización de respaldo utilizable.
/// </summary>
/// -----------------------------------------------------------------------------------------------------------
public sealed class MarketPriceUnavailableException(string message, Exception? innerException = null)
    : Exception(message, innerException)
{
}
