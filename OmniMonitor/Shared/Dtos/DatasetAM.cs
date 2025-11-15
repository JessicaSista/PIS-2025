using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OmniMonitor.Shared.Dtos
{
    public class DatasetAM
    {
        [Key]
        public int Id_Dataset { get; set; }

        [Required]
        [MaxLength(1)] // 'S' o 'N'
        public string Is_Dataset { get; set; } = string.Empty;

        [Required]
        [MaxLength(256)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Nombre { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Descripcion { get; set; }

        [MaxLength(10)]
        public string? ContentType { get; set; }

        [Required]
        public int Type_Dataset { get; set; }

        public int? Id_Event_Task { get; set; }

        public ICollection<DatasetEventTaskInstance> Grupo_Event_Task_Instance { get; set; } = new List<DatasetEventTaskInstance>();

        public int? Id_Asset_Type { get; set; }

        public ICollection<DatasetAsset> Grupo_Asset { get; set; } = new List<DatasetAsset>();
        public ICollection<DatasetStock> Grupo_Stock { get; set; } = new List<DatasetStock>();

        public int DatasetId { get; set; }  // Clave foránea
        
        /// <summary>
        /// Filtros aplicados almacenados como JSON. 
        /// Contiene un array de FilterCondition serializados.
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string? Filters { get; set; }
        
        [ForeignKey(nameof(DatasetId))]
        public virtual Datasets Datasets { get; set; } = null!;
    }
}
