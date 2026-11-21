using System.ComponentModel.DataAnnotations;

namespace APIInverFlexSA.Domain.Entities
{
    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Orden de compra o venta de un instrumento.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    public class Order
    {
        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Identificador de la orden.
        /// </summary>
        /// -----------------------------------------------------------------------------------------------------------
        public Guid Id { get; set; }

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Identificador del cliente.
        /// </summary>
        /// -----------------------------------------------------------------------------------------------------------
        public Guid ClientId { get; set; }

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Cliente propietario.
        /// </summary>
        /// -----------------------------------------------------------------------------------------------------------
        public User Client { get; set; } = null!;

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Identificador del instrumento.
        /// </summary>
        /// -----------------------------------------------------------------------------------------------------------
        public Guid InstrumentId { get; set; }

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Instrumento negociado.
        /// </summary>
        /// -----------------------------------------------------------------------------------------------------------
        public Instrument Instrument { get; set; } = null!;

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Tipo de operación.
        /// </summary>
        /// -----------------------------------------------------------------------------------------------------------
        public OrderType Type { get; set; }

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Cantidad solicitada.
        /// </summary>
        /// -----------------------------------------------------------------------------------------------------------
        public decimal Quantity { get; set; }

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Precio unitario de ejecución.
        /// </summary>
        /// -----------------------------------------------------------------------------------------------------------
        public decimal UnitPrice { get; set; }

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Importe total calculado como cantidad por precio unitario.
        /// </summary>
        /// -----------------------------------------------------------------------------------------------------------
        public decimal TotalAmount => Quantity * UnitPrice;

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Estado actual de la orden.
        /// </summary>
        /// -----------------------------------------------------------------------------------------------------------
        public OrderStatus Status { get; set; }

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Identificador del asesor que aprobó la orden.
        /// </summary>
        /// -----------------------------------------------------------------------------------------------------------
        public Guid? ApprovedByAdvisorId { get; set; }

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Fecha de creación en UTC.
        /// </summary>
        /// -----------------------------------------------------------------------------------------------------------
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>Fecha de ejecución en UTC.</summary>
        /// -----------------------------------------------------------------------------------------------------------
        public DateTime? ExecutedAt { get; set; }

        //Token de concurrencia optimista para evitar doble aprobación
        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Token utilizado para concurrencia optimista.
        /// </summary>
        /// -----------------------------------------------------------------------------------------------------------
        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>Aprueba una orden pendiente y registra al asesor responsable.</summary>
        /// <param name="advisorId">Identificador del asesor.</param>
        /// <exception cref="InvalidOperationException">Si la orden no está pendiente de aprobación.</exception>
        /// -----------------------------------------------------------------------------------------------------------
        public void Approve(Guid advisorId)
        {
            if (Status != OrderStatus.PendienteAprobacion)
                throw new InvalidOperationException("Solo se pueden aprobar órdenes en estado PendienteAprobacion.");

            Status = OrderStatus.Ejecutada;
            ApprovedByAdvisorId = advisorId;
            ExecutedAt = DateTime.UtcNow;
        }

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>Cancela una orden que todavía no se ha ejecutado.</summary>
        /// <exception cref="InvalidOperationException">Si la orden ya fue ejecutada o cancelada.</exception>
        /// -----------------------------------------------------------------------------------------------------------
        public void Cancel()
        {
            if (Status == OrderStatus.Ejecutada || Status == OrderStatus.Cancelada)
                throw new InvalidOperationException("No se puede cancelar una orden ya ejecutada o cancelada.");

            Status = OrderStatus.Cancelada;
        }
    }
}
