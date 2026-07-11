using BLL.Interfaces;
using BLL.Services;
using DAL;
using Microsoft.Extensions.DependencyInjection;

namespace BLL;

public static class DependencyInjection
{
    public static IServiceCollection AddBusinessServices(this IServiceCollection services, string connectionString)
    {
        services.AddDataAccess(connectionString);
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<ISubjectService, SubjectService>();
        services.AddScoped<IChunkConfigurationService, ChunkConfigurationService>();
        services.AddScoped<ITokenUsageStatisticsService, TokenUsageStatisticsService>();

        return services;
    }
}
