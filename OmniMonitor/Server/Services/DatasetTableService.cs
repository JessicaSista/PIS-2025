using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OmniMonitor.Server.Context;
using OmniMonitor.Shared.Dtos;
using OmniMonitor.Server.Services;


namespace OmniMonitor.Server.Services
{

    public interface IDatasetTableService
    {
        Task<List<ResumenDataset>> GetAllAsync(string username);
        Task<ResponseDatasetTable> GetByIdAsync(int id, string username);
    Task<DatasetTable> AddAsync(System.Text.Json.JsonElement createRequest, string tipoDataset, string username);
        Task<DatasetTable> UpdateAsync(DatasetTable datasetTable);
        Task<bool> DeleteAsync(int id, string username);
    }


    public class DatasetTableService : IDatasetTableService
    {
        private readonly ApplicationDbContext _context;
        private readonly IDatasetAmService _datasetAM;
        private readonly IDatasetService _datasetIM;
        private readonly IDatasetUMService _datasetUM;
        private readonly IDatasetEMService _datasetEM;


        public DatasetTableService(ApplicationDbContext context, IDatasetAmService datasetAM, IDatasetService datasetIM, IDatasetUMService datasetUM, IDatasetEMService datasetEM)
        {
            _context = context;
            _datasetAM = datasetAM;
            _datasetIM = datasetIM;
            _datasetUM = datasetUM;
            _datasetEM = datasetEM;
        }

        public async Task<List<ResumenDataset>> GetAllAsync(string username)
        {
            var resumenes = new List<ResumenDataset>();

            var datasetTables = await _context.Set<DatasetTable>().ToListAsync();

            var datasetsAM = await _datasetAM.GetAllDatasetAMsAsync(username);
            resumenes.AddRange(datasetsAM.Select(d => {
                var tabla = datasetTables.FirstOrDefault(t => t.TipoDataset == "AM" && t.IdDataset == d.Id_Dataset);
                return new ResumenDataset
                {
                    ID_Table = tabla?.ID ?? 0,
                    ID = d.Id_Dataset,
                    Nombre = d.Nombre,
                    Descripcion = d.Descripcion,
                    UltimaActualizacion = tabla.fechaCreacion
                };
            }));

            var datasetsIM = await _datasetIM.GetAllDatasetsIMAsync(username);
            resumenes.AddRange(datasetsIM.Select(d => {
                var tabla = datasetTables.FirstOrDefault(t => t.TipoDataset == "IM" && t.IdDataset == d.Id);
                return new ResumenDataset
                {
                    ID_Table = tabla?.ID ?? 0,
                    ID = d.Id,
                    Nombre = d.Name,
                    Descripcion = d.Description,
                    UltimaActualizacion = tabla.fechaCreacion
                };
            }));

            var datasetsUM = await _datasetUM.GetAllDatasetsUMAsync(username);
            resumenes.AddRange(datasetsUM.Select(d => {
                var tabla = datasetTables.FirstOrDefault(t => t.TipoDataset == "UM" && t.IdDataset == d.Id);
                return new ResumenDataset
                {
                    ID_Table = tabla?.ID ?? 0,
                    ID = d.Id,
                    Nombre = d.Name,
                    Descripcion = d.Description,
                    UltimaActualizacion = tabla.fechaCreacion
                };
            }));

            var datasetsEM = await _datasetEM.GetAllDatasetsEMAsync(username);
            resumenes.AddRange(datasetsEM.Select(d => {
                var tabla = datasetTables.FirstOrDefault(t => t.TipoDataset == "EM" && t.IdDataset == d.Id);
                return new ResumenDataset
                {
                    ID_Table = tabla?.ID ?? 0,
                    ID = d.Id,
                    Nombre = d.Name,
                    Descripcion = d.Description,
                    UltimaActualizacion = tabla.fechaCreacion
                };
            }));

            return resumenes;
        }

