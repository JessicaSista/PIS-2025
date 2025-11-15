using Microsoft.EntityFrameworkCore;
using OmniMonitor.Server.Context;
using OmniMonitor.Server.Services;
using System.Dynamic;
using System.Linq.Dynamic.Core;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using OmniMonitor.Shared.Dtos;

public interface IJoinConfigurationService
{
    Task<CrossModuleJoin> CreateJoinAsync(CreateJoinRequestDto request, string username);
    Task<List<dynamic>> ExecuteJoinAsync(int joinId);
    Task<List<dynamic>> ExecuteJoinWithFiltersAsync(int joinId, JoinFiltersConfig? filters = null);
    Task<List<CrossModuleJoinDto>> GetJoinsByUsernameAsync(string username);
}

public class JoinConfigurationService : IJoinConfigurationService
{
    private readonly ApplicationDbContext _context;
    private readonly IApiDataService _apiDataService;

    public JoinConfigurationService(ApplicationDbContext context, IApiDataService apiDataService)
    {
        _context = context;
        _apiDataService = apiDataService;
    }

    public async Task<CrossModuleJoin> CreateJoinAsync(CreateJoinRequestDto request, string username)
    {
        var leftOperand = new JoinOperand
        {
            ModuleType = request.LeftOperand.ModuleType,
            DatasetId = request.LeftOperand.DatasetId,
            EntityName = request.LeftOperand.EntityName,
            JoinPropertyName = request.LeftOperand.JoinPropertyName
        };

        var rightOperand = new JoinOperand
        {
            ModuleType = request.RightOperand.ModuleType,
            DatasetId = request.RightOperand.DatasetId,
            EntityName = request.RightOperand.EntityName,
            JoinPropertyName = request.RightOperand.JoinPropertyName
        };

        // 2. Add operands to the context
        _context.JoinOperands.Add(leftOperand);
        _context.JoinOperands.Add(rightOperand);

        // 3. Save changes to get the IDs of the new operands
        await _context.SaveChangesAsync();

        // 4. Create the main CrossModuleJoin entity
        var joinDefinition = new CrossModuleJoin
        {
            Name = request.Name,
            Description = request.Description,
            Username = username,
            JoinType = request.JoinType,
            LeftOperandId = leftOperand.Id,
            RightOperandId = rightOperand.Id
        };

        _context.CrossModuleJoins.Add(joinDefinition);

        // 5. Save the final join configuration
        await _context.SaveChangesAsync();

        return joinDefinition;
    }

    string BuildSelector(string key, string type)
    {
        switch (type.ToLower())
        {
            case "int":
            case "integer":
                return $"int(it.{key})";

            case "guid":
                return $"Guid(it.{key})";

            case "string":
            default:
                return $"it.{key}";
        }
    }

