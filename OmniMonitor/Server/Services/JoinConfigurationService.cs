using Microsoft.EntityFrameworkCore;
using OmniMonitor.Server.Context;
using OmniMonitor.Server.Services;
using System.Dynamic;
using System.Linq.Dynamic.Core;
using System.Reflection;

public interface IJoinConfigurationService
{
    Task<CrossModuleJoin> CreateJoinAsync(CreateJoinRequestDto request, string username);
    Task<List<dynamic>> ExecuteJoinAsync(int joinId);
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
        // 1. Map DTOs to the database entities for Operands
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
        string leftJoinType = joinConfig.LeftOperand.JoinPropertyType;

        string rightJoinKey = joinConfig.RightOperand.JoinPropertyName;
        string rightJoinType = joinConfig.RightOperand.JoinPropertyType;

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

        var joinedResults = leftData.AsQueryable().Join(
            rightData.AsQueryable(),
            BuildSelector(leftJoinKey, leftJoinType),
            BuildSelector(rightJoinKey, rightJoinType),
            "new(outer as Left, inner as Right)"
        ).ToDynamicList();

        return FlattenJoinResults(joinedResults, joinConfig);
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
}