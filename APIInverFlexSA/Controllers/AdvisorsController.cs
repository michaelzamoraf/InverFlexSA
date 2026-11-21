using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace APIInverFlexSA.Controllers
{
    /// -----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Endpoints de consulta para asesores financieros.
    /// </summary>
    /// -----------------------------------------------------------------------------------------------------------
    [Route("api/v{version:apiVersion}/[controller]")]
    //[Produces("application/json")]
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "asesor")]
    public class AdvisorsController : ControllerBase
    {
        #region Métodos Públicos

        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Genera un informe de rendimiento para el periodo indicado.
        /// </summary>
        /// <param name="from">Inicio del periodo.</param>
        /// <param name="to">Fin del periodo.</param>
        /// <param name="format">Formato solicitado.</param>
        /// <returns>Datos del informe generado.</returns>
        /// -----------------------------------------------------------------------------------------------------------
        [HttpGet("reports/performance")]
        public IActionResult GetClientsPerformanceReport([FromQuery] DateTime from, [FromQuery] DateTime to, [FromQuery] string format = "json")
        {
            //Aquí se invocaría la Query de reporte consolidado de clientes asignados al asesor
            return Ok(new { Message = "Reporte generado", From = from, To = to, Format = format });
        }

        #endregion
    }
}
