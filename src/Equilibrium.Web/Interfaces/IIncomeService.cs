using Equilibrium.Web.Models.Income;
using Equilibrium.Web.Models.Filters;
using Equilibrium.Web.Models.Generics;

namespace Equilibrium.Web.Interfaces
{
    public interface IIncomeService
    {
        // M�todos principais com filtros avan�ados
        Task<IEnumerable<IncomeModel>> GetFilteredIncomesAsync(IncomeFilter filter, string token);
        Task<PagedResult<IncomeModel>> GetPagedIncomesAsync(IncomeFilter filter, string token);

        // M�todos existentes (mantidos para compatibilidade)
        Task<IEnumerable<IncomeModel>> GetAllIncomesAsync(string token);
        Task<IncomeModel> GetIncomeByIdAsync(string id, string token);
        Task<IEnumerable<IncomeModel>> GetIncomesByMonthAsync(int month, int year, string token);
        Task<IEnumerable<IncomeModel>> GetPendingIncomesAsync(string token);
        Task<IEnumerable<IncomeModel>> GetReceivedIncomesAsync(string token);
        Task<IEnumerable<IncomeModel>> GetOverdueIncomesAsync(string token);
        Task<IEnumerable<IncomeModel>> GetIncomesByTypeAsync(string typeId, string token);

        // Novos m�todos com filtros espec�ficos
        Task<IEnumerable<IncomeModel>> GetIncomesByDateRangeAsync(DateTime startDate, DateTime endDate, string token);
        Task<IEnumerable<IncomeModel>> GetIncomesByAmountRangeAsync(decimal minAmount, decimal maxAmount, string token);
        Task<IEnumerable<IncomeModel>> GetRecurringIncomesAsync(string token);
        Task<IEnumerable<IncomeModel>> SearchIncomesAsync(string searchTerm, string token);
        Task<IEnumerable<IncomeModel>> GetIncomesByReceivedDateRangeAsync(DateTime startDate, DateTime endDate, string token);

        // M�todos de a��o (CRUD)
        Task<IncomeModel> CreateIncomeAsync(CreateIncomeModel model, string token);
        Task<IncomeModel> UpdateIncomeAsync(string id, UpdateIncomeModel model, string token);
        Task DeleteIncomeAsync(string id, string token);
        Task<IncomeModel> MarkAsReceivedAsync(string id, DateTime? receivedDate, string token);
        Task<IncomeModel> CancelIncomeAsync(string id, string token);

        // M�todos para parcelas
        Task<string> GetInstallmentParentIncomeAsync(string installmentId, string token);
        Task<bool> MarkInstallmentAsReceivedAsync(string installmentId, DateTime receivedDate, string token);
        Task<bool> CancelInstallmentAsync(string installmentId, string token);

        // M�todos estat�sticos
        Task<decimal> GetTotalIncomesByPeriodAsync(int month, int year, string token);
        Task<decimal> GetPendingIncomesTotalAsync(string token);
        Task<decimal> GetReceivedIncomesTotalAsync(string token);

        // M�todos para relat�rios
        Task<Dictionary<string, decimal>> GetIncomesByTypeAsync(int month, int year, string token);
        Task<List<IncomeModel>> GetRecentIncomesAsync(int count, string token);
        Task<List<IncomeModel>> GetUpcomingIncomesAsync(int days, string token);

        // M�todos para an�lise de tend�ncias
        Task<Dictionary<string, decimal>> GetMonthlyIncomeAnalysisAsync(int year, string token);
        Task<decimal> GetAverageMonthlyIncomeAsync(int year, string token);
        Task<(decimal currentPeriod, decimal previousPeriod, decimal percentageChange)>
            ComparePeriodsAsync(DateTime currentStart, DateTime currentEnd, string token);
    }
}