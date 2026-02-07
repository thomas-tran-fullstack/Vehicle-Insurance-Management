using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleInsuranceAPI.Data;
using VehicleInsuranceAPI.Models;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace VehicleInsuranceAPI.Controllers
{
    [Route("api/company-expenses")]
    [ApiController]
    public class CompanyExpensesController : ControllerBase
    {
        private readonly VehicleInsuranceContext _context;

        public CompanyExpensesController(VehicleInsuranceContext context)
        {
            _context = context;
        }

        // GET: Lấy danh sách chi phí
        [HttpGet]
        public async Task<IActionResult> GetExpenses(int pageSize = 20, int pageNumber = 1)
        {
            try
            {
                var total = await _context.CompanyExpenses.CountAsync();
                var expenses = await _context.CompanyExpenses
                    .OrderByDescending(e => e.ExpenseDate)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return Ok(new { total, expenses });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error: " + ex.Message });
            }
        }

        // GET: Lấy chi phí theo ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetExpense(int id)
        {
            try
            {
                var expense = await _context.CompanyExpenses.FindAsync(id);
                if (expense == null)
                    return NotFound(new { message = "Expense not found" });

                return Ok(expense);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error: " + ex.Message });
            }
        }

        // POST: Tạo chi phí mới
        [HttpPost]
        public async Task<IActionResult> CreateExpense([FromBody] CreateExpenseRequest request)
        {
            if (request.Amount <= 0)
                return BadRequest(new { message = "Amount must be greater than 0" });

            try
            {
                var expense = new CompanyExpense
                {
                    ExpenseDate = request.ExpenseDate,
                    ExpenseType = request.ExpenseType,
                    Description = request.Description,
                    Amount = request.Amount
                };

                _context.CompanyExpenses.Add(expense);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetExpense), new { id = expense.ExpenseId }, expense);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error: " + ex.Message });
            }
        }

        // PUT: Cập nhật chi phí
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateExpense(int id, [FromBody] CreateExpenseRequest request)
        {
            try
            {
                var expense = await _context.CompanyExpenses.FindAsync(id);
                if (expense == null)
                    return NotFound(new { message = "Expense not found" });

                expense.ExpenseDate = request.ExpenseDate;
                expense.ExpenseType = request.ExpenseType;
                expense.Description = request.Description;
                expense.Amount = request.Amount;

                _context.CompanyExpenses.Update(expense);
                await _context.SaveChangesAsync();

                return Ok(expense);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error: " + ex.Message });
            }
        }

        // DELETE: Xóa chi phí
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteExpense(int id)
        {
            try
            {
                var expense = await _context.CompanyExpenses.FindAsync(id);
                if (expense == null)
                    return NotFound(new { message = "Expense not found" });

                _context.CompanyExpenses.Remove(expense);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Expense deleted" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error: " + ex.Message });
            }
        }

        // GET: Tóm tắt chi phí theo tháng
        [HttpGet("summary/monthly")]
        public async Task<IActionResult> GetMonthlySummary()
        {
            try
            {
                var summary = await _context.CompanyExpenses
                    .GroupBy(e => new { e.ExpenseDate!.Value.Year, e.ExpenseDate!.Value.Month })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        TotalAmount = g.Sum(e => e.Amount)
                    })
                    .OrderByDescending(x => x.Year)
                    .ThenByDescending(x => x.Month)
                    .ToListAsync();

                return Ok(summary);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error: " + ex.Message });
            }
        }

        // GET: Chi phí theo loại
        [HttpGet("summary/by-type")]
        public async Task<IActionResult> GetByType()
        {
            try
            {
                var summary = await _context.CompanyExpenses
                    .GroupBy(e => e.ExpenseType)
                    .Select(g => new
                    {
                        Type = g.Key,
                        TotalAmount = g.Sum(e => e.Amount),
                        Count = g.Count()
                    })
                    .ToListAsync();

                return Ok(summary);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error: " + ex.Message });
            }
        }
    }

    public class CreateExpenseRequest
    {
        public DateOnly? ExpenseDate { get; set; }
        public string? ExpenseType { get; set; }
        public string? Description { get; set; }
        public decimal? Amount { get; set; }
    }
}
