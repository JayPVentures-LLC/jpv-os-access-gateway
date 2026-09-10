using System.Security.Claims;

namespace JPVOS.Services.Reciprocity;

public sealed class ReciprocityAccessMiddleware
{
    private readonly RequestDelegate _next;

    public ReciprocityAccessMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(
        HttpContext context,
        ReciprocityLedgerStore ledger,
        ReciprocityEnforcementService enforcement)
    {
        if (context.User.Identity?.IsAuthenticated != true || context.User.IsInRole("Founder"))
        {
            await _next(context);
            return;
        }

        var subjectId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(subjectId))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        var evidence = ledger.GetEvidence(subjectId);
        if (evidence is null)
        {
            await _next(context);
            return;
        }

        var isRemediationPath = context.Request.Path.StartsWithSegments("/reciprocity/remediation", StringComparison.OrdinalIgnoreCase);
        var decision = await enforcement.EvaluateAsync(
            evidence,
            new ReciprocityAdmissionRequest(
                subjectId,
                context.Request.Path.Value ?? "/",
                IsJpvOwnedOrAdministered: true,
                IsDiscretionary: true,
                IsRemediationPath: isRemediationPath),
            context.RequestAborted);

        if (!decision.Allowed)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.Headers["JPV-Access-Reason"] = decision.ReasonCode;
            return;
        }

        await _next(context);
    }
}
