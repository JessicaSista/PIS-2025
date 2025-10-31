using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmniMonitor.Server.Context;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Reflection;
using System.Threading.Tasks;

namespace OmniMonitor.Server.Services
{
    /// <summary>
    /// Servicio para la gestión y ejecución de joins entre módulos.
    /// </summary>
    public interface IJoinConfigurationService
    {
        /// <summary>
        /// Crea una nueva configuración de join cruzado.
        /// </summary>
        /// <param name="request">Datos para la creación del join.</param>
        /// <param name="username">Nombre de usuario.</param>
        /// <returns>La configuración de join creada.</returns>
        Task<CrossModuleJoin> CreateJoinAsync(CreateJoinRequestDto request, string username);

        /// <summary>
        /// Ejecuta un join cruzado por su ID.
        /// </summary>
        /// <param name="joinId">ID del join.</param>
        /// <returns>Lista de resultados dinámicos del join.</returns>
        Task<List<dynamic>> ExecuteJoinAsync(int joinId);

        /// <summary>
        /// Obtiene todas las configuraciones de join de un usuario.
        /// </summary>
        /// <param name="username">Nombre de usuario.</param>
        /// <returns>Lista de joins configurados.</returns>
        Task<List<CrossModuleJoinDto>> GetJoinsByUsernameAsync(string username);
    }

    /// <inheritdoc />
    public class JoinConfigurationService : IJoinConfigurationService
    {
        #region Campos privados

        private readonly ApplicationDbContext _context;
        private readonly IApiDataService _apiDataService;
        private readonly ILogger<JoinConfigurationService> _logger;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor de JoinConfigurationService.
        /// </summary>
        /// <param name="context">Contexto de base de datos.</param>
        /// <param name="apiDataService">Servicio de datos dinámicos.</param>
        /// <param name="logger">Logger para registrar eventos.</param>
        public JoinConfigurationService(
            ApplicationDbContext context,
            IApiDataService apiDataService,
            ILogger<JoinConfigurationService> logger)
        {
            _context = context;
            _apiDataService = apiDataService;
            _logger = logger;
        }

        #endregion

        #region Métodos públicos