    public async Task<List<dynamic>> ExecuteJoinAsync(int joinId)
    {
        // 1. Load the Join "Recipe" from the database
        var joinConfig = await _context.CrossModuleJoins
            .Include(j => j.LeftOperand)
            .Include(j => j.RightOperand)
            .FirstOrDefaultAsync(j => j.Id == joinId);

        if (joinConfig == null)
        {
            throw new KeyNotFoundException($"Join configuration with ID {joinId} not found.");
        }

        var leftData = await _apiDataService.GetDataForOperand(joinConfig.LeftOperand, joinConfig.Username);
        var rightData = await _apiDataService.GetDataForOperand(joinConfig.RightOperand, joinConfig.Username);

        if (leftData == null || rightData == null)
        {
            return new List<dynamic>(); // Return empty if any data source fails
        }

        // 3. Perform the in-memory join using Dynamic LINQ
        string leftJoinKey = joinConfig.LeftOperand.JoinPropertyName;
        string leftJoinType = GetPropertyTypeDynamically(leftData, leftJoinKey);
        //string leftJoinType = "string";

        string rightJoinKey = joinConfig.RightOperand.JoinPropertyName;
        string rightJoinType = GetPropertyTypeDynamically(rightData, rightJoinKey);
        //string rightJoinType = "string";

        if ((joinConfig.JoinType == JoinType.Inner || joinConfig.JoinType == JoinType.LeftOuter) && (leftData == null || !leftData.AsQueryable().Any()))
            return new List<dynamic>();
        if ((joinConfig.JoinType == JoinType.Inner || joinConfig.JoinType == JoinType.RightOuter) && (rightData == null || !rightData.AsQueryable().Any()))
            return new List<dynamic>();

        if (leftJoinType != rightJoinType)
        {
            // Lanzamos una excepción con un mensaje claro que le dirá al usuario exactamente qué está mal.
            throw new InvalidOperationException(
                $"Incompatibilidad de tipos para el Join. La propiedad izquierda '{leftJoinKey}' es de tipo '{leftJoinType}', " +
                $"pero la propiedad derecha '{rightJoinKey}' es de tipo '{rightJoinType}'. " +
                "Por favor, asegúrate de que las propiedades en las que haces el Join tengan tipos de datos compatibles."
            );
        }

        var processedLeftQuery = leftData.AsQueryable().Select($"new(it as Data, {BuildSelector(leftJoinKey, leftJoinType)} as JoinKey)");
        var processedRightQuery = rightData.AsQueryable().Select($"new(it as Data, {BuildSelector(rightJoinKey, rightJoinType)} as JoinKey)");
        List<dynamic> nestedResults;

        var leftList = processedLeftQuery.ToDynamicList();
        var rightList = processedRightQuery.ToDynamicList();

        switch (joinConfig.JoinType)
        {
            case JoinType.Inner:
                nestedResults = leftList.Join(
                    rightList,
                    outer => ((dynamic)outer).JoinKey,
                    inner => ((dynamic)inner).JoinKey,
                    (outer, inner) => (dynamic)new { Left = ((dynamic)outer).Data, Right = ((dynamic)inner).Data }
                ).ToList();
                break;

            case JoinType.LeftOuter:
                nestedResults = PerformLeftJoin(leftList, rightList);
                break;

            case JoinType.RightOuter:
                var rightJoinResults = PerformLeftJoin(rightList, leftList);
                nestedResults = rightJoinResults.Select(r =>
                {
                    var leftValue = r.GetType().GetProperty("Right").GetValue(r, null);
                    var rightValue = r.GetType().GetProperty("Left").GetValue(r, null);
                    return (dynamic)new { Left = leftValue, Right = rightValue };
                }).ToList();
                break;

            default:
                throw new NotSupportedException($"El tipo de Join '{joinConfig.JoinType}' no está soportado.");
        }

        return FlattenJoinResults(nestedResults, joinConfig);
    }

