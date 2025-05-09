using AutoMapper;
using Equilibrium.Application.DTOs.Investment;
using Equilibrium.Application.Interfaces;
using Equilibrium.Domain.Entities;
using Equilibrium.Domain.Enums;
using Equilibrium.Domain.Specifications;
using Equilibrium.Application.DTOs.Common;
using Equilibrium.Domain.DTOs.Filters;
using Equilibrium.Domain.Interfaces.Services;

namespace Equilibrium.Application.Services
{
    public class InvestmentService : IInvestmentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IStockPriceService _stockPriceService;

        public InvestmentService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IStockPriceService stockPriceService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _stockPriceService = stockPriceService;
        }

        public async Task<InvestmentDto> GetByIdAsync(Guid id)
        {
            var investment = await _unitOfWork.Investments.GetInvestmentWithTransactionsAsync(id);
            return investment == null ? throw new KeyNotFoundException("Investimento n�o encontrado") : _mapper.Map<InvestmentDto>(investment);
        }

        public async Task<IEnumerable<InvestmentDto>> GetAllByUserIdAsync(Guid userId)
        {
            var investments = await _unitOfWork.Investments.GetInvestmentsByUserIdAsync(userId);
            return _mapper.Map<IEnumerable<InvestmentDto>>(investments);
        }

        public async Task<IEnumerable<InvestmentDto>> GetByTypeAsync(Guid userId, InvestmentType type)
        {
            var investments = await _unitOfWork.Investments.GetInvestmentsByTypeAsync(userId, type);
            return _mapper.Map<IEnumerable<InvestmentDto>>(investments);
        }

        public async Task<InvestmentDto> CreateAsync(CreateInvestmentDto createInvestmentDto, Guid userId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId) ?? throw new KeyNotFoundException("Usu�rio n�o encontrado");
            var existingInvestment = await _unitOfWork.Investments.GetInvestmentBySymbolAsync(userId, createInvestmentDto.Symbol);
            if (existingInvestment != null)
                throw new InvalidOperationException("J� existe um investimento com este s�mbolo");

            var stockQuoteDto = await _stockPriceService.GetBatchQuoteAsync(createInvestmentDto.Symbol) ?? throw new ArgumentNullException($"N�o foi encontrado atualiza��es para: {createInvestmentDto.Symbol}");

            decimal totalInvested = createInvestmentDto.InitialQuantity * createInvestmentDto.InitialPrice;
            decimal currentTotal = createInvestmentDto.InitialQuantity * stockQuoteDto.Price;
            decimal gainLossValue = currentTotal - totalInvested;
            decimal gainLossPercentage = totalInvested > 0 ? (gainLossValue / totalInvested) * 100 : 0;

            var investment = new Investment(
                createInvestmentDto.Symbol,
                stockQuoteDto.ShortName,
                stockQuoteDto.Currency,
                createInvestmentDto.Type,
                createInvestmentDto.InitialQuantity,
                createInvestmentDto.InitialPrice,
                stockQuoteDto.Price,
                totalInvested,
                currentTotal,
                gainLossPercentage,
                gainLossValue,
                user
            );

            investment.AddTransaction(
                createInvestmentDto.TransactionDate,
                TransactionType.Buy,
                createInvestmentDto.InitialQuantity,
                createInvestmentDto.InitialPrice,
                totalInvested,
                0,
                createInvestmentDto.Broker,
                "Transa��o inicial",
                true
            );

            await _unitOfWork.Investments.AddAsync(investment);
            await _unitOfWork.CompleteAsync();

            return _mapper.Map<InvestmentDto>(investment);
        }

        public async Task DeleteAsync(Guid id)
        {
            var investment = await _unitOfWork.Investments.GetInvestmentWithTransactionsAsync(id) ?? throw new KeyNotFoundException("Investimento n�o encontrado");
            await _unitOfWork.Investments.DeleteAsync(investment);
            await _unitOfWork.CompleteAsync();
        }

        public async Task<InvestmentDto> RefreshPriceAsync(Guid id)
        {
            var investment = await _unitOfWork.Investments.GetInvestmentWithTransactionsAsync(id) ?? throw new KeyNotFoundException("Investimento n�o encontrado");

            var stockQuote = await _stockPriceService.GetBatchQuoteAsync(investment.Symbol) ?? throw new ArgumentNullException($"Erro ao atualizar pre�o, devido a n�o encontrar: {investment.Symbol}");
            investment.UpdateCurrentPrice(stockQuote.Price);

            await _unitOfWork.Investments.UpdateAsync(investment);
            await _unitOfWork.CompleteAsync();

            return _mapper.Map<InvestmentDto>(investment);
        }

        public async Task<IEnumerable<InvestmentDto>> RefreshAllPricesAsync(Guid userId)
        {
            var investments = await _unitOfWork.Investments.GetInvestmentsByUserIdAsync(userId);

            var symbols = investments.Select(i => i.Symbol).ToList();

            try
            {
                var quotes = await _stockPriceService.GetBatchQuotesAsync(symbols);


                foreach (var investment in investments)
                {
                    var quote = quotes.FirstOrDefault(q => q.Symbol.Equals(investment.Symbol, StringComparison.OrdinalIgnoreCase));
                    if (quote != null)
                    {
                        investment.UpdateInvestment(quote.Price, quote.ShortName);
                        await _unitOfWork.Investments.UpdateAsync(investment);
                    }
                }

                await _unitOfWork.CompleteAsync();


            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao atualizar pre�os: {ex.Message}");
            }

            return _mapper.Map<IEnumerable<InvestmentDto>>(investments);
        }

        public async Task<PagedResult<InvestmentDto>> GetFilteredAsync(InvestmentFilter filter, Guid userId)
        {
            var specification = new InvestmentSpecification(filter)
            {
                UserId = userId
            };

            var (investments, totalCount) = await _unitOfWork.Investments.FindWithSpecificationAsync(specification);

            return new PagedResult<InvestmentDto>
            {
                Items = _mapper.Map<IEnumerable<InvestmentDto>>(investments),
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };
        }
    }
}

