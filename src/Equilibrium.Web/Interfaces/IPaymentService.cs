using Equilibrium.Web.Models.Payment;
using Equilibrium.Web.Models.Filters;
using Equilibrium.Web.Models.Generics;

namespace Equilibrium.Web.Interfaces
{
    public interface IPaymentService
    {
        // M�todos principais com filtros avan�ados
        Task<IEnumerable<PaymentModel>> GetFilteredPaymentsAsync(PaymentFilter filter, string token);
        Task<PagedResult<PaymentModel>> GetPagedPaymentsAsync(PaymentFilter filter, string token);

        // M�todos existentes (mantidos para compatibilidade)
        Task<IEnumerable<PaymentModel>> GetAllPaymentsAsync(string token);
        Task<PaymentModel> GetPaymentByIdAsync(string id, string token);
        Task<IEnumerable<PaymentModel>> GetPaymentsByMonthAsync(int month, int year, string token);
        Task<IEnumerable<PaymentModel>> GetPendingPaymentsAsync(string token);
        Task<IEnumerable<PaymentModel>> GetOverduePaymentsAsync(string token);
        Task<IEnumerable<PaymentModel>> GetPaymentsByTypeAsync(string typeId, string token);
        Task<IEnumerable<PaymentModel>> GetPaymentsByMethodAsync(string methodId, string token);

        // Novos m�todos com filtros espec�ficos
        Task<IEnumerable<PaymentModel>> GetPaymentsByDateRangeAsync(DateTime startDate, DateTime endDate, string token);
        Task<IEnumerable<PaymentModel>> GetPaymentsByAmountRangeAsync(decimal minAmount, decimal maxAmount, string token);
        Task<IEnumerable<PaymentModel>> GetRecurringPaymentsAsync(string token);
        Task<IEnumerable<PaymentModel>> GetPaymentsByCreditCardAsync(string creditCardId, string token);
        Task<IEnumerable<PaymentModel>> GetFinancingPaymentsAsync(string financingId, string token);
        Task<IEnumerable<PaymentModel>> SearchPaymentsAsync(string searchTerm, string token);

        // M�todos de a��o (CRUD)
        Task<PaymentModel> CreatePaymentAsync(CreatePaymentModel model, string token);
        Task<PaymentModel> UpdatePaymentAsync(string id, UpdatePaymentModel model, string token);
        Task DeletePaymentAsync(string id, string token);
        Task<PaymentModel> MarkAsPaidAsync(string id, DateTime? paymentDate, string token);
        Task<PaymentModel> MarkAsOverdueAsync(string id, string token);
        Task<PaymentModel> CancelPaymentAsync(string id, string token);

        // M�todos para parcelas
        Task<string> GetInstallmentParentPaymentAsync(string installmentId, string token);
        Task<bool> MarkInstallmentAsPaidAsync(string installmentId, DateTime paymentDate, string token);
        Task<bool> MarkInstallmentAsOverdueAsync(string installmentId, string token);
        Task<bool> CancelInstallmentAsync(string installmentId, string token);

        // M�todos estat�sticos
        Task<decimal> GetTotalPaymentsByPeriodAsync(int month, int year, string token);
        Task<decimal> GetPendingPaymentsTotalAsync(string token);
        Task<decimal> GetOverduePaymentsTotalAsync(string token);

        // M�todos para relat�rios
        Task<Dictionary<string, decimal>> GetPaymentsByTypeAsync(int month, int year, string token);
        Task<Dictionary<string, decimal>> GetPaymentsByMethodAsync(int month, int year, string token);
    }
}