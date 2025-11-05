using Microsoft.AspNetCore.Mvc;
using OmniMonitor.Server.Context;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmniMonitor.Server.Controllers
{
    public enum TipoPropiedad
    {
        String,
        Numero,
        Fecha,
        Enumerado
    }

    public class PropiedadEntidadDto
    {
        public string Nombre { get; set; } = string.Empty;
        public TipoPropiedad Tipo { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class DatasetFilterController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DatasetFilterController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("FiltrarPorModuloYEntidad")]
        public async Task<ActionResult<List<PropiedadEntidadDto>>> FiltrarPorModuloYEntidad(string modulo, int entidadId)
        {
            var resultado = new List<PropiedadEntidadDto>();

            switch (modulo)
            {
                case "UM":
                    switch (entidadId)
                    {
                        case 1: // Noticias
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Title", Tipo = TipoPropiedad.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "DatePublished", Tipo = TipoPropiedad.Fecha });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Summary", Tipo = TipoPropiedad.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Description", Tipo = TipoPropiedad.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Categories", Tipo = TipoPropiedad.Enumerado });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "StartDate", Tipo = TipoPropiedad.Fecha });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "EndDate", Tipo = TipoPropiedad.Fecha });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Important", Tipo = TipoPropiedad.Enumerado });
                            break;
                        case 2: // Eventos
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Name", Tipo = TipoPropiedad.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Description", Tipo = TipoPropiedad.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Date", Tipo = TipoPropiedad.Fecha });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Type", Tipo = TipoPropiedad.Enumerado });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Location", Tipo = TipoPropiedad.String });
                            break;
                    }
                    break;
                case "AM":
                    switch (entidadId)
                    {
                        case 1: // Asset
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Id", Tipo = TipoPropiedad.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Name", Tipo = TipoPropiedad.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "ExternalId", Tipo = TipoPropiedad.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Code", Tipo = TipoPropiedad.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Address", Tipo = TipoPropiedad.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "CreatedAt", Tipo = TipoPropiedad.Fecha });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "LifeTimeToDate", Tipo = TipoPropiedad.Numero });
                            break;
                        case 2: // EventTaskInstance
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Id", Tipo = TipoPropiedad.Numero });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "StartDate", Tipo = TipoPropiedad.Fecha });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "State", Tipo = TipoPropiedad.Enumerado });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Subject", Tipo = TipoPropiedad.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "FinalizedDate", Tipo = TipoPropiedad.Fecha });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Critical", Tipo = TipoPropiedad.Enumerado });
                            break;
                        case 3: // Stock
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Id", Tipo = TipoPropiedad.Numero });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Name", Tipo = TipoPropiedad.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Quantity", Tipo = TipoPropiedad.Numero });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Location", Tipo = TipoPropiedad.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Sku", Tipo = TipoPropiedad.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Minimum", Tipo = TipoPropiedad.Numero });
                            //faltarian categories y tambien los Dtos compuestos
                            break;
                    }
                    break;
                case "EM":
                    switch (entidadId)
                    {
                        case 1: // Alertas
                            resultado.Add(new PropiedadEntidadDto { Nombre = "AlertId", Tipo = TipoPropiedad.Numero });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "AlertName", Tipo = TipoPropiedad.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "SourceId", Tipo = TipoPropiedad.Numero });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Location", Tipo = TipoPropiedad.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "SourceAddress", Tipo = TipoPropiedad.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "AlertState", Tipo = TipoPropiedad.Enumerado });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "AlertCategory", Tipo = TipoPropiedad.Enumerado });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "AlertData", Tipo = TipoPropiedad.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "CreatedAt", Tipo = TipoPropiedad.Fecha });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "ModifiedAt", Tipo = TipoPropiedad.Fecha });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "DeviceType", Tipo = TipoPropiedad.Numero });
                            break;
                        case 2: // Eventos
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Id", Tipo = TipoPropiedad.Numero });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Name", Tipo = TipoPropiedad.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Origin", Tipo = TipoPropiedad.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "DateTime", Tipo = TipoPropiedad.Fecha });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "CreationDate", Tipo = TipoPropiedad.Fecha });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "LastModification", Tipo = TipoPropiedad.Fecha });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "SourceType", Tipo = TipoPropiedad.Numero });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "State", Tipo = TipoPropiedad.String });
                            break;
                        case 3: // Extensiones
                            resultado.Add(new PropiedadEntidadDto { Nombre = "ExtensionId", Tipo = TipoPropiedad.Numero });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "ExtensionState", Tipo = TipoPropiedad.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "TakenByUsername", Tipo = TipoPropiedad.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "EventDateTime", Tipo = TipoPropiedad.Fecha });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "CreatedAt", Tipo = TipoPropiedad.Fecha });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "LastModification", Tipo = TipoPropiedad.Fecha });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "WorkZoneId", Tipo = TipoPropiedad.Numero });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "WorkZoneName", Tipo = TipoPropiedad.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "EventId", Tipo = TipoPropiedad.Numero });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "EventName", Tipo = TipoPropiedad.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Dangerous", Tipo = TipoPropiedad.Enumerado });
                            break;
                    }
                    break;
            }

            return Ok(resultado);
        }
    }
}
