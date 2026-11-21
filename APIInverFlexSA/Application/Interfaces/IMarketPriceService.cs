namespace APIInverFlexSA.Application.Interfaces
{
    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Obtiene precios de mercado para instrumentos financieros.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    public interface IMarketPriceService
    {
        #region Métodos Públicos

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Obtiene el precio actual de un ticker.
        /// </summary>
        /// <param name="ticker">Ticker del instrumento.</param>
        /// <param name="ct">Cancelación.</param>
        /// <returns>Precio actual.</returns>
        /// -----------------------------------------------------------------------------------------------------------
        Task<decimal> GetCurrentPriceAsync(string ticker, CancellationToken ct = default);

        #endregion
    }
}
