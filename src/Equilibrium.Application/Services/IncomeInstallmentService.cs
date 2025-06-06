﻿using AutoMapper;
using Equilibrium.Application.DTOs.IncomeInstallment;
using Equilibrium.Application.Interfaces;
using Equilibrium.Domain.Interfaces.Services;
using Equilibrium.Resources;

namespace Equilibrium.Application.Services
{
    public class IncomeInstallmentService : IIncomeInstallmentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public IncomeInstallmentService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IncomeInstallmentDto> GetByIdAsync(Guid id)
        {
            var installment = await _unitOfWork.IncomeInstallments.GetByIdAsync(id);
            return installment == null
                ? throw new KeyNotFoundException(ResourceFinanceApi.IncomeInstallment_NotFound)
                : _mapper.Map<IncomeInstallmentDto>(installment);
        }

        public async Task<IEnumerable<IncomeInstallmentDto>> GetByIncomeIdAsync(Guid incomeId)
        {
            var installments = await _unitOfWork.IncomeInstallments.GetInstallmentsByIncomeIdAsync(incomeId);
            return _mapper.Map<IEnumerable<IncomeInstallmentDto>>(installments);
        }

        public async Task<IEnumerable<IncomeInstallmentDto>> GetByDueDateAsync(Guid userId, DateTime startDate, DateTime endDate)
        {
            var installments = await _unitOfWork.IncomeInstallments.GetInstallmentsByDueDateAsync(userId, startDate, endDate);
            return _mapper.Map<IEnumerable<IncomeInstallmentDto>>(installments);
        }

        public async Task<IEnumerable<IncomeInstallmentDto>> GetPendingAsync(Guid userId)
        {
            var installments = await _unitOfWork.IncomeInstallments.GetPendingInstallmentsByUserIdAsync(userId);
            return _mapper.Map<IEnumerable<IncomeInstallmentDto>>(installments);
        }

        public async Task<IEnumerable<IncomeInstallmentDto>> GetReceivedAsync(Guid userId)
        {
            var installments = await _unitOfWork.IncomeInstallments.GetReceivedInstallmentsByUserIdAsync(userId);
            return _mapper.Map<IEnumerable<IncomeInstallmentDto>>(installments);
        }

        public async Task<IncomeInstallmentDto> MarkAsReceivedAsync(Guid id, DateTime? receivedDate = null)
        {
            var installment = await _unitOfWork.IncomeInstallments.GetByIdAsync(id) ?? throw new KeyNotFoundException(ResourceFinanceApi.IncomeInstallment_NotFound);
            installment.MarkAsReceived(receivedDate ?? DateTime.Now);
            await _unitOfWork.IncomeInstallments.UpdateAsync(installment);
            await _unitOfWork.CompleteAsync();

            return _mapper.Map<IncomeInstallmentDto>(installment);
        }

        public async Task<IncomeInstallmentDto> CancelAsync(Guid id)
        {
            var installment = await _unitOfWork.IncomeInstallments.GetByIdAsync(id) ?? throw new KeyNotFoundException(ResourceFinanceApi.IncomeInstallment_NotFound);
            installment.Cancel();
            await _unitOfWork.IncomeInstallments.UpdateAsync(installment);
            await _unitOfWork.CompleteAsync();

            return _mapper.Map<IncomeInstallmentDto>(installment);
        }
    }
}