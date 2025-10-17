public class ReportJoin
{
    // Foreign key to the Report
    public int ReportId { get; set; }
    public virtual Report Report { get; set; }

    // Foreign key to the CrossModuleJoin
    public int CrossModuleJoinId { get; set; }
    public virtual CrossModuleJoin CrossModuleJoin { get; set; }

    // You can add extra data about the relationship here.
    // For example, the order in which this join should run in the report.
    public int ExecutionOrder { get; set; }
}