namespace BLL.Interfaces;

public interface IAiCostEstimator
{
    decimal EstimateCost(string modelName, int promptTokens, int completionTokens);
}
