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
        Enumerado,
        Boolean
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
    private readonly ISondaUMService _sondaUMService;
    private readonly ISondaAMService _sondaAMService;
    private readonly ISondaEMService _sondaEMService;
    private readonly ISondaAuthService _sondaAuthService;


        public DatasetFilterController(ApplicationDbContext context, ISondaUMService sondaUMService, ISondaAMService sondaAMService, ISondaEMService sondaEMService, ISondaAuthService sondaAuthService)
        {
            _context = context;
            _sondaUMService = sondaUMService;
            _sondaAMService = sondaAMService;
            _sondaEMService = sondaEMService;
            _sondaAuthService = sondaAuthService;
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
                        case 2: // Noticias
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Id", Tipo = TipoPropiedad.Numero });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Title", Tipo = TipoPropiedad.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "DatePublished", Tipo = TipoPropiedad.Fecha });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Summary", Tipo = TipoPropiedad.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Description", Tipo = TipoPropiedad.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Categories.Name", Tipo = TipoPropiedad.Enumerado });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "StartDate", Tipo = TipoPropiedad.Fecha });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "EndDate", Tipo = TipoPropiedad.Fecha });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Important", Tipo = TipoPropiedad.Boolean });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Zone", Tipo = TipoPropiedad.Enumerado });
                            break;
                        case 1: // Eventos
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Id", Tipo = TipoPropiedad.Numero });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Name", Tipo = TipoPropiedad.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Description", Tipo = TipoPropiedad.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Date", Tipo = TipoPropiedad.Fecha });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Type.Name", Tipo = TipoPropiedad.Enumerado });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "TypeId", Tipo = TipoPropiedad.Numero });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Location", Tipo = TipoPropiedad.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "ApprovalState", Tipo = TipoPropiedad.Enumerado });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "ReviewedAt", Tipo = TipoPropiedad.Fecha });
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
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Critical", Tipo = TipoPropiedad.Boolean });
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
                            resultado.Add(new PropiedadEntidadDto { Nombre = "SourceAddress", Tipo = TipoPropiedad.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "AlertState", Tipo = TipoPropiedad.Enumerado });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "AlertCategory.Name", Tipo = TipoPropiedad.Enumerado });
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
                            resultado.Add(new PropiedadEntidadDto { Nombre = "State", Tipo = TipoPropiedad.Enumerado });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Categories.Name", Tipo = TipoPropiedad.Enumerado });
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
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Dangerous", Tipo = TipoPropiedad.Boolean });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Address", Tipo = TipoPropiedad.Enumerado });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Categories.Name", Tipo = TipoPropiedad.Enumerado });
                            break;
                    }
                    break;
            }

            return Ok(resultado);
        }

    
        [HttpGet("GetAtributoValores")]
        public async Task<ActionResult<List<string>>> GetAtributoValores(string modulo, int entidadId, string atributo, string token)
        {
            // DEBUG: Loguear los datos crudos de entidades para EM eventos
            
            // Get token from query
            
            if (string.IsNullOrWhiteSpace(token))
                return BadRequest("El parámetro 'token' es obligatorio.");

            string username = await _sondaAuthService.GetUserByTokenOMAsync(token);
            if (string.IsNullOrWhiteSpace(username))
                return BadRequest("No se pudo obtener el usuario a partir del token.");

            List<object> valores = new();
            IEnumerable<object> entidades = Enumerable.Empty<object>();
            bool moduloValido = true, entidadValida = true;

            switch (modulo)
            {
                case "UM":
                    if (entidadId == 1)
                        entidades = await _sondaUMService.GetAllNews(username);
                    else if (entidadId == 2)
                        entidades = await _sondaUMService.GetAllEvents(username);
                    else
                        entidadValida = false;
                    break;
                case "AM":
                    if (entidadId == 1)
                        entidades = await _sondaAMService.GetAssets(null, null, null, null, null, null, username);
                    else if (entidadId == 2)
                        entidades = await _sondaAMService.GetEventTaskInstances(
                            "1900-11-01,3030-11-06", // dates
                            null, // page
                            null, // queryString
                            null, // bundleId
                            null, // state
                            null, // sort
                            null, // taskTypeId
                            null, // groupId
                            null, // pageSize
                            false, // tasksAssignedToMe
                            false, // tasksPendingApproval
                            username // username
                        );
                    else if (entidadId == 3)
                        entidades = await _sondaAMService.GetAllStock(null, null, null, null, null, username);
                    else
                        entidadValida = false;
                    break;
                case "EM":
                    if (entidadId == 1)
                        entidades = await _sondaEMService.GetAlerts(null, null, null, null, null, null, null, null, null, username);
                    else if (entidadId == 2)
                        entidades = await _sondaEMService.GetEvents(null, null, null, null, username);
                    else if (entidadId == 3)
                        entidades = await _sondaEMService.GetExtensions(null, null, null, null, null, null, null, null, null, username);
                    else
                        entidadValida = false;
                    break;
                default:
                    moduloValido = false;
                    break;
            }

            if (!moduloValido)
                return BadRequest("Módulo no definido");
            if (!entidadValida)
                return BadRequest("Entidad no definida para el módulo seleccionado");


            if (modulo == "EM" && entidadId == 2)
            {
                foreach (var entidad in entidades)
                {
                    var categoriasProp = entidad.GetType().GetProperty("Categories");
                    var categorias = categoriasProp?.GetValue(entidad) as IEnumerable<object>;
                    if (categorias != null)
                    {
                        Console.WriteLine($"Evento: {entidad.GetType().GetProperty("Id")?.GetValue(entidad)}");
                        foreach (var cat in categorias)
                        {
                            var id = cat.GetType().GetProperty("Id")?.GetValue(cat);
                            var name = cat.GetType().GetProperty("Name")?.GetValue(cat);
                            Console.WriteLine($"  Categoria: Id={id}, Name={name}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Evento: {entidad.GetType().GetProperty("Id")?.GetValue(entidad)} no tiene categorias");
                    }
                }
            }

            var partes = atributo.Split('.');
            foreach (var entidad in entidades)
            {
                void ExtraerValores(object? actual, int parteIdx)
                {
                    if (actual == null || parteIdx >= partes.Length) return;
                    var prop = actual.GetType().GetProperty(partes[parteIdx]);
                    var valor = prop?.GetValue(actual);

                    if (valor is IEnumerable<object> coleccion)
                    {
                        // Si es la última parte, agrego cada elemento
                        if (parteIdx == partes.Length - 1)
                        {
                            foreach (var item in coleccion)
                                if (item != null)
                                    valores.Add(item);
                        }
                        else
                        {
                            // Para cada elemento, sigo el path restante
                            foreach (var item in coleccion)
                                ExtraerValores(item, parteIdx + 1);
                        }
                    }
                    else
                    {
                        // Si es la última parte y no es colección, agrego el valor
                        if (parteIdx == partes.Length - 1 && valor != null)
                            valores.Add(valor);
                        else if (valor != null)
                            ExtraerValores(valor, parteIdx + 1);
                    }
                }
                ExtraerValores(entidad, 0);
            }

            return Ok(valores.Distinct().ToList());
        }
    }
}
