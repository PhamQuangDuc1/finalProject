using BLL.Interfaces;

namespace BLL.Services;

public class AiCostEstimator : IAiCostEstimator
{
    private sealed record ModelPricing(decimal PromptUsdPerThousandTokens, decimal CompletionUsdPerThousandTokens);

    private static readonly IReadOnlyDictionary<string, ModelPricing> PricingByModel =
        new Dictionary<string, ModelPricing>(StringComparer.OrdinalIgnoreCase)
        {
            ["gpt-4o-mini"] = new(0.00015m, 0.00060m),
            ["gpt-4o"] = new(0.00500m, 0.01500m),
            ["text-embedding-3-small"] = new(0.00002m, 0.00000m),
            ["text-embedding-3-large"] = new(0.00013m, 0.00000m)
        };

    private static readonly ModelPricing DefaultPricing = new(0.00010m, 0.00030m);

    public decimal EstimateCost(string modelName, int promptTokens, int completionTokens)
    {
        var pricing = PricingByModel.TryGetValue(modelName, out var configuredPricing)
            ? configuredPricing
            : DefaultPricing;

        var promptCost = promptTokens / 1000m * pricing.PromptUsdPerThousandTokens;
        var completionCost = completionTokens / 1000m * pricing.CompletionUsdPerThousandTokens;

        return decimal.Round(promptCost + completionCost, 6);
    }
}
