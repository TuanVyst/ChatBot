using System.Threading.Tasks;

namespace ServiceLayer.Interfaces
{
    public interface IChatService
    {
        Task<(
            bool success,
            string? answer,
            int promptTokens,
            int completionTokens,
            int totalTokens,
            string modelName,
            string? errorMessage)>
        GenerateAnswerAsync(string prompt);
    }
}