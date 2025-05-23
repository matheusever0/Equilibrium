﻿using Equilibrium.Domain.Enums;

namespace Equilibrium.Domain.Entities
{
    public class Payment
    {
        public Guid Id { get; protected set; }
        public string Description { get; protected set; }
        public decimal Amount { get; protected set; }
        public DateTime DueDate { get; protected set; }
        public DateTime? PaymentDate { get; protected set; }
        public PaymentStatus Status { get; protected set; }
        public bool IsRecurring { get; protected set; }
        public string Notes { get; protected set; }
        public DateTime CreatedAt { get; protected set; }
        public DateTime? UpdatedAt { get; protected set; }

        public Guid UserId { get; protected set; }
        public User User { get; protected set; }

        public Guid PaymentTypeId { get; protected set; }
        public PaymentType PaymentType { get; protected set; }

        public Guid PaymentMethodId { get; protected set; }
        public PaymentMethod PaymentMethod { get; protected set; }

        public Guid? FinancingId { get; protected set; }
        public Financing? Financing { get; protected set; }

        public Guid? FinancingInstallmentId { get; protected set; }
        public FinancingInstallment? FinancingInstallment { get; protected set; }

        public ICollection<PaymentInstallment> Installments { get; protected set; }
        protected Payment()
        {
            Installments = [];
        }

        public Payment(
            string description,
            decimal amount,
            DateTime dueDate,
            PaymentType paymentType,
            PaymentMethod paymentMethod,
            User user,
            bool isRecurring = false,
            string notes = null)
        {
            Id = Guid.NewGuid();
            Description = description;
            Amount = amount;
            DueDate = dueDate;
            Status = PaymentStatus.Pending;
            IsRecurring = isRecurring;
            Notes = notes;
            CreatedAt = DateTime.Now;

            PaymentTypeId = paymentType.Id;
            PaymentType = paymentType;

            PaymentMethodId = paymentMethod.Id;
            PaymentMethod = paymentMethod;

            UserId = user.Id;
            User = user;

            Installments = [];
        }

        public Payment(
             string description,
             decimal amount,
             DateTime dueDate,
             PaymentType paymentType,
             PaymentMethod paymentMethod,
             User user,
             Financing financing,
             FinancingInstallment? financingInstallment = null,
             bool isRecurring = false,
             string notes = null)
        {
            Id = Guid.NewGuid();
            Description = description;
            Amount = amount;
            DueDate = dueDate;
            Status = PaymentStatus.Pending;
            IsRecurring = isRecurring;
            Notes = notes;
            CreatedAt = DateTime.Now;

            PaymentTypeId = paymentType.Id;
            PaymentType = paymentType;

            PaymentMethodId = paymentMethod.Id;
            PaymentMethod = paymentMethod;

            UserId = user.Id;
            User = user;

            Installments = [];
            FinancingId = financing.Id;
            Financing = financing;

            if (financingInstallment != null)
            {
                FinancingInstallmentId = financingInstallment.Id;
                FinancingInstallment = financingInstallment;
            }
        }

        public void MarkAsPaid(DateTime paymentDate)
        {
            PaymentDate = paymentDate;
            Status = PaymentStatus.Paid;

            // Se for um pagamento de financiamento
            if (FinancingId.HasValue)
            {
                // Se for um pagamento de parcela
                if (FinancingInstallmentId.HasValue && FinancingInstallment != null)
                {
                    FinancingInstallment.AddPayment(this);
                }

                // Atualizar o saldo devedor do financiamento
                if (Financing != null)
                {
                    // Somente o valor da amortização reduz o saldo devedor
                    decimal amortizationAmount = FinancingInstallment?.AmortizationAmount ?? Amount;
                    Financing.UpdateRemainingDebt(amortizationAmount);
                }
            }

            UpdateUpdatedAt();
        }

        public void MarkAsOverdue()
        {
            Status = PaymentStatus.Overdue;
            UpdateUpdatedAt();
        }

        public void Cancel()
        {
            Status = PaymentStatus.Cancelled;
            UpdateUpdatedAt();
        }

        public void AddInstallments(int numberOfInstallments)
        {
            if (numberOfInstallments <= 1)
                return;

            decimal installmentAmount = Math.Round(Amount / numberOfInstallments, 2);
            decimal remainingAmount = Amount - installmentAmount * (numberOfInstallments - 1);

            for (int i = 0; i < numberOfInstallments; i++)
            {
                var amount = i == numberOfInstallments - 1 ? remainingAmount : installmentAmount;
                var dueDate = DueDate.AddMonths(i);

                var installment = new PaymentInstallment(this, i + 1, amount, dueDate);
                Installments.Add(installment);
            }

            UpdateUpdatedAt();
        }

        public void UpdateDescription(string description)
        {
            Description = description;
            UpdateUpdatedAt();
        }

        public void UpdateAmount(decimal amount)
        {
            Amount = amount;
            UpdateUpdatedAt();
        }

        public void UpdateDueDate(DateTime dueDate)
        {
            DueDate = dueDate;
            UpdateUpdatedAt();
        }

        public void UpdateNotes(string notes)
        {
            Notes = notes;
            UpdateUpdatedAt();
        }

        public void UpdateType(PaymentType paymentType)
        {
            PaymentType = paymentType;
            UpdateUpdatedAt();
        }
        public void UpdateMethod(PaymentMethod paymentMethod)
        {
            PaymentMethod = paymentMethod;
            UpdateUpdatedAt();
        }


        public void UpdateRecurring(bool recurring)
        {
            IsRecurring = recurring;
            UpdateUpdatedAt();
        }

        private void UpdateUpdatedAt()
        {
            UpdatedAt = DateTime.Now;
        }
    }
}