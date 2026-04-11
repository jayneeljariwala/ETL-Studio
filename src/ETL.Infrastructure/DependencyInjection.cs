using ETL.Infrastructure.BackgroundJobs;
using ETL.Application.ETL.Abstractions;
using ETL.Infrastructure.ETL.Engine;
using ETL.Infrastructure.ETL.Extractors;
using ETL.Infrastructure.ETL.Loaders;
using ETL.Infrastructure.ETL.Transform;
using ETL.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ETL.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddHttpClient();

        services.AddScoped<IDataExtractor, CsvDataExtractor>();
        services.AddScoped<IDataExtractor, ExcelDataExtractor>();
        services.AddScoped<IDataExtractor, SqlServerDataExtractor>();
        services.AddScoped<IDataExtractor, RestApiDataExtractor>();

        services.AddScoped<IDataLoader, PostgresDataLoader>();
        services.AddScoped<IDataLoader, SqlServerDataLoader>();

        services.AddScoped<ICustomExpressionEvaluator, RoslynExpressionEvaluator>();
        services.AddScoped<IDataTransformer, TransformationPipeline>();
        services.AddScoped<IEtlEngine, EtlEngine>();
        services.AddScoped<EtlJobBackgroundJob>();

        return services;
    }
}
