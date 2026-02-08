using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using VehicleInsuranceAPI.Data;
using VehicleInsuranceAPI.Models;

namespace VehicleInsuranceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private readonly VehicleInsuranceContext _context;

        public ReportController(VehicleInsuranceContext context)
        {
            _context = context;
        }

        // API 1: Lấy số liệu KPI tổng quan
        [HttpGet("kpi-summary")]
        public async Task<IActionResult> GetKpiSummary()
        {
            try
            {
                var today = DateOnly.FromDateTime(DateTime.Today);
                var startOfMonth = new DateOnly(today.Year, today.Month, 1);
                var startOfLastMonth = startOfMonth.AddMonths(-1);
                var startOfYear = new DateOnly(today.Year, 1, 1);

                // 1. DOANH THU
                var currentMonthRevenue = await _context.Policies
                    .Where(p => p.PolicyStartDate >= startOfMonth)
                    .SumAsync(p => (decimal?)p.PremiumAmount) ?? 0;

                var lastMonthRevenue = await _context.Policies
                    .Where(p => p.PolicyStartDate >= startOfLastMonth && p.PolicyStartDate < startOfMonth)
                    .SumAsync(p => (decimal?)p.PremiumAmount) ?? 0;

                // Tính % tăng trưởng
                double growthPercent = 0;
                if (lastMonthRevenue > 0)
                    growthPercent = (double)((currentMonthRevenue - lastMonthRevenue) / lastMonthRevenue) * 100;
                else if (currentMonthRevenue > 0)
                    growthPercent = 100;

                // 2. CHI TRẢ BỒI THƯỜNG
                var claimsPaid = await _context.Claims
                    .Where(c => c.Status == "PAID" || c.Status == "APPROVED")
                    .SumAsync(c => (decimal?)c.ClaimableAmount) ?? 0;

                var claimsCount = await _context.Claims
                    .CountAsync(c => c.Status == "APPROVED" || c.Status == "PAID");

                // 3. SỐ LƯỢNG HỢP ĐỒNG
                var activePolicies = await _context.Policies.CountAsync(p => p.Status == "ACTIVE");
                var newPolicies = await _context.Policies.CountAsync(p => p.PolicyStartDate >= startOfMonth);

                // 4. LOSS RATIO
                var totalYearRevenue = await _context.Policies
                    .Where(p => p.PolicyStartDate >= startOfYear)
                    .SumAsync(p => (decimal?)p.PremiumAmount) ?? 0;

                double lossRatio = totalYearRevenue > 0
                    ? (double)(claimsPaid / totalYearRevenue) * 100
                    : 0;

                return Ok(new
                {
                    totalRevenue = currentMonthRevenue,
                    revenueGrowth = Math.Round(growthPercent, 1),
                    totalClaimsPaid = claimsPaid,
                    totalClaims = claimsCount,
                    lossRatio = Math.Round(lossRatio, 1),
                    totalPolicies = activePolicies,
                    newPolicies = newPolicies
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error: " + ex.Message });
            }
        }

        // API 2: Lấy dữ liệu biểu đồ 6 tháng
        [HttpGet("chart-data")]
        public async Task<IActionResult> GetChartData()
        {
            try
            {
                var today = DateOnly.FromDateTime(DateTime.Today);
                var sixMonthsAgo = today.AddMonths(-5);
                var startOfPeriod = new DateOnly(sixMonthsAgo.Year, sixMonthsAgo.Month, 1);

                // Group theo tháng
                var revenueData = await _context.Policies
                    .Where(p => p.PolicyStartDate.HasValue && p.PolicyStartDate >= startOfPeriod)
                    .GroupBy(p => new { p.PolicyStartDate!.Value.Year, p.PolicyStartDate!.Value.Month })
                    .Select(g => new { Year = g.Key.Year, Month = g.Key.Month, Total = g.Sum(p => p.PremiumAmount) })
                    .ToListAsync();

                var expenseData = await _context.Claims
                    .Where(c => c.AccidentDate.HasValue && c.AccidentDate >= startOfPeriod && (c.Status == "PAID" || c.Status == "APPROVED"))
                    .GroupBy(c => new { c.AccidentDate!.Value.Year, c.AccidentDate!.Value.Month })
                    .Select(g => new { Year = g.Key.Year, Month = g.Key.Month, Total = g.Sum(c => c.ClaimableAmount) })
                    .ToListAsync();

                // Chuẩn hóa dữ liệu trả về
                var labels = new List<string>();
                var dataRevenue = new List<decimal>();
                var dataExpense = new List<decimal>();

                for (int i = 0; i < 6; i++)
                {
                    var d = startOfPeriod.AddMonths(i);
                    labels.Add($"T{d.Month}/{d.Year}");

                    var rev = revenueData.FirstOrDefault(x => x.Year == d.Year && x.Month == d.Month);
                    dataRevenue.Add(rev?.Total ?? 0);

                    var exp = expenseData.FirstOrDefault(x => x.Year == d.Year && x.Month == d.Month);
                    dataExpense.Add(exp?.Total ?? 0);
                }

                return Ok(new { labels, revenue = dataRevenue, expense = dataExpense });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error: " + ex.Message });
            }
        }
    }
}
