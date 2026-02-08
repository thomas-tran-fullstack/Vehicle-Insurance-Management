using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleInsuranceAPI.Data;
using VehicleInsuranceAPI.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace VehicleInsuranceAPI.Backend.VehicleInspectionDispatch
{
    [ApiController]
    [Route("api/inspections")]
    public class VehicleInspectionDispatchController : ControllerBase
    {
        private readonly VehicleInsuranceContext _context;

        public VehicleInspectionDispatchController(VehicleInsuranceContext context)
        {
            _context = context;
        }

        // GET /api/inspections/by-claim/{claimId}
        [HttpGet("by-claim/{claimId}")]
        public async Task<IActionResult> GetInspectionByClaim(int claimId)
        {
            try
            {
                var inspection = await _context.VehicleInspections
                    .Where(v => v.ClaimId == claimId)
                    .Include(v => v.Vehicle)
                    .Include(v => v.Staff)
                    .Include(v => v.VerifiedByStaff)
                    .FirstOrDefaultAsync();

                if (inspection == null)
                    return NotFound(new { success = false, message = "Inspection not found" });

                return Ok(new { success = true, data = inspection });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // GET /api/inspections/by-vehicle/{vehicleId}
        [HttpGet("by-vehicle/{vehicleId}")]
        public async Task<IActionResult> GetInspectionsByVehicle(int vehicleId)
        {
            try
            {
                var inspections = await _context.VehicleInspections
                    .Where(v => v.VehicleId == vehicleId)
                    .Include(v => v.Vehicle)
                    .Include(v => v.Staff)
                    .Include(v => v.VerifiedByStaff)
                    .OrderByDescending(v => v.ScheduledDate)
                    .ToListAsync();

                return Ok(new { success = true, data = inspections });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // GET /api/inspections/by-user/{userId}
        [HttpGet("by-user/{userId:int}")]
        public async Task<IActionResult> GetInspectionsByUser(int userId)
        {
            try
            {
                var inspections = await _context.VehicleInspections
                    .Where(v => v.Vehicle != null && v.Vehicle.Customer != null && v.Vehicle.Customer.UserId == userId)
                    .Select(v => new
                    {
                        v.InspectionId,
                        v.ClaimId,
                        v.AssignedStaffId,
                        v.VerifiedByStaffId,
                        VehicleName = v.Vehicle != null ? v.Vehicle.VehicleName : "N/A",
                        VehicleNumber = v.Vehicle != null ? v.Vehicle.VehicleNumber : "N/A",
                        v.InspectionLocation,
                        v.Status,
                        v.OverallAssessment,
                        v.ConfirmedCorrect,
                        v.ScheduledDate,
                        v.CompletedDate,
                        StaffName = v.Staff != null ? v.Staff.FullName : "Unassigned",
                        VerifiedByStaffName = v.VerifiedByStaff != null ? v.VerifiedByStaff.FullName : null
                    })
                    .OrderByDescending(v => v.ScheduledDate)
                    .ToListAsync();

                return Ok(new { success = true, data = inspections });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // GET /api/inspections/by-customer/{customerId}
        [HttpGet("by-customer/{customerId}")]
        public async Task<IActionResult> GetInspectionsByCustomer(int customerId)
        {
            try
            {
                var inspections = await _context.VehicleInspections
                    .Where(v => v.Vehicle != null && v.Vehicle.CustomerId == customerId)
                    .Include(v => v.Vehicle)
                    .Include(v => v.Staff)
                    .Include(v => v.VerifiedByStaff)
                    .OrderByDescending(v => v.ScheduledDate)
                    .ToListAsync();

                return Ok(new { success = true, data = inspections });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // GET /api/inspections/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetInspectionDetail(int id)
        {
            try
            {
                var inspection = await _context.VehicleInspections
                    .Where(v => v.InspectionId == id)
                    .Select(v => new
                    {
                        v.InspectionId,
                        v.ClaimId,
                        v.AssignedStaffId,
                        v.VerifiedByStaffId,
                        VehicleName = v.Vehicle != null ? v.Vehicle.VehicleName : "N/A",
                        VehicleNumber = v.Vehicle != null ? v.Vehicle.VehicleNumber : "N/A",
                        v.InspectionLocation,
                        v.Status,
                        v.OverallAssessment,
                        v.ConfirmedCorrect,
                        v.ScheduledDate,
                        v.CompletedDate,
                        StaffName = v.Staff != null ? v.Staff.FullName : "Unassigned",
                        VerifiedByStaffName = v.VerifiedByStaff != null ? v.VerifiedByStaff.FullName : null
                    })
                    .FirstOrDefaultAsync();

                if (inspection == null)
                    return NotFound(new { success = false, message = "Inspection not found" });

                return Ok(new { success = true, data = inspection });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // POST /api/inspections/create
        [HttpPost("create")]
        public async Task<IActionResult> CreateInspection([FromBody] InspectionCreateDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest(new { success = false, message = "Invalid request body" });

                Console.WriteLine($"[INSPECTION DEBUG] Creating inspection - VehicleId: {dto.VehicleId}, ClaimId: {dto.ClaimId}, StaffId: {dto.AssignedStaffId}");

                // Validate vehicle exists
                var vehicle = await _context.Vehicles.FindAsync(dto.VehicleId);
                if (vehicle == null)
                    return BadRequest(new { success = false, message = "Vehicle not found" });

                // Validate claim exists and is approved
                Claim claim = null;
                if (dto.ClaimId.HasValue)
                {
                    claim = await _context.Claims.FindAsync(dto.ClaimId);
                    if (claim == null)
                        return BadRequest(new { success = false, message = "Claim not found" });

                    if (claim.Status != "APPROVED")
                        return BadRequest(new { success = false, message = "Can only create inspection for approved claims" });
                }

                // Validate assigned staff exists
                var staff = await _context.Staff.FindAsync(dto.AssignedStaffId);
                if (staff == null)
                    return BadRequest(new { success = false, message = "Staff not found" });

                var inspection = new VehicleInspection
                {
                    VehicleId = dto.VehicleId,
                    ClaimId = dto.ClaimId,
                    AssignedStaffId = dto.AssignedStaffId,
                    ScheduledDate = dto.ScheduledDate,
                    InspectionLocation = dto.InspectionLocation,
                    Status = "SCHEDULED"
                };

                _context.VehicleInspections.Add(inspection);
                await _context.SaveChangesAsync();

                Console.WriteLine($"[INSPECTION SUCCESS] Inspection #{inspection.InspectionId} created");

                // Return simplified response to avoid serialization issues
                return Ok(new { 
                    success = true, 
                    message = "Inspection created and scheduled", 
                    data = new {
                        inspectionId = inspection.InspectionId,
                        vehicleId = inspection.VehicleId,
                        claimId = inspection.ClaimId,
                        status = inspection.Status,
                        scheduledDate = inspection.ScheduledDate,
                        inspectionLocation = inspection.InspectionLocation
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[INSPECTION ERROR] {ex.Message}");
                Console.WriteLine($"[INSPECTION ERROR] StackTrace: {ex.StackTrace}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // PUT /api/inspections/{id}/start
        [HttpPut("{id}/start")]
        public async Task<IActionResult> StartInspection(int id)
        {
            try
            {
                var inspection = await _context.VehicleInspections.FindAsync(id);
                if (inspection == null)
                    return NotFound(new { success = false, message = "Inspection not found" });

                inspection.Status = "IN_PROGRESS";

                _context.VehicleInspections.Update(inspection);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Inspection started", data = inspection });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // PUT /api/inspections/{id}/complete
        [HttpPut("{id}/complete")]
        public async Task<IActionResult> CompleteInspection(int id, [FromBody] InspectionCompleteDto dto)
        {
            try
            {
                var inspection = await _context.VehicleInspections.FindAsync(id);
                if (inspection == null)
                    return NotFound(new { success = false, message = "Inspection not found" });

                inspection.Status = "COMPLETED";
                inspection.CompletedDate = DateTime.Now;
                inspection.OverallAssessment = dto.OverallAssessment;
                inspection.ConfirmedCorrect = dto.ConfirmedCorrect;
                inspection.DocumentPath = dto.DocumentPath;
                inspection.Result = dto.Result;

                _context.VehicleInspections.Update(inspection);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Inspection completed and submitted for verification", data = inspection });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // PUT /api/inspections/{id}/verify
        [HttpPut("{id}/verify")]
        public async Task<IActionResult> VerifyInspection(int id, [FromBody] InspectionVerifyDto dto)
        {
            try
            {
                var inspection = await _context.VehicleInspections.FindAsync(id);
                if (inspection == null)
                    return NotFound(new { success = false, message = "Inspection not found" });

                if (inspection.Status != "COMPLETED")
                    return BadRequest(new { success = false, message = "Only completed inspections can be verified" });

                inspection.Status = "VERIFIED";
                inspection.VerifiedByStaffId = dto.VerifiedByStaffId;
                inspection.VerifiedAt = DateTime.Now;

                _context.VehicleInspections.Update(inspection);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Inspection verified", data = inspection });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // GET /api/inspections/pending/all
        [HttpGet("pending/all")]
        public async Task<IActionResult> GetAllPendingInspections()
        {
            try
            {
                var inspections = await _context.VehicleInspections
                    .Where(v => v.Status == "SCHEDULED" || v.Status == "IN_PROGRESS" || v.Status == "COMPLETED")
                    .Select(v => new
                    {
                        v.InspectionId,
                        v.VehicleId,
                        v.ClaimId,
                        v.AssignedStaffId,
                        v.ScheduledDate,
                        v.CompletedDate,
                        v.Status,
                        v.Result,
                        v.InspectionLocation,
                        v.OverallAssessment,
                        v.ConfirmedCorrect,
                        v.VerifiedByStaffId,
                        v.VerifiedAt,
                        VehicleName = v.Vehicle != null ? v.Vehicle.VehicleType + " " + v.Vehicle.VehicleName + " (" + v.Vehicle.VehicleNumber + ")" : "N/A",
                        StaffName = v.Staff != null ? v.Staff.FullName : "Unassigned"
                    })
                    .OrderByDescending(v => v.ScheduledDate)
                    .ToListAsync();

                return Ok(new { success = true, data = inspections });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // GET /api/inspections/assigned/{staffId}
        [HttpGet("assigned/{staffId}")]
        public async Task<IActionResult> GetAssignedInspections(int staffId)
        {
            try
            {
                var inspections = await _context.VehicleInspections
                    .Where(v => v.AssignedStaffId == staffId)
                    .Include(v => v.Vehicle)
                    .Include(v => v.Staff)
                    .Include(v => v.VerifiedByStaff)
                    .OrderByDescending(v => v.ScheduledDate)
                    .ToListAsync();

                return Ok(new { success = true, data = inspections });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // GET /api/inspections/demo/info
        [HttpGet("demo/info")]
        public IActionResult Demo()
        {
            return Ok(new
            {
                message = "Vehicle Inspection Dispatch API",
                endpoints = new[] {
                    "GET /api/inspections/by-claim/{claimId}",
                    "GET /api/inspections/by-vehicle/{vehicleId}",
                    "GET /api/inspections/by-customer/{customerId}",
                    "GET /api/inspections/{id}",
                    "POST /api/inspections/create",
                    "PUT /api/inspections/{id}/start",
                    "PUT /api/inspections/{id}/complete",
                    "PUT /api/inspections/{id}/verify",
                    "GET /api/inspections/pending/all",
                    "GET /api/inspections/assigned/{staffId}"
                }
            });
        }
    }

    // DTOs
    public class InspectionCreateDto
    {
        public int VehicleId { get; set; }
        public int? ClaimId { get; set; }
        public int AssignedStaffId { get; set; }
        public DateTime ScheduledDate { get; set; }
        public string InspectionLocation { get; set; }
    }

    public class InspectionCompleteDto
    {
        public string OverallAssessment { get; set; }
        public bool? ConfirmedCorrect { get; set; }
        public string DocumentPath { get; set; }
        public string Result { get; set; }
    }

    public class InspectionVerifyDto
    {
        public int VerifiedByStaffId { get; set; }
    }
}
