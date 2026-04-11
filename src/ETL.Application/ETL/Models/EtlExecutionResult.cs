namespace ETL.Application.ETL.Models;

public sealed class EtlExecutionResult
{
    public int RecordsRead { get; set; }
    public int RecordsTransformed { get; set; }
    public int RecordsLoaded { get; set; }
    public int RecordsFailed { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> ValidationErrors { get; } = new();
}
