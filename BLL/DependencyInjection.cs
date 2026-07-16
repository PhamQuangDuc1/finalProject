using BLL.DTOs;
using BLL.Interfaces;
using BLL.Services;
using DAL;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BLL;

public static class DependencyInjection
{
    public static IServiceCollection AddBusinessServices(
        this IServiceCollection services,
        string connectionString,
        IConfiguration configuration)
    {
        services.AddDataAccess(connectionString);
        services.Configure<GeminiOptions>(configuration.GetSection("Gemini"));
        services.Configure<VNPayOptions>(configuration.GetSection("VNPay"));
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
        services.AddScoped<ISubscriptionPackageService, SubscriptionPackageService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IAdminSubscriptionService, AdminSubscriptionService>();
        services.AddHttpClient<IAiQuestionAnsweringService, GeminiQuestionAnsweringService>();
        services.AddScoped<IVnPayGateway, VnPayGateway>();

        return services;
    }
}
