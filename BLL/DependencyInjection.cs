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
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<ISubjectService, SubjectService>();
        services.AddScoped<ITeacherAssignmentService, TeacherAssignmentService>();
        services.AddScoped<ISystemSettingService, SystemSettingService>();
        services.AddScoped<IChunkingService, ChunkingService>();
        services.AddScoped<IChunkConfigurationService, SystemSettingService>();
        services.AddScoped<IAiCostEstimator, AiCostEstimator>();
        services.AddScoped<IAiUsageService, AiUsageService>();
        services.AddScoped<ITokenUsageStatisticsService, TokenUsageStatisticsService>();

        return services;
    }
}
