using Microsoft.EntityFrameworkCore;
using OmniMonitor.Server.Context;

public interface IReportService
{
    Task<Report> CreateReportAsync(CreateReportRequestDto request);
    Task<List<Report>> GetAllReportsByUsernameAsync(string username);
    Task<Report?> GetReportByIdAsync(int reportId, string username);
}

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _context;

    public ReportService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Creates a new report and returns the complete Report entity.
    /// </summary>
    public async Task<Report> CreateReportAsync(CreateReportRequestDto request)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var report = new Report
            {
                Name = request.Name,
                Description = request.Description,
                Username = request.Username
            };
            _context.Reports.Add(report);
            await _context.SaveChangesAsync();

            if (request.ReportJoins != null && request.ReportJoins.Any())
            {
                foreach (var joinItemDto in request.ReportJoins)
                {
                    var reportJoin = new ReportJoin
                    {
                        ReportId = report.Id,
                        CrossModuleJoinId = joinItemDto.CrossModuleJoinId,
                        ExecutionOrder = joinItemDto.ExecutionOrder
                    };
                    _context.ReportJoins.Add(reportJoin);
                }
                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();
            return report;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Retrieves a list of all Report entities for a user.
    /// </summary>
    public async Task<List<Report>> GetAllReportsByUsernameAsync(string username)
    {
        return await _context.Reports
            .AsNoTracking()
            .Where(r => r.Username == username)
            .ToListAsync();
    }

    /// <summary>
    /// Retrieves a single, fully detailed Report entity by its ID.
    /// </summary>
    public async Task<Report?> GetReportByIdAsync(int reportId, string username)
    {
        return await _context.Reports
            .AsNoTracking()
            .Where(r => r.Id == reportId && r.Username == username)
            .Include(r => r.ReportJoins)
                .ThenInclude(rj => rj.CrossModuleJoin)
                    .ThenInclude(j => j.LeftOperand)
            .Include(r => r.ReportJoins)
                .ThenInclude(rj => rj.CrossModuleJoin)
                    .ThenInclude(j => j.RightOperand) 
            .FirstOrDefaultAsync();
    }
}