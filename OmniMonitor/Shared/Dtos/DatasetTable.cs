using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OmniMonitor.Shared.Dtos
{
	[Table("DatasetTable")]
	public class DatasetTable
	{
		[Key]
		public int ID { get; set; }

		public string TipoDataset { get; set; }

		public int IdDataset { get; set; }

        public DateTime fechaCreacion { get; set; }
	}
}
