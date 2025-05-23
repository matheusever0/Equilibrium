﻿using Equilibrium.Domain.Interfaces.Repositories;

namespace Equilibrium.Domain.Interfaces.Services
{
    public interface IUnitOfWork : IDisposable
    {
        IUserRepository Users { get; }
        IRoleRepository Roles { get; }
        IPermissionRepository Permissions { get; }
        IPaymentRepository Payments { get; }
        IPaymentTypeRepository PaymentTypes { get; }
        IPaymentMethodRepository PaymentMethods { get; }
        ICreditCardRepository CreditCards { get; }
        IPaymentInstallmentRepository PaymentInstallments { get; }
        IIncomeRepository Incomes { get; }
        IIncomeTypeRepository IncomeTypes { get; }
        IIncomeInstallmentRepository IncomeInstallments { get; }
        IInvestmentRepository Investments { get; }
        IInvestmentTransactionRepository InvestmentTransactions { get; }
        IFinancingRepository Financings { get; }
        IFinancingInstallmentRepository FinancingInstallments { get; }
        Task<int> CompleteAsync();
    }
}