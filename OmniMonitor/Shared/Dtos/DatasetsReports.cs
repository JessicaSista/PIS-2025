public class DatasetReports
{
    public int ReportId { get; set; }
    public virtual Report Report { get; set; }

    public int DatasetsOfReportsId { get; set; }
    public virtual DatasetsOfReports DatasetsOfReports { get; set; }
}