        /// <inheritdoc />
        public async Task<CrossModuleJoin> CreateJoinAsync(CreateJoinRequestDto request, string username)
        {
            try
            {
                _logger.LogInformation("Creando join '{Name}' para usuario {Username}", request.Name, username);

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

                _context.JoinOperands.Add(leftOperand);
                _context.JoinOperands.Add(rightOperand);
                await _context.SaveChangesAsync();

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
                await _context.SaveChangesAsync();

                _logger.LogInformation("Join '{Name}' creado correctamente para usuario {Username}", request.Name, username);

                return joinDefinition;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando join '{Name}' para usuario {Username}", request.Name, username);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<List<dynamic>> ExecuteJoinAsync(int joinId)
        {
            try
            {
                _logger.LogInformation("Ejecutando join con ID {JoinId}", joinId);

                var joinConfig = await _context.CrossModuleJoins
                    .Include(j => j.LeftOperand)
                    .Include(j => j.RightOperand)
                    .FirstOrDefaultAsync(j => j.Id == joinId);

                if (joinConfig == null)
                {
                    _logger.LogWarning("No se encontró la configuración de join con ID {JoinId}", joinId);
                    throw new KeyNotFoundException($"Join configuration with ID {joinId} not found.");
                }

                var leftData = await _apiDataService.GetDataForOperand(joinConfig.LeftOperand, joinConfig.Username);
                var rightData = await _apiDataService.GetDataForOperand(joinConfig.RightOperand, joinConfig.Username);

                if (leftData == null || rightData == null)
                {
                    _logger.LogWarning("No se pudo obtener datos para uno o ambos operandos del join con ID {JoinId}", joinId);
                    return new List<dynamic>();
                }

                string leftJoinKey = joinConfig.LeftOperand.JoinPropertyName;
                string leftJoinType = GetPropertyTypeDynamically(leftData, leftJoinKey);
                string rightJoinKey = joinConfig.RightOperand.JoinPropertyName;
                string rightJoinType = GetPropertyTypeDynamically(rightData, rightJoinKey);

                if ((joinConfig.JoinType == JoinType.Inner || joinConfig.JoinType == JoinType.LeftOuter) && (leftData == null || !leftData.AsQueryable().Any()))
                {
                    return new List<dynamic>();
                }
                if ((joinConfig.JoinType == JoinType.Inner || joinConfig.JoinType == JoinType.RightOuter) && (rightData == null || !rightData.AsQueryable().Any()))
                {
                    return new List<dynamic>();
                }

                if (!string.Equals(leftJoinType, rightJoinType, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Incompatibilidad de tipos para el Join: '{LeftKey}' ({LeftType}) vs '{RightKey}' ({RightType})", leftJoinKey, leftJoinType, rightJoinKey, rightJoinType);
                    throw new InvalidOperationException(
                        $"Incompatibilidad de tipos para el Join. La propiedad izquierda '{leftJoinKey}' es de tipo '{leftJoinType}', " +
                        $"pero la propiedad derecha '{rightJoinKey}' es de tipo '{rightJoinType}'. " +
                        "Por favor, asegúrate de que las propiedades en las que haces el Join tengan tipos de datos compatibles."
                    );
                }

                var processedLeftQuery = leftData.AsQueryable().Select($"new(it as Data, {BuildSelector(leftJoinKey, leftJoinType)} as JoinKey)");
                var processedRightQuery = rightData.AsQueryable().Select($"new(it as Data, {BuildSelector(rightJoinKey, rightJoinType)} as JoinKey)");

                var leftList = processedLeftQuery.ToDynamicList();
                var rightList = processedRightQuery.ToDynamicList();

                List<dynamic> nestedResults;
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
                        _logger.LogWarning("El tipo de Join '{JoinType}' no está soportado.", joinConfig.JoinType);
                        throw new NotSupportedException($"El tipo de Join '{joinConfig.JoinType}' no está soportado.");
                }

                return FlattenJoinResults(nestedResults, joinConfig);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ejecutando join con ID {JoinId}", joinId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<List<CrossModuleJoinDto>> GetJoinsByUsernameAsync(string username)
        {
            try
            {
                _logger.LogInformation("Obteniendo joins para usuario {Username}", username);

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo joins para usuario {Username}", username);
                throw;
            }
        }

        #endregion

        #region Métodos privados

        /// <summary>
        /// Construye el selector de clave para Dynamic LINQ.
        /// </summary>
        /// <param name="key">Nombre de la propiedad.</param>
        /// <param name="type">Tipo de la propiedad.</param>
        /// <returns>Expresión de selector.</returns>
        private string BuildSelector(string key, string type)
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

        /// <summary>
        /// Determina el tipo de una propiedad de forma dinámica.
        /// </summary>
        /// <param name="data">Colección de datos.</param>
        /// <param name="propertyName">Nombre de la propiedad.</param>
        /// <returns>Tipo de la propiedad como string.</returns>
        private string GetPropertyTypeDynamically(IEnumerable<dynamic> data, string propertyName)
        {
            var firstItem = data.AsQueryable().FirstOrDefault();
            if (firstItem == null)
            {
                return "string";
            }

            PropertyInfo propertyInfo = (firstItem as object).GetType().GetProperty(propertyName);
            if (propertyInfo == null)
            {
                throw new InvalidOperationException($"La propiedad '{propertyName}' no fue encontrada en el tipo de dato '{firstItem.GetType().Name}'.");
            }

            Type propertyType = propertyInfo.PropertyType;
            if (propertyType == typeof(int) || propertyType == typeof(long) || propertyType == typeof(short))
            {
                return "int";
            }
            if (propertyType == typeof(Guid))
            {
                return "guid";
            }
            return "string";
        }

        /// <summary>
        /// Realiza un Left Join entre dos listas dinámicas.
        /// </summary>
        /// <param name="leftData">Lista izquierda.</param>
        /// <param name="rightData">Lista derecha.</param>
        /// <returns>Lista de resultados del join.</returns>
        private List<dynamic> PerformLeftJoin(List<dynamic> leftData, List<dynamic> rightData)
        {
            return leftData.GroupJoin(
                rightData,
                outer => ((dynamic)outer).JoinKey,
                inner => ((dynamic)inner).JoinKey,
                (outer, innerGroup) => new { LeftObject = outer, RightGroup = innerGroup }
            )
            .SelectMany(
                group => group.RightGroup.DefaultIfEmpty(),
                (group, rightItem) => (dynamic)new
                {
                    Left = ((dynamic)group.LeftObject).Data,
                    Right = rightItem == null ? null : ((dynamic)rightItem).Data
                }
            ).ToList();
        }

        /// <summary>
        /// Aplana los resultados del join en una lista de objetos dinámicos.
        /// </summary>
        /// <param name="nestedResults">Resultados anidados.</param>
        /// <param name="joinConfig">Configuración del join.</param>
        /// <returns>Lista de objetos dinámicos aplanados.</returns>
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
                    if (string.Equals(leftPrefix, rightPrefix, StringComparison.Ordinal))
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

        #endregion
    }
}