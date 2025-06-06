﻿using Equilibrium.Domain.Entities;
using Equilibrium.Domain.Interfaces.Repositories;
using Equilibrium.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Equilibrium.Infrastructure.Repositories
{
    public class InvestmentTransactionRepository : RepositoryBase<InvestmentTransaction>, IInvestmentTransactionRepository
    {
        public InvestmentTransactionRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<InvestmentTransaction>> GetTransactionsByInvestmentIdAsync(Guid investmentId)
        {
            return await _dbSet
                .Where(t => t.InvestmentId == investmentId)
                .OrderByDescending(t => t.Date)
                .ToListAsync();
        }

        public async Task<IEnumerable<InvestmentTransaction>> GetTransactionsByDateRangeAsync(Guid userId, DateTime startDate, DateTime endDate)
        {
            return await _dbSet
                .Include(t => t.Investment)
                .Where(t => t.Investment.UserId == userId &&
                             t.Date >= startDate &&
                             t.Date <= endDate)
                .OrderByDescending(t => t.Date)
                .ToListAsync();
        }
    }
}
