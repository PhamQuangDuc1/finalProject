using Microsoft.AspNetCore.Authorization;

namespace finalProject.Authorization;

public static class AuthorizationServiceCollectionExtensions
{
    public static IServiceCollection AddStudyMateAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(StudyMatePolicies.ManageDepartments, RequireAdmin);
            options.AddPolicy(StudyMatePolicies.ManageSubjects, RequireAdmin);
            options.AddPolicy(StudyMatePolicies.ManageChunkConfiguration, RequireAdmin);
            options.AddPolicy(StudyMatePolicies.ViewAllDocuments, RequireAdmin);
            options.AddPolicy(StudyMatePolicies.ViewTokenUsageStatistics, RequireAdmin);
            options.AddPolicy(StudyMatePolicies.ManageOwnDocuments, policy =>
                policy.RequireRole(StudyMateRoles.Teacher));
        });

        return services;
    }

    private static void RequireAdmin(AuthorizationPolicyBuilder policy)
    {
        policy.RequireRole(StudyMateRoles.Admin);
    }
}
