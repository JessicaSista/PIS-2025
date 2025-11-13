using Microsoft.AspNetCore.Mvc;
using OmniMonitor.Server.Context;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OmniMonitor.Shared.Dtos;


namespace OmniMonitor.Server.Controllers
{
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
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Id", Tipo = FilterValueType.Number });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Title", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "DatePublished", Tipo = FilterValueType.Date });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Summary", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Description", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Categories.Name", Tipo = FilterValueType.Enum });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "StartDate", Tipo = FilterValueType.Date });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "EndDate", Tipo = FilterValueType.Date });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Important", Tipo = FilterValueType.Boolean });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Zone.Name", Tipo = FilterValueType.Enum });
                            break;
                        case 1: // Eventos
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Id", Tipo = FilterValueType.Number });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Name", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Description", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Date", Tipo = FilterValueType.Date });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Type.Name", Tipo = FilterValueType.Enum });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "TypeId", Tipo = FilterValueType.Number });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Location", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "ApprovalState", Tipo = FilterValueType.Enum });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "ReviewedAt", Tipo = FilterValueType.Date });
                            break;
                    }
                    break;
                case "AM":
                    switch (entidadId)
                    {
                        case 2: // Asset
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Id", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Name", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "ExternalId", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Code", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Address", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "CreatedAt", Tipo = FilterValueType.Date });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "LifeTimeToDate", Tipo = FilterValueType.Number });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "TypeDto.Name", Tipo = FilterValueType.Enum });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "BundleDto.Name", Tipo = FilterValueType.Enum });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "BrandDto.Name", Tipo = FilterValueType.Enum });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "StateDto.Name", Tipo = FilterValueType.Enum });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "ResponsibleDto.Name", Tipo = FilterValueType.Enum });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Device.Name", Tipo = FilterValueType.Enum });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "ProviderDto.Name", Tipo = FilterValueType.Enum });
                            break;
                        case 1: // EventTaskInstance
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Id", Tipo = FilterValueType.Number });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "EventTaskDto.Id", Tipo = FilterValueType.Number });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "StartDate", Tipo = FilterValueType.Date });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "State", Tipo = FilterValueType.Enum });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Subject", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "FinalizedDate", Tipo = FilterValueType.Date });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "TakenBy.Name", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Critical", Tipo = FilterValueType.Boolean });
                            break;
                        case 3: // Stock
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Id", Tipo = FilterValueType.Number });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Name", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Quantity", Tipo = FilterValueType.Number });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Location", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Sku", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Minimum", Tipo = FilterValueType.Number });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Bundle.Name", Tipo = FilterValueType.Enum });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "BundleId", Tipo = FilterValueType.Number });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Supervisor.Name", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Categories.Name", Tipo = FilterValueType.Enum });
                            //faltarian categories y tambien los Dtos compuestos
                            break;
                    }
                    break;
                case "EM":
                    switch (entidadId)
                    {
                        case 1: // Alertas
                            resultado.Add(new PropiedadEntidadDto { Nombre = "AlertId", Tipo = FilterValueType.Number });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "AlertName", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "SourceId", Tipo = FilterValueType.Number });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "SourceAddress", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "AlertState", Tipo = FilterValueType.Enum });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "AlertCategory.Name", Tipo = FilterValueType.Enum });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "AlertData", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "CreatedAt", Tipo = FilterValueType.Date });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "ModifiedAt", Tipo = FilterValueType.Date });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "DeviceType", Tipo = FilterValueType.Number });
                            break;
                        case 2: // Eventos
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Id", Tipo = FilterValueType.Number });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Name", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Origin", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "DateTime", Tipo = FilterValueType.Date });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "CreationDate", Tipo = FilterValueType.Date });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "LastModification", Tipo = FilterValueType.Date });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "SourceType", Tipo = FilterValueType.Number });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "State", Tipo = FilterValueType.Enum });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Categories.Name", Tipo = FilterValueType.Enum });
                            break;
                        case 3: // Extensiones
                            resultado.Add(new PropiedadEntidadDto { Nombre = "ExtensionId", Tipo = FilterValueType.Number });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "ExtensionState", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "TakenByUsername", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "EventDateTime", Tipo = FilterValueType.Date });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "CreatedAt", Tipo = FilterValueType.Date });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "LastModification", Tipo = FilterValueType.Date });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "WorkZoneId", Tipo = FilterValueType.Number });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "WorkZoneName", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "EventId", Tipo = FilterValueType.Number });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "EventName", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Dangerous", Tipo = FilterValueType.Boolean });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Address", Tipo = FilterValueType.Enum });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Categories.Name", Tipo = FilterValueType.Enum });
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
                    if (entidadId == 2)
                        entidades = await _sondaUMService.GetAllNews(username);
                    else if (entidadId == 1)
                        entidades = await _sondaUMService.GetAllEvents(username);
                    else
                        entidadValida = false;
                    break;
                case "AM":
                    if (entidadId == 2)
                        entidades = await _sondaAMService.GetAssets(null, null, null, null, null, null, username);
                    else if (entidadId == 1)
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

            // DTO para la petición de filtrado
    // Usar FiltrarDatosRequest desde Shared.Dtos

    [HttpPost("FiltrarDatos")]
    public async Task<ActionResult<List<object>>> FiltrarDatos([FromBody] OmniMonitor.Shared.Dtos.FiltrarDatosRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return BadRequest("El parámetro 'token' es obligatorio.");

        string username = await _sondaAuthService.GetUserByTokenOMAsync(request.Token);
        if (string.IsNullOrWhiteSpace(username))
            return BadRequest("No se pudo obtener el usuario a partir del token.");

        IEnumerable<object> entidades = Enumerable.Empty<object>();
        bool moduloValido = true, entidadValida = true;

        switch (request.Modulo)
        {
            case "UM":
                if (request.EntidadId == 2)
                    entidades = await _sondaUMService.GetAllNews(username);
                else if (request.EntidadId == 1)
                    entidades = await _sondaUMService.GetAllEvents(username);
                else
                    entidadValida = false;
                break;
            case "AM":
                if (request.EntidadId == 2)
                    entidades = await _sondaAMService.GetAssets(null, null, null, null, null, null, username);
                else if (request.EntidadId == 1)
                    entidades = await _sondaAMService.GetEventTaskInstances(
                        "1900-11-01,3030-11-06", null, null, null, null, null, null, null, null, false, false, username);
                else if (request.EntidadId == 3)
                    entidades = await _sondaAMService.GetAllStock(null, null, null, null, null, username);
                else
                    entidadValida = false;
                break;
            case "EM":
                if (request.EntidadId == 1)
                    entidades = await _sondaEMService.GetAlerts(null, null, null, null, null, null, null, null, null, username);
                else if (request.EntidadId == 2)
                    entidades = await _sondaEMService.GetEvents(null, null, null, null, username);
                else if (request.EntidadId == 3)
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

        // DEBUG: Mostrar filtros recibidos
        Console.WriteLine($"[DEBUG] Filtros recibidos: {request.Filtros.Count}");
        foreach (var filtro in request.Filtros)
        {
            Console.WriteLine($"[DEBUG] Filtro: AttributeName={filtro.AttributeName}, Type={filtro.Type}, ValueType={filtro.ValueType}, Condition={filtro.Condition}");
        }

        // DEBUG: Mostrar propiedades de los objetos antes de filtrar
        int idx = 0;
        foreach (var entidad in entidades)
        {
            Console.WriteLine($"[DEBUG] Entidad #{idx}: Tipo={entidad.GetType().Name}");
            foreach (var prop in entidad.GetType().GetProperties())
            {
                var val = prop.GetValue(entidad);
                Console.WriteLine($"    Propiedad: {prop.Name} = {val}");
            }
            idx++;
        }

        // Filtrar usando ApiDataService.FilterObjects
        var filtrados = ApiDataService.StaticFilterObjects(entidades, request.Filtros);
        Console.WriteLine($"[DEBUG] Total objetos filtrados: {filtrados.Count}");
        foreach (var obj in filtrados)
        {
            Console.WriteLine($"[DEBUG] Filtrado: Tipo={obj.GetType().Name}");
        }
        return Ok(filtrados);
    }

}}