    public async Task<List<dynamic>> ExecuteJoinWithFiltersAsync(int joinId, JoinFiltersConfig? filters = null)
    {
        // 1. Load the Join "Recipe" from the database
        var joinConfig = await _context.CrossModuleJoins
            .Include(j => j.LeftOperand)
            .Include(j => j.RightOperand)
            .FirstOrDefaultAsync(j => j.Id == joinId);

        if (joinConfig == null)
        {
            throw new KeyNotFoundException($"Join configuration with ID {joinId} not found.");
        }

        // 2. Obtener datos y aplicar filtros si se proporcionan
        var leftData = await _apiDataService.GetDataForOperand(joinConfig.LeftOperand, joinConfig.Username);
        
        // Aplicar filtros al operando izquierdo si existen
        if (filters?.LeftOperandFilters?.Filters != null && filters.LeftOperandFilters.Filters.Any())
        {
            foreach (var f in filters.LeftOperandFilters.Filters)
            {
            }
            leftData = ApiDataService.StaticFilterObjects(leftData, filters.LeftOperandFilters.Filters);
        }

        var rightData = await _apiDataService.GetDataForOperand(joinConfig.RightOperand, joinConfig.Username);
        
        // Aplicar filtros al operando derecho si existen
        if (filters?.RightOperandFilters?.Filters != null && filters.RightOperandFilters.Filters.Any())
        {
            foreach (var f in filters.RightOperandFilters.Filters)
            {
            }
            rightData = ApiDataService.StaticFilterObjects(rightData, filters.RightOperandFilters.Filters);
        }

        if (leftData == null || rightData == null)
        {
            return new List<dynamic>(); // Return empty if any data source fails
        }

        // 3. Perform the in-memory join using Dynamic LINQ
        string leftJoinKey = joinConfig.LeftOperand.JoinPropertyName;
        string leftJoinType = GetPropertyTypeDynamically(leftData, leftJoinKey);

        string rightJoinKey = joinConfig.RightOperand.JoinPropertyName;
        string rightJoinType = GetPropertyTypeDynamically(rightData, rightJoinKey);

        if ((joinConfig.JoinType == JoinType.Inner || joinConfig.JoinType == JoinType.LeftOuter) && (leftData == null || !leftData.AsQueryable().Any()))
            return new List<dynamic>();
        if ((joinConfig.JoinType == JoinType.Inner || joinConfig.JoinType == JoinType.RightOuter) && (rightData == null || !rightData.AsQueryable().Any()))
            return new List<dynamic>();

        if (leftJoinType != rightJoinType)
        {
            throw new InvalidOperationException(
                $"Incompatibilidad de tipos para el Join. La propiedad izquierda '{leftJoinKey}' es de tipo '{leftJoinType}', " +
                $"pero la propiedad derecha '{rightJoinKey}' es de tipo '{rightJoinType}'. " +
                "Por favor, asegÃºrate de que las propiedades en las que haces el Join tengan tipos de datos compatibles."
            );
        }

        var processedLeftQuery = leftData.AsQueryable().Select($"new(it as Data, {BuildSelector(leftJoinKey, leftJoinType)} as JoinKey)");
        var processedRightQuery = rightData.AsQueryable().Select($"new(it as Data, {BuildSelector(rightJoinKey, rightJoinType)} as JoinKey)");

        List<dynamic> nestedResults;

        var leftList = processedLeftQuery.ToDynamicList();
        var rightList = processedRightQuery.ToDynamicList();

        switch (joinConfig.JoinType)
        {
            case JoinType.Inner:
                nestedResults = leftList.Join(
                    rightList,
                    outer => ((dynamic)outer).JoinKey,
                    inner => ((dynamic)inner).JoinKey,
                    (outer, inner) => (dynamic)new { Left = ((dynamic)outer).Data, Right = ((dynamic)inner).Data }
                ).ToList();
                break;

            case JoinType.LeftOuter:
                nestedResults = PerformLeftJoin(leftList, rightList);
                break;

            case JoinType.RightOuter:
                var rightJoinResults = PerformLeftJoin(rightList, leftList);
                nestedResults = rightJoinResults.Select(r =>
                {
                    var leftValue = r.GetType().GetProperty("Right").GetValue(r, null);
                    var rightValue = r.GetType().GetProperty("Left").GetValue(r, null);
                    return (dynamic)new { Left = leftValue, Right = rightValue };
                }).ToList();
                break;

            default:
                throw new NotSupportedException($"El tipo de Join '{joinConfig.JoinType}' no estÃ¡ soportado.");
        }

        var finalResults = FlattenJoinResults(nestedResults, joinConfig);
        return finalResults;
    }

    public async Task<List<CrossModuleJoinDto>> GetJoinsByUsernameAsync(string username)
    {
        var joins = await _context.CrossModuleJoins
        .AsNoTracking()
        .Where(j => j.Username == username)
        .Include(j => j.LeftOperand)
        .Include(j => j.RightOperand)
        .Select(j => new CrossModuleJoinDto
        {
            Id = j.Id,
            Name = j.Name,
            Description = j.Description,
            JoinType = j.JoinType,
            LeftOperand = new JoinOperandDto
            {
                ModuleType = j.LeftOperand.ModuleType,
                DatasetId = j.LeftOperand.DatasetId,
                EntityName = j.LeftOperand.EntityName,
                JoinPropertyName = j.LeftOperand.JoinPropertyName
            },
            RightOperand = new JoinOperandDto
            {
                ModuleType = j.RightOperand.ModuleType,
                DatasetId = j.RightOperand.DatasetId,
                EntityName = j.RightOperand.EntityName,
                JoinPropertyName = j.RightOperand.JoinPropertyName
            }
        })
        .ToListAsync();

        return joins;
    }