        public async Task<ResponseDatasetTable> GetByIdAsync(int id, string username)
        {
            var tabla = await _context.Set<DatasetTable>().FindAsync(id);
            if (tabla == null) return null;

            object result = null;
            switch (tabla.TipoDataset)
            {
                case "AM":
                    result = await _datasetAM.GetDatasetAMByIdAsync(tabla.IdDataset, username);
                    break;
                case "IM":
                    result = await _datasetIM.GetDatasetIMByIdAsync(tabla.IdDataset, username);
                    break;
                case "UM":
                    result = await _datasetUM.GetDatasetUMByIdAsync(tabla.IdDataset, username);
                    break;
                case "EM":
                    result = await _datasetEM.GetDatasetEMByIdAsync(tabla.IdDataset, username);
                    break;
                default:
                    return null;
            }
            if (result == null)
            {
                return null;
            }

            // Serializar el resultado a JsonElement
            var options = new System.Text.Json.JsonSerializerOptions
            {
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
                WriteIndented = false
            };
            var json = System.Text.Json.JsonSerializer.Serialize(result, options);
            var dataElement = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);

            return new ResponseDatasetTable
            {
                IdDatasetTable = tabla.ID,
                TipoDataset = tabla.TipoDataset,
                Data = dataElement
            };
        }

        public async Task<DatasetTable> AddAsync(System.Text.Json.JsonElement createRequest, string tipoDataset, string username)
        {
            int idDataset = 0;
            switch (tipoDataset)
            {
                case "AM":
                    var amReq = System.Text.Json.JsonSerializer.Deserialize<OmniMonitor.Shared.Dtos.CreateDatasetAMRequest>(createRequest.GetRawText());
                    var amResult = await _datasetAM.CreateDatasetAMAsync(amReq);
                    idDataset = amResult.Id_Dataset;
                    break;
                case "IM":
                    var imReq = System.Text.Json.JsonSerializer.Deserialize<OmniMonitor.Shared.Dtos.CreateDatasetRequest>(createRequest.GetRawText());
                    Console.WriteLine($"[DEBUG] IMRequest.Name: {imReq.Name}, IMRequest.Username: {imReq.Username}");
                    var imResult = await _datasetIM.CreateDatasetIMAsync(imReq);
                    idDataset = imResult.Id;
                    break;
                case "UM":
                    var umReq = System.Text.Json.JsonSerializer.Deserialize<OmniMonitor.Shared.Dtos.CreateDatasetUMRequest>(createRequest.GetRawText());
                    var umResult = await _datasetUM.CreateDatasetUMAsync(umReq);
                    idDataset = umResult.Id;
                    break;
                case "EM":
                    var emReq = System.Text.Json.JsonSerializer.Deserialize<OmniMonitor.Shared.Dtos.CreateDatasetEMRequest>(createRequest.GetRawText());
                    var emResult = await _datasetEM.CreateDatasetEMAsync(emReq);
                    idDataset = emResult.Id;
                    break;
                default:
                    throw new System.Exception("Tipo de dataset no soportado");
            }

            var datasetTable = new DatasetTable
            {
                TipoDataset = tipoDataset,
                IdDataset = idDataset,
                fechaCreacion = DateTime.UtcNow
            };
            _context.Set<DatasetTable>().Add(datasetTable);
            await _context.SaveChangesAsync();
            return datasetTable;
        }

        public async Task<DatasetTable> UpdateAsync(DatasetTable datasetTable)
        {
            _context.Set<DatasetTable>().Update(datasetTable);
            await _context.SaveChangesAsync();
            return datasetTable;
        }

        public async Task<bool> DeleteAsync(int id, string username)
        {
            var entity = await _context.Set<DatasetTable>().FindAsync(id);
            if (entity == null) return false;

            // Eliminar la instancia correspondiente en la tabla de datasets
            switch (entity.TipoDataset)
            {
                case "AM":
                    await _datasetAM.DeleteDatasetAMAsync(entity.IdDataset, username);
                    break;
                case "IM":
                    await _datasetIM.DeleteDatasetIMAsync(entity.IdDataset, username);
                    break;
                case "UM":
                    await _datasetUM.DeleteDatasetUMAsync(entity.IdDataset, username);
                    break;
                case "EM":
                    await _datasetEM.DeleteDatasetEMAsync(entity.IdDataset, username);
                    break;
            }

            _context.Set<DatasetTable>().Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
