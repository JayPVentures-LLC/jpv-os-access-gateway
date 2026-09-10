using Microsoft.Extensions.DependencyInjection;

namespace JPVOS.Services.Reciprocity;

public static class ReciprocityServiceCollectionExtensions
{
    public static IServiceCollection AddJpvReciprocityGate(this IServiceCollection services, string auditPath)
    {
        services.AddSingleton<ReciprocityEvaluator>();
        services.AddSingleton<ReciprocityAdmissionGate>();
        services.AddSingleton(new ReciprocityAuditStore(auditPath));
        services.AddSingleton<ReciprocityEnforcementService>();
        return services;
    }
}
