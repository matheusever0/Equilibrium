﻿using Equilibrium.API.Extensions;
using Equilibrium.Application.DTOs.Investment;
using Equilibrium.Application.Interfaces;
using Equilibrium.Domain.Enums;
using Equilibrium.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace Equilibrium.API.Controllers
{
    public class InvestmentsController : AuthenticatedController<IInvestmentService>
    {
        public InvestmentsController(IUnitOfWork unitOfWork, 
            IInvestmentService service) : base(unitOfWork, service)
        {
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<InvestmentDto>>> GetAll()
        {
            var userId = HttpContext.GetCurrentUserId();
            var investments = await _service.GetAllByUserIdAsync(userId);
            return Ok(investments);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<InvestmentDto>> GetById(Guid id)
        {
            try
            {
                var investment = await _service.GetByIdAsync(id);

                return investment.UserId != HttpContext.GetCurrentUserId() ? (ActionResult<InvestmentDto>)Forbid() : (ActionResult<InvestmentDto>)Ok(investment);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet("type/{type}")]
        public async Task<ActionResult<IEnumerable<InvestmentDto>>> GetByType(InvestmentType type)
        {
            var userId = HttpContext.GetCurrentUserId();
            var investments = await _service.GetByTypeAsync(userId, type);
            return Ok(investments);
        }

        [HttpPost]
        public async Task<ActionResult<InvestmentDto>> Create(CreateInvestmentDto createInvestmentDto)
        {
            try
            {
                var userId = HttpContext.GetCurrentUserId();
                var investment = await _service.CreateAsync(createInvestmentDto, userId);
                return CreatedAtAction(nameof(GetById), new { id = investment.Id }, investment);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id)
        {
            try
            {
                var investment = await _service.GetByIdAsync(id);
                if (investment.UserId != HttpContext.GetCurrentUserId())
                    return Forbid();

                await _service.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/refresh")]
        public async Task<ActionResult<InvestmentDto>> RefreshPrice(Guid id)
        {
            try
            {
                var investment = await _service.GetByIdAsync(id);
                if (investment.UserId != HttpContext.GetCurrentUserId())
                    return Forbid();

                var updatedInvestment = await _service.RefreshPriceAsync(id);
                return Ok(updatedInvestment);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("refresh-all")]
        public async Task<ActionResult<IEnumerable<InvestmentDto>>> RefreshAllPrices()
        {
            try
            {
                var userId = HttpContext.GetCurrentUserId();
                var updatedInvestments = await _service.RefreshAllPricesAsync(userId);
                return Ok(updatedInvestments);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