    private List<dynamic> FlattenJoinResults(List<dynamic> nestedResults, CrossModuleJoin joinConfig)
    {
        if (nestedResults == null || !nestedResults.Any())
        {
            return new List<dynamic>();
        }

        var flattenedList = new List<dynamic>();
        string leftPrefix = joinConfig.LeftOperand.EntityName.ToString();
        string rightPrefix = joinConfig.RightOperand.EntityName.ToString();

        foreach (var result in nestedResults)
        {
            var expando = new ExpandoObject() as IDictionary<string, object>;

            var leftItem = result.GetType().GetProperty("Left").GetValue(result, null);
            if (leftItem != null)
            {
                foreach (PropertyInfo prop in leftItem.GetType().GetProperties())
                {
                    expando[$"{leftPrefix}_{prop.Name}"] = prop.GetValue(leftItem, null);
                }
            }

            var rightItem = result.GetType().GetProperty("Right").GetValue(result, null);
            if (rightItem != null)
            {
                string currentRightPrefix = rightPrefix;
                // Si el prefijo es el mismo, añade un sufijo para evitar colisiones de nombres de columna
                if (leftPrefix == rightPrefix)
                {
                    currentRightPrefix = $"{rightPrefix}_Right";
                }

                foreach (PropertyInfo prop in rightItem.GetType().GetProperties())
                {
                    expando[$"{currentRightPrefix}_{prop.Name}"] = prop.GetValue(rightItem, null);
                }
            }

            flattenedList.Add(expando);
        }

        return flattenedList;
    }

    private string GetPropertyTypeDynamically(IEnumerable<dynamic> data, string propertyName)
    {
        // 1. Tomamos el primer elemento de la lista para inspeccionarlo.
        var firstItem = data.AsQueryable().FirstOrDefault();

        // 2. CASO CRÍTICO: Si la lista de datos está vacía, no podemos determinar el tipo.
        // En este caso, debemos retornar un valor por defecto o lanzar un error.
        // Retornar "string" (sin casting) es la opción más segura.
        if (firstItem == null)
        {
            return "string";
        }

        // 3. Usamos reflexión para obtener la información de la propiedad.
        // Hacemos un cast a 'object' para poder usar GetType().
        PropertyInfo propertyInfo = (firstItem as object).GetType().GetProperty(propertyName);

        if (propertyInfo == null)
        {
            // Si la propiedad no existe en el objeto, lanzamos un error claro.
            throw new InvalidOperationException($"La propiedad '{propertyName}' no fue encontrada en el tipo de dato '{firstItem.GetType().Name}'.");
        }

        // 4. Obtenemos el tipo de dato de la propiedad.
        Type propertyType = propertyInfo.PropertyType;

        // 5. Mapeamos el tipo de C# a los strings que usa nuestro BuildSelector.
        if (propertyType == typeof(int) || propertyType == typeof(long) || propertyType == typeof(short))
        {
            return "int";
        }
        if (propertyType == typeof(Guid))
        {
            return "guid";
        }
        // Para cualquier otro tipo (string, DateTime, etc.), no aplicaremos casting.
        return "string";
        }

    private List<dynamic> PerformLeftJoin(List<dynamic> leftData, List<dynamic> rightData)
    {
        return leftData.GroupJoin(
            rightData,
            outer => ((dynamic)outer).JoinKey, // Accede a la clave pre-procesada
            inner => ((dynamic)inner).JoinKey, // Accede a la clave pre-procesada
            (outer, innerGroup) => new { LeftObject = outer, RightGroup = innerGroup }
        )
        .SelectMany(
            group => group.RightGroup.DefaultIfEmpty(), // Crucial para LEFT JOIN: si no hay coincidencias, devuelve un solo 'null'
            (group, rightItem) => (dynamic)new
            {
                Left = ((dynamic)group.LeftObject).Data, // Extrae los datos originales
                Right = rightItem == null ? null : ((dynamic)rightItem).Data // Si no hay coincidencia, Right es null
            }
        ).ToList();
    }
}
