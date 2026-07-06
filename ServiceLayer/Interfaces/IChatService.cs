using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceLayer.Interfaces
{
    public interface IChatService
    {
        Task<(bool success, string? answer, string? errorMessage)> GenerateAnswerAsync(string prompt);
    }

}
