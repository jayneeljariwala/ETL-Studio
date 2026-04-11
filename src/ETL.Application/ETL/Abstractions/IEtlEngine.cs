using ETL.Application.ETL.Models;

namespace ETL.Application.ETL.Abstractions;

public interface IEtlEngine
{
    Task<EtlExecutionResult> ExecuteAsync(EtlExecutionRequest request, CancellationToken cancellationToken);
}
