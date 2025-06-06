﻿using AutoMapper;
using Equilibrium.Application.DTOs.Payment;
using Equilibrium.Application.Interfaces;
using Equilibrium.Domain.Entities;
using Equilibrium.Domain.Enums;
using Equilibrium.Domain.Interfaces.Services;
using Equilibrium.Resources;
using Microsoft.Extensions.DependencyInjection;

namespace Equilibrium.Application.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IServiceProvider _serviceProvider;

        public PaymentService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IServiceProvider serviceProvider)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _serviceProvider = serviceProvider;
        }

        public async Task<PaymentDto> GetByIdAsync(Guid id)
        {
            var payment = await _unitOfWork.Payments.GetPaymentWithDetailsAsync(id);
            return payment == null ? throw new KeyNotFoundException(ResourceFinanceApi.Payment_NotFound) : _mapper.Map<PaymentDto>(payment);
        }

        public async Task<IEnumerable<PaymentDto>> GetAllByUserIdAsync(Guid userId)
        {
            var payments = await _unitOfWork.Payments.GetPaymentsByUserIdAsync(userId);
            return _mapper.Map<IEnumerable<PaymentDto>>(payments);
        }

        public async Task<IEnumerable<PaymentDto>> GetByMonthAsync(Guid userId, int month, int year)
        {
            var payments = await _unitOfWork.Payments.GetPaymentsByUserIdAndMonthAsync(userId, month, year);
            return _mapper.Map<IEnumerable<PaymentDto>>(payments);
        }

        public async Task<IEnumerable<PaymentDto>> GetPendingAsync(Guid userId)
        {
            var payments = await _unitOfWork.Payments.GetPendingPaymentsByUserIdAsync(userId);
            return _mapper.Map<IEnumerable<PaymentDto>>(payments);
        }

        public async Task<IEnumerable<PaymentDto>> GetOverdueAsync(Guid userId)
        {
            var payments = await _unitOfWork.Payments.GetOverduePaymentsByUserIdAsync(userId);
            return _mapper.Map<IEnumerable<PaymentDto>>(payments);
        }

        public async Task<IEnumerable<PaymentDto>> GetByTypeAsync(Guid userId, Guid paymentTypeId)
        {
            var payments = await _unitOfWork.Payments.GetPaymentsByTypeAsync(userId, paymentTypeId);
            return _mapper.Map<IEnumerable<PaymentDto>>(payments);
        }

        public async Task<IEnumerable<PaymentDto>> GetByMethodAsync(Guid userId, Guid paymentMethodId)
        {
            var payments = await _unitOfWork.Payments.GetPaymentsByMethodAsync(userId, paymentMethodId);
            return _mapper.Map<IEnumerable<PaymentDto>>(payments);
        }

        public async Task<PaymentDto> CreateAsync(CreatePaymentDto createPaymentDto, Guid userId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId) ?? throw new KeyNotFoundException(ResourceFinanceApi.User_NotFound);
            var paymentType = await _unitOfWork.PaymentTypes.GetByIdAsync(createPaymentDto.PaymentTypeId) ?? throw new KeyNotFoundException(ResourceFinanceApi.PaymentType_NotFound);
            if (!paymentType.IsSystem && paymentType.UserId != userId)
                throw new UnauthorizedAccessException(ResourceFinanceApi.Error_Unauthorized);

            var paymentMethod = await _unitOfWork.PaymentMethods.GetByIdAsync(createPaymentDto.PaymentMethodId) ?? throw new KeyNotFoundException(ResourceFinanceApi.PaymentMethod_NotFound);
            if (!paymentMethod.IsSystem && paymentMethod.UserId != userId)
                throw new UnauthorizedAccessException(ResourceFinanceApi.Error_Unauthorized);

            if (paymentMethod.Type == PaymentMethodType.CreditCard && !createPaymentDto.CreditCardId.HasValue)
                throw new InvalidOperationException(ResourceFinanceApi.Payment_CreditCardRequired);

            if (createPaymentDto.CreditCardId.HasValue)
            {
                var creditCard = await _unitOfWork.CreditCards.GetByIdAsync(createPaymentDto.CreditCardId.Value) ?? throw new KeyNotFoundException(ResourceFinanceApi.CreditCard_NotFound);
                if (creditCard.UserId != userId)
                    throw new UnauthorizedAccessException(ResourceFinanceApi.Error_Unauthorized);

                if (creditCard.AvailableLimit < createPaymentDto.Amount)
                    throw new InvalidOperationException(ResourceFinanceApi.Payment_InsufficientCreditLimit);

                creditCard.DecrementAvailableLimit(createPaymentDto.Amount);
                await _unitOfWork.CreditCards.UpdateAsync(creditCard);
            }

            Payment? payment = null;

            if (paymentType.IsFinancingType)
            {
                // Verificar se foi fornecido um financiamento
                if (!createPaymentDto.FinancingId.HasValue)
                    throw new InvalidOperationException("É necessário informar um financiamento para este tipo de pagamento");

                // Buscar o financiamento
                var financing = await _unitOfWork.Financings.GetByIdAsync(createPaymentDto.FinancingId.Value);
                if (financing == null)
                    throw new KeyNotFoundException("Financiamento não encontrado");

                if (financing.UserId != userId)
                    throw new UnauthorizedAccessException(ResourceFinanceApi.Error_Unauthorized);

                // Se informou uma parcela específica
                if (createPaymentDto.FinancingInstallmentId.HasValue)
                {
                    var installment = await _unitOfWork.FinancingInstallments.GetByIdAsync(createPaymentDto.FinancingInstallmentId.Value);
                    if (installment == null)
                        throw new KeyNotFoundException("Parcela de financiamento não encontrada");

                    if (installment.FinancingId != financing.Id)
                        throw new InvalidOperationException("A parcela não pertence ao financiamento informado");

                    // Criar um pagamento vinculado ao financiamento e parcela
                    payment = new Payment(
                        createPaymentDto.Description,
                        createPaymentDto.Amount,
                        createPaymentDto.DueDate,
                        paymentType,
                        paymentMethod,
                        user,
                        financing,
                        installment,
                        createPaymentDto.IsRecurring,
                        createPaymentDto.Notes
                    );
                }
                else
                {
                    // Criar um pagamento vinculado apenas ao financiamento
                    payment = new Payment(
                        createPaymentDto.Description,
                        createPaymentDto.Amount,
                        createPaymentDto.DueDate,
                        paymentType,
                        paymentMethod,
                        user,
                        financing,
                        null,  // Sem parcela específica
                        createPaymentDto.IsRecurring,
                        createPaymentDto.Notes
                    );
                }
            }
            else
            {
                payment = new Payment(
                        createPaymentDto.Description,
                        createPaymentDto.Amount,
                        createPaymentDto.DueDate,
                        paymentType,
                        paymentMethod,
                        user,
                        createPaymentDto.IsRecurring,
                        createPaymentDto.Notes
                    );
             }

            await _unitOfWork.Payments.AddAsync(payment);

            if (createPaymentDto.PaymentDate.HasValue)
            {
                payment.MarkAsPaid(createPaymentDto.PaymentDate.Value);

                await _unitOfWork.CompleteAsync();

                if (payment.FinancingId.HasValue)
                {
                    // Após marcar como pago, recalcular as parcelas restantes
                    var financingService = _serviceProvider.GetRequiredService<IFinancingService>();
                    await financingService.RecalculateRemainingInstallmentsAsync(payment.FinancingId.Value, payment.DueDate);
                }
            }

            if (createPaymentDto.NumberOfInstallments > 1)
            {
                payment.AddInstallments(createPaymentDto.NumberOfInstallments);
            }

            await _unitOfWork.CompleteAsync();
            var savedPayment = await _unitOfWork.Payments.GetByIdAsync(payment.Id);
            return _mapper.Map<PaymentDto>(savedPayment);
        }

        public async Task<PaymentDto> UpdateAsync(Guid id, UpdatePaymentDto updatePaymentDto)
        {
            var payment = await _unitOfWork.Payments.GetPaymentWithDetailsAsync(id) ?? throw new KeyNotFoundException(ResourceFinanceApi.Payment_NotFound);
            if (!string.IsNullOrEmpty(updatePaymentDto.Description))
                payment.UpdateDescription(updatePaymentDto.Description);

            if (updatePaymentDto.Amount > 0)
                payment.UpdateAmount(updatePaymentDto.Amount);

            if (updatePaymentDto.DueDate.HasValue)
                payment.UpdateDueDate(updatePaymentDto.DueDate.Value);

            if (updatePaymentDto.Notes != null)
                payment.UpdateNotes(updatePaymentDto.Notes);

            if (updatePaymentDto.Status.HasValue)
            {
                switch (updatePaymentDto.Status.Value)
                {
                    case PaymentStatus.Paid:
                        if (updatePaymentDto.PaymentDate.HasValue)
                            payment.MarkAsPaid(updatePaymentDto.PaymentDate.Value);
                        else
                            payment.MarkAsPaid(DateTime.Now);
                        break;
                    case PaymentStatus.Overdue:
                        payment.MarkAsOverdue();
                        break;
                    case PaymentStatus.Cancelled:
                        payment.Cancel();
                        break;
                }
            }

            if (updatePaymentDto.PaymentTypeId.HasValue)
            {
                var paymentType = await _unitOfWork.PaymentTypes.GetByIdAsync(updatePaymentDto.PaymentTypeId.Value) ?? throw new KeyNotFoundException(ResourceFinanceApi.PaymentType_NotFound);
                if (!paymentType.IsSystem && paymentType.UserId != payment.UserId)
                    throw new UnauthorizedAccessException(ResourceFinanceApi.Error_Unauthorized);

                payment.UpdateType(paymentType);

            }

            if (updatePaymentDto.PaymentMethodId.HasValue)
            {
                var paymentMethod = await _unitOfWork.PaymentMethods.GetByIdAsync(updatePaymentDto.PaymentMethodId.Value) ?? throw new KeyNotFoundException(ResourceFinanceApi.PaymentMethod_NotFound);
                if (!paymentMethod.IsSystem && paymentMethod.UserId != payment.UserId)
                    throw new UnauthorizedAccessException(ResourceFinanceApi.Error_Unauthorized);

                payment.UpdateMethod(paymentMethod);

            }

            if (updatePaymentDto.IsRecurring.HasValue)
            {
                payment.UpdateRecurring(updatePaymentDto.IsRecurring.Value);
            }

            await _unitOfWork.Payments.UpdateAsync(payment);
            await _unitOfWork.CompleteAsync();

            return _mapper.Map<PaymentDto>(payment);
        }

        public async Task DeleteAsync(Guid id)
        {
            var payment = await _unitOfWork.Payments.GetByIdAsync(id) ?? throw new KeyNotFoundException(ResourceFinanceApi.Payment_NotFound);
            if (payment.Status == PaymentStatus.Paid)
                throw new InvalidOperationException(ResourceFinanceApi.Payment_AlreadyPaid);

            await _unitOfWork.Payments.DeleteAsync(payment);
            await _unitOfWork.CompleteAsync();
        }

        public async Task<PaymentDto> MarkAsPaidAsync(Guid id, DateTime paymentDate)
        {
            var payment = await _unitOfWork.Payments.GetPaymentWithDetailsAsync(id) ??
                throw new KeyNotFoundException(ResourceFinanceApi.Payment_NotFound);

            payment.MarkAsPaid(paymentDate);
            await _unitOfWork.Payments.UpdateAsync(payment);

            if (payment.FinancingId.HasValue)
            {
                var financingService = _serviceProvider.GetRequiredService<IFinancingService>();

                await financingService.RecalculateRemainingInstallmentsAsync(payment.FinancingId.Value, payment.DueDate);
            }

            await _unitOfWork.CompleteAsync();

            return _mapper.Map<PaymentDto>(payment);
        }

        public async Task<PaymentDto> MarkAsOverdueAsync(Guid id)
        {
            var payment = await _unitOfWork.Payments.GetPaymentWithDetailsAsync(id) ?? throw new KeyNotFoundException(ResourceFinanceApi.Payment_NotFound);
            payment.MarkAsOverdue();
            await _unitOfWork.Payments.UpdateAsync(payment);
            await _unitOfWork.CompleteAsync();

            return _mapper.Map<PaymentDto>(payment);
        }

        public async Task<PaymentDto> CancelAsync(Guid id)
        {
            var payment = await _unitOfWork.Payments.GetPaymentWithDetailsAsync(id) ??
                throw new KeyNotFoundException(ResourceFinanceApi.Payment_NotFound);

            // Se o pagamento estiver associado a um financiamento e estiver no status Pago
            if (payment.FinancingId.HasValue && payment.Status == PaymentStatus.Paid)
            {
                var financing = await _unitOfWork.Financings.GetByIdAsync(payment.FinancingId.Value);
                if (financing != null && financing.Status == FinancingStatus.Active)
                {
                    decimal amortizationAmount = 0;

                    // Se o pagamento estiver vinculado a uma parcela específica
                    if (payment.FinancingInstallmentId.HasValue)
                    {
                        var installment = await _unitOfWork.FinancingInstallments.GetByIdAsync(payment.FinancingInstallmentId.Value);
                        if (installment != null)
                        {

                            amortizationAmount = installment.AmortizationAmount;

                            // Atualizar status e valores da parcela
                            await RevertPaymentFromInstallment(installment, payment);
                        }
                    }
                    else
                    {
                        // Para pagamentos não vinculados a parcelas específicas (pagamentos extras)
                        // Considerar o valor total como amortização
                        amortizationAmount = payment.Amount;
                    }

                    // Aumentar a dívida restante pelo valor da amortização
                    financing.RestoreRemainingDebt(amortizationAmount);
                    await _unitOfWork.Financings.UpdateAsync(financing);

                    // Recalcular parcelas futuras
                    var financingService = _serviceProvider.GetRequiredService<IFinancingService>();
                    await financingService.RecalculateRemainingInstallmentsAsync(financing.Id, payment.DueDate, true);
                }
            }

            payment.Cancel();
            await _unitOfWork.Payments.UpdateAsync(payment);
            await _unitOfWork.CompleteAsync();

            return _mapper.Map<PaymentDto>(payment);
        }

        // Método auxiliar para reverter os efeitos de um pagamento em uma parcela
        private async Task RevertPaymentFromInstallment(FinancingInstallment installment, Payment payment)
        {
            // Remover o valor do pagamento do valor pago
            decimal newPaidAmount = installment.PaidAmount - payment.Amount;

            // Recalcular o valor restante
            decimal newRemainingAmount = installment.TotalAmount - newPaidAmount;

            // Determinar o novo status com base nos valores de pagamento
            FinancingInstallmentStatus newStatus;
            if (newPaidAmount <= 0)
            {
                newPaidAmount = 0;
                newRemainingAmount = installment.TotalAmount;
                newStatus = FinancingInstallmentStatus.Pending;
            }
            else if (newRemainingAmount > 0)
            {
                newStatus = FinancingInstallmentStatus.PartiallyPaid;
            }
            else
            {
                newStatus = FinancingInstallmentStatus.Paid;
            }

            // Atualizar parcela
            installment.RevertPayment(newPaidAmount, newRemainingAmount, newStatus);
            await _unitOfWork.FinancingInstallments.UpdateAsync(installment);
        }
    }
}
