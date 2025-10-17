using Microsoft.EntityFrameworkCore;
using OmniMonitor.Server.Context;

public interface IReportService
{
    Task<Report> CreateReportAsync(CreateReportRequestDto request);
    Task<ReportJoin> AddJoinToReportAsync(int reportId, ReportJoinItemDto joinRequest, string username);
    Task<List<Report>> GetAllReportsByUsernameAsync(string username);
    Task<Report?> GetReportByIdAsync(int reportId, string username);
    Task<bool> DeleteReportAsync(int reportId, string username);
    Task<Report?> UpdateReportAsync(int reportId, string name, string descripcion, string username);
    Task<bool> RemoveJoinFromReportAsync(int reportId, int joinId, string username);
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
        var report = new Report
        {
            Name = request.Name,
            Description = request.Description,
            Username = request.Username
        };
        _context.Reports.Add(report);
        await _context.SaveChangesAsync();

        return report;
    }

    public async Task<ReportJoin> AddJoinToReportAsync(int reportId, ReportJoinItemDto joinRequest, string username)
    {
        // 1. Verificar que el reporte existe y pertenece al usuario.
        var report = await _context.Reports
            .FirstOrDefaultAsync(r => r.Id == reportId && r.Username == username);

        if (report == null)
        {
            // Si el reporte no se encuentra o no pertenece al usuario, lanzamos una excepción.
            throw new KeyNotFoundException($"El reporte con ID {reportId} no fue encontrado para este usuario.");
        }

        // 2. (Opcional pero recomendado) Verificar que el Join existe.
        var joinExists = await _context.CrossModuleJoins.AnyAsync(j => j.Id == joinRequest.CrossModuleJoinId);
        if (!joinExists)
        {
            throw new KeyNotFoundException($"La configuración de Join con ID {joinRequest.CrossModuleJoinId} no existe.");
        }

        // 3. Crear y añadir la nueva entrada en la tabla de enlace.
        var newReportJoin = new ReportJoin
        {
            ReportId = reportId,
            CrossModuleJoinId = joinRequest.CrossModuleJoinId,
            ExecutionOrder = joinRequest.ExecutionOrder
        };

        _context.ReportJoins.Add(newReportJoin);
        await _context.SaveChangesAsync();

        return newReportJoin;
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

    public async Task<bool> DeleteReportAsync(int reportId, string username)
    {
        // 1. Busca el reporte asegurándote de que pertenezca al usuario correcto.
        var reportToDelete = await _context.Reports
            .FirstOrDefaultAsync(r => r.Id == reportId && r.Username == username);

        // 2. Si no se encuentra, retorna false.
        if (reportToDelete == null)
        {
            return false;
        }

        // 3. Elimina el reporte. La base de datos se encargará de eliminar en cascada
        //    las entradas correspondientes en la tabla 'ReportJoins'.
        _context.Reports.Remove(reportToDelete);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<Report?> UpdateReportAsync(int reportId, string name, string descripcion, string username)
    {
        // 1. Busca el reporte asegurándote de que pertenezca al usuario correcto.
        var reportToUpdate = await _context.Reports
            .FirstOrDefaultAsync(r => r.Id == reportId && r.Username == username);

        // 2. Si no se encuentra, retorna null.
        if (reportToUpdate == null)
        {
            return null;
        }

        // 3. Actualiza las propiedades y guarda los cambios.
        reportToUpdate.Name = name;
        reportToUpdate.Description = descripcion;

        await _context.SaveChangesAsync();

        return reportToUpdate;
    }

    public async Task<bool> RemoveJoinFromReportAsync(int reportId, int joinId, string username)
    {
        var joinLinkToRemove = await _context.ReportJoins
            .FirstOrDefaultAsync(rj =>
                rj.ReportId == reportId &&
                rj.CrossModuleJoinId == joinId &&
                rj.Report.Username == username);

        if (joinLinkToRemove == null)
        {
            return false;
        }

        _context.ReportJoins.Remove(joinLinkToRemove);
        await _context.SaveChangesAsync();

        return true;
    }
}