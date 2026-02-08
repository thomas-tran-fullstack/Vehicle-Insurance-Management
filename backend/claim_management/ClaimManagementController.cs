using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleInsuranceAPI.Data;
using VehicleInsuranceAPI.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace VehicleInsuranceAPI.Backend.ClaimManagement
{
    [ApiController]
    [Route("api/claims")]
    public class ClaimManagementController : ControllerBase
    {
        private readonly VehicleInsuranceContext _context;

        public ClaimManagementController(VehicleInsuranceContext context)
        {
            _context = context;
        }

        // GET /api/claims/by-customer/{customerId}
        [HttpGet("by-customer/{customerId}")]
        public async Task<IActionResult> GetClaimsByCustomer(int customerId)
        {
            try
            {
                var claims = await _context.Claims
                    .Where(c => c.Policy != null && c.Policy.CustomerId == customerId)
                    .Select(c => new
                    {
                        c.ClaimId,
                        c.ClaimNumber,
                        c.PolicyId,
                        PolicyNumber = c.Policy.PolicyNumber,
                        c.AccidentDate,
                        c.Description,
                        c.Status,
                        c.SubmittedAt,
                        ReviewedByStaffId = c.ReviewedByStaffId,
                        ReviewedByStaffName = c.ReviewedByStaff == null ? null : c.ReviewedByStaff.FullName,
                        ApprovedByStaffId = c.ApprovedByStaffId,
                        ApprovedByStaffName = c.ApprovedByStaff == null ? null : c.ApprovedByStaff.FullName,
                        c.ReviewNote,
                        c.ClaimableAmount
                    })
                    .OrderByDescending(c => c.SubmittedAt)
                    .ToListAsync();

                return Ok(new { success = true, data = claims });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // GET /api/claims/by-user/{userId}
        [HttpGet("by-user/{userId:int}")]
        public async Task<IActionResult> GetClaimsByUser(int userId)
        {
            try
            {
                var claims = await _context.Claims
                    .Where(c => c.Policy != null && c.Policy.Customer.UserId == userId)
                    .Select(c => new
                    {
                        c.ClaimId,
                        c.ClaimNumber,
                        c.PolicyId,
                        PolicyNumber = c.Policy.PolicyNumber,
                        c.AccidentDate,
                        c.Description,
                        c.Status,
                        c.SubmittedAt,
                        ReviewedByStaffId = c.ReviewedByStaffId,
                        ReviewedByStaffName = c.ReviewedByStaff == null ? null : c.ReviewedByStaff.FullName,
                        ApprovedByStaffId = c.ApprovedByStaffId,
                        ApprovedByStaffName = c.ApprovedByStaff == null ? null : c.ApprovedByStaff.FullName,
                        c.ReviewNote,
                        c.ClaimableAmount
                    })
                    .OrderByDescending(c => c.SubmittedAt)
                    .ToListAsync();

                return Ok(new { success = true, data = claims });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // GET /api/claims/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetClaimDetail(int id)
        {
            try
            {
                var claim = await _context.Claims
                    .Include(c => c.Policy)
                    .Include(c => c.ReviewedByStaff)
                    .Include(c => c.ApprovedByStaff)
                    .FirstOrDefaultAsync(c => c.ClaimId == id);

                if (claim == null)
                    return NotFound(new { success = false, message = "Claim not found" });

                return Ok(new { success = true, data = claim });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // POST /api/claims/create
        [HttpPost("create")]
        public async Task<IActionResult> CreateClaim([FromBody] ClaimCreateDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest(new { success = false, message = "Invalid request body" });

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    return BadRequest(new { success = false, message = "Validation failed", errors = errors });
                }

                Console.WriteLine($"[CLAIM DEBUG] Creating claim for policy {dto.PolicyId}, accident date: {dto.AccidentDate}");

                var policy = await _context.Policies.FindAsync(dto.PolicyId);
                if (policy == null)
                    return BadRequest(new { success = false, message = "Policy not found" });

                // Validation: Policy must be ACTIVE
                if (policy.Status != "ACTIVE")
                    return BadRequest(new { success = false, message = $"Policy is {policy.Status}. Only ACTIVE policies can create claims." });

                // Validation: Accident date must be within past 5 days and not in future
                var today = DateOnly.FromDateTime(DateTime.Now);
                var fiveDaysAgo = today.AddDays(-5);
                var accidentDate = DateOnly.FromDateTime(dto.AccidentDate);

                Console.WriteLine($"[CLAIM DEBUG] Today: {today}, Five days ago: {fiveDaysAgo}, Accident date: {accidentDate}");
                Console.WriteLine($"[CLAIM DEBUG] Policy dates - Start: {policy.PolicyStartDate}, End: {policy.PolicyEndDate}");

                if (accidentDate > today)
                    return BadRequest(new { success = false, message = "Accident date cannot be in the future" });

                if (accidentDate < fiveDaysAgo)
                    return BadRequest(new { success = false, message = "Claim must be submitted within 5 days of accident" });

                // Validation: Accident date must be within policy valid period if dates exist
                // If policy dates are missing, we skip this validation
                if (policy.PolicyStartDate != default && policy.PolicyEndDate != default)
                {
                    if (accidentDate < policy.PolicyStartDate)
                        return BadRequest(new { success = false, message = $"Accident date ({accidentDate}) is before policy start date ({policy.PolicyStartDate})" });
                    
                    if (accidentDate > policy.PolicyEndDate)
                        return BadRequest(new { success = false, message = $"Accident date ({accidentDate}) is after policy end date ({policy.PolicyEndDate})" });
                }

                // Get next claim number
                var maxClaimNumber = await _context.Claims
                    .OrderByDescending(c => c.ClaimNumber)
                    .FirstOrDefaultAsync();
                var nextClaimNumber = (maxClaimNumber?.ClaimNumber ?? 2026300000) + 1;

                var claim = new Claim
                {
                    ClaimNumber = nextClaimNumber,
                    PolicyId = dto.PolicyId,
                    CustomerNameSnapshot = policy.CustomerNameSnapshot,
                    PolicyStartDateSnapshot = policy.PolicyStartDate,
                    PolicyEndDateSnapshot = policy.PolicyEndDate,
                    AccidentPlace = dto.AccidentPlace,
                    AccidentDate = accidentDate,
                    Description = dto.Description,
                    DocumentPath = dto.DocumentPath,
                    InsuredAmount = policy.PremiumAmount ?? 0, // Use premium as insured amount
                    ClaimableAmount = dto.ClaimableAmount ?? 0,
                    Status = "SUBMITTED",
                    SubmittedAt = DateTime.Now
                };

                _context.Claims.Add(claim);
                await _context.SaveChangesAsync();

                Console.WriteLine($"[CLAIM SUCCESS] Claim #{claim.ClaimNumber} created successfully for policy {claim.PolicyId}");

                // Return simplified response to avoid serialization issues
                return Ok(new { 
                    success = true, 
                    message = "Claim created successfully", 
                    data = new { 
                        claimId = claim.ClaimId,
                        claimNumber = claim.ClaimNumber,
                        status = claim.Status,
                        submittedAt = claim.SubmittedAt
                    } 
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CLAIM ERROR] {ex.Message}");
                Console.WriteLine($"[CLAIM ERROR] StackTrace: {ex.StackTrace}");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // PUT /api/claims/{id}/review
        [HttpPut("{id}/review")]
        public async Task<IActionResult> ReviewClaim(int id, [FromBody] ClaimReviewDto dto)
        {
            try
            {
                var claim = await _context.Claims.FindAsync(id);
                if (claim == null)
                    return NotFound(new { success = false, message = "Claim not found" });

                claim.Status = "UNDER_REVIEW";
                claim.ReviewedByStaffId = dto.ReviewedByStaffId;
                claim.ReviewedAt = DateTime.Now;
                claim.ReviewNote = dto.ReviewNote;

                _context.Claims.Update(claim);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Claim moved to Under Review", data = claim });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // PUT /api/claims/{id}/request-info
        [HttpPut("{id}/request-info")]
        public async Task<IActionResult> RequestMoreInfo(int id, [FromBody] ClaimRequestInfoDto dto)
        {
            try
            {
                var claim = await _context.Claims.FindAsync(id);
                if (claim == null)
                    return NotFound(new { success = false, message = "Claim not found" });

                claim.Status = "REQUEST_MORE_INFO";
                claim.ReviewNote = dto.Message;

                _context.Claims.Update(claim);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Request for more info sent", data = claim });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // PUT /api/claims/{id}/approve
        [HttpPut("{id}/approve")]
        public async Task<IActionResult> ApproveClaim(int id, [FromBody] ClaimDecisionDto dto)
        {
            try
            {
                var claim = await _context.Claims.FindAsync(id);
                if (claim == null)
                    return NotFound(new { success = false, message = "Claim not found" });

                // Validate claimable amount
                if (dto.ClaimableAmount > claim.InsuredAmount)
                    return BadRequest(new { success = false, message = "Claimable amount cannot exceed insured amount" });

                claim.Status = "APPROVED";
                claim.ApprovedByStaffId = dto.ApprovedByStaffId;
                claim.DecisionAt = DateTime.Now;
                claim.DecisionNote = dto.DecisionNote;
                claim.ClaimableAmount = dto.ClaimableAmount;

                _context.Claims.Update(claim);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Claim approved successfully", data = claim });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // PUT /api/claims/{id}/reject
        [HttpPut("{id}/reject")]
        public async Task<IActionResult> RejectClaim(int id, [FromBody] ClaimDecisionDto dto)
        {
            try
            {
                var claim = await _context.Claims.FindAsync(id);
                if (claim == null)
                    return NotFound(new { success = false, message = "Claim not found" });

                claim.Status = "REJECTED";
                claim.ApprovedByStaffId = dto.ApprovedByStaffId;
                claim.DecisionAt = DateTime.Now;
                claim.DecisionNote = dto.DecisionNote;

                _context.Claims.Update(claim);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Claim rejected", data = claim });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // PUT /api/claims/{id}/pay
        [HttpPut("{id}/pay")]
        public async Task<IActionResult> PayClaim(int id)
        {
            try
            {
                var claim = await _context.Claims.FindAsync(id);
                if (claim == null)
                    return NotFound(new { success = false, message = "Claim not found" });

                if (claim.Status != "APPROVED")
                    return BadRequest(new { success = false, message = "Only approved claims can be paid" });

                claim.Status = "PAID";
                claim.PaidAt = DateTime.Now;

                _context.Claims.Update(claim);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Claim paid successfully", data = claim });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // GET /api/claims/pending/all
        [HttpGet("pending/all")]
        public async Task<IActionResult> GetAllPendingClaims()
        {
            try
            {
                var claims = await _context.Claims
                    .Where(c => c.Status == "SUBMITTED" || c.Status == "UNDER_REVIEW" || c.Status == "REQUEST_MORE_INFO" || c.Status == "APPROVED")
                    .Select(c => new
                    {
                        c.ClaimId,
                        c.ClaimNumber,
                        c.PolicyId,
                        VehicleId = c.Policy != null ? c.Policy.VehicleId : 0,
                        c.Status,
                        c.SubmittedAt,
                        c.AccidentPlace,
                        c.AccidentDate,
                        c.InsuredAmount,
                        c.ClaimableAmount,
                        c.Description,
                        c.CustomerNameSnapshot,
                        c.ReviewedByStaffId,
                        c.ApprovedByStaffId,
                        PolicyNumber = c.Policy != null ? c.Policy.PolicyNumber.ToString() : "N/A",
                        VehicleName = c.Policy != null ? c.Policy.VehicleNameSnapshot : "N/A",
                        ReviewedByName = c.ReviewedByStaff != null ? c.ReviewedByStaff.FullName : null
                    })
                    .OrderByDescending(c => c.SubmittedAt)
                    .ToListAsync();

                return Ok(new { success = true, data = claims });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // GET /api/claims/demo
        [HttpGet("demo/info")]
        public IActionResult Demo()
        {
            return Ok(new { 
                message = "Claim Management API",
                endpoints = new[] {
                    "GET /api/claims/by-customer/{customerId}",
                    "GET /api/claims/{id}",
                    "POST /api/claims/create",
                    "PUT /api/claims/{id}/review",
                    "PUT /api/claims/{id}/request-info",
                    "PUT /api/claims/{id}/approve",
                    "PUT /api/claims/{id}/reject",
                    "PUT /api/claims/{id}/pay",
                    "GET /api/claims/pending/all"
                }
            });
        }
    }

    // DTOs
    public class ClaimCreateDto
    {
        public int PolicyId { get; set; }
        public string AccidentPlace { get; set; }
        public DateTime AccidentDate { get; set; }
        public string Description { get; set; }
        public string? DocumentPath { get; set; }
        public decimal? ClaimableAmount { get; set; }
    }

    public class ClaimReviewDto
    {
        public int ReviewedByStaffId { get; set; }
        public string ReviewNote { get; set; }
    }

    public class ClaimRequestInfoDto
    {
        public string Message { get; set; }
    }

    public class ClaimDecisionDto
    {
        public int ApprovedByStaffId { get; set; }
        public string DecisionNote { get; set; }
        public decimal ClaimableAmount { get; set; }
    }
}
