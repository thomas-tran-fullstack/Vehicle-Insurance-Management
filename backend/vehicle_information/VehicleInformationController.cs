using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleInsuranceAPI.Data;
using VehicleInsuranceAPI.Models;
using System.Security.Claims;
using OfficeOpenXml;

namespace VehicleInsuranceAPI.Backend.VehicleInformation
{
    [ApiController]
    [Route("api/[controller]")]
    public class VehicleInformationController : ControllerBase
    {
        private readonly VehicleInsuranceContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly ILogger<VehicleInformationController> _logger;

        public VehicleInformationController(
            VehicleInsuranceContext context,
            IWebHostEnvironment hostEnvironment,
            ILogger<VehicleInformationController> logger)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
            _logger = logger;
        }

        // GET: api/vehicleinformation/models - Get all vehicle models
        [HttpGet("models")]
        public async Task<ActionResult<object>> GetVehicleModels()
        {
            try
            {
                var models = await _context.VehicleModels
                    .OrderBy(m => m.VehicleType)
                    .ThenBy(m => m.ModelName)
                    .Select(m => new
                    {
                        m.ModelId,
                        m.ModelName,
                        m.VehicleClass,
                        m.VehicleType,
                        m.Description
                    })
                    .ToListAsync();

                return Ok(new { success = true, data = models });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting vehicle models: {ex.Message}");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        // GET: api/vehicleinformation/customer - Get vehicles of current customer
        [HttpGet("customer")]
        public async Task<ActionResult<IEnumerable<object>>> GetCustomerVehicles()
        {
            try
            {
                // Get current customer - try JWT claim first, then fallback to request header
                int userId = 0;
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int claimUserId))
                {
                    userId = claimUserId;
                }
                else if (Request.Headers.TryGetValue("X-User-Id", out var userIdHeader) && 
                         int.TryParse(userIdHeader.ToString(), out int headerUserId))
                {
                    userId = headerUserId;
                }
                
                if (userId == 0)
                    return Unauthorized(new { success = false, message = "User not authenticated" });

                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (customer == null)
                    return NotFound(new { success = false, message = "Customer not found" });

                var vehicles = await _context.Vehicles
                    .Where(v => v.CustomerId == customer.CustomerId)
                    .OrderByDescending(v => v.CreatedDate)
                    .Select(v => new
                    {
                        v.VehicleId,
                        v.VehicleName,
                        v.VehicleType,
                        v.VehicleBrand,
                        v.VehicleSegment,
                        v.VehicleVersion,
                        v.VehicleRate,
                        v.BodyNumber,
                        v.EngineNumber,
                        v.VehicleNumber,
                        v.RegistrationDate,
                        v.SeatCount,
                        v.VehicleImage,
                        v.ManufactureYear,
                        v.ModelId,
                        v.VehicleModelName,
                        ModelName = v.Model != null ? v.Model.ModelName : null,
                        VehicleClass = v.Model != null ? v.Model.VehicleClass : null,
                        v.CreatedDate,
                        v.UpdatedDate
                    })
                    .ToListAsync();

                return Ok(new { success = true, data = vehicles });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting customer vehicles: {ex.Message}");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        // GET: api/vehicleinformation/all - Get all vehicles (Admin)
        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<object>>> GetAllVehicles([FromQuery] string? brand, [FromQuery] string? segment, [FromQuery] int? seatCount, [FromQuery] string? search)
        {
            try
            {
                var query = _context.Vehicles
                    .Include(v => v.Model)
                    .Include(v => v.Customer)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(brand))
                    query = query.Where(v => v.VehicleBrand != null && v.VehicleBrand.ToLower().Contains(brand.ToLower()));

                if (!string.IsNullOrEmpty(segment))
                    query = query.Where(v => v.VehicleSegment != null && v.VehicleSegment.ToLower().Contains(segment.ToLower()));

                if (seatCount.HasValue)
                    query = query.Where(v => v.SeatCount == seatCount);

                // Apply search
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(v =>
                        (v.VehicleName != null && v.VehicleName.ToLower().Contains(search.ToLower())) ||
                        (v.VehicleNumber != null && v.VehicleNumber.ToLower().Contains(search.ToLower())) ||
                        (v.BodyNumber != null && v.BodyNumber.ToLower().Contains(search.ToLower())) ||
                        (v.EngineNumber != null && v.EngineNumber.ToLower().Contains(search.ToLower())) ||
                        (v.Customer != null && v.Customer.CustomerName != null && v.Customer.CustomerName.ToLower().Contains(search.ToLower()))
                    );

                var vehicles = await query
                    .OrderByDescending(v => v.CreatedDate)
                    .Select(v => new
                    {
                        v.VehicleId,
                        v.VehicleName,
                        v.VehicleType,
                        v.VehicleBrand,
                        v.VehicleSegment,
                        v.VehicleVersion,
                        v.VehicleRate,
                        v.BodyNumber,
                        v.EngineNumber,
                        v.VehicleNumber,
                        v.RegistrationDate,
                        v.SeatCount,
                        v.VehicleImage,
                        v.ManufactureYear,
                        ModelName = v.Model != null ? v.Model.ModelName : null,
                        CustomerName = v.Customer != null ? v.Customer.CustomerName : null,
                        CustomerId = v.Customer != null ? v.Customer.CustomerId : 0,
                        v.CreatedDate,
                        v.UpdatedDate
                    })
                    .ToListAsync();

                return Ok(new { success = true, data = vehicles });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting all vehicles: {ex.Message}");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        // GET: api/vehicleinformation/5 - Get vehicle detail
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetVehicleDetail(int id)
        {
            try
            {
                var vehicle = await _context.Vehicles
                    .Include(v => v.Model)
                    .Include(v => v.Customer)
                    .ThenInclude(c => c.User)
                    .Include(v => v.Policies)
                    .FirstOrDefaultAsync(v => v.VehicleId == id);

                if (vehicle == null)
                    return NotFound(new { success = false, message = "Vehicle not found" });

                var vehicleDetail = new
                {
                    v = vehicle.VehicleId,
                    vehicle.VehicleName,
                    vehicle.VehicleType,
                    vehicle.VehicleBrand,
                    vehicle.VehicleSegment,
                    vehicle.VehicleVersion,
                    vehicle.VehicleRate,
                    vehicle.BodyNumber,
                    vehicle.EngineNumber,
                    vehicle.VehicleNumber,
                    vehicle.RegistrationDate,
                    vehicle.SeatCount,
                    vehicle.VehicleImage,
                    vehicle.ManufactureYear,
                    vehicle.ModelId,
                    vehicle.VehicleModelName,
                    ModelName = vehicle.Model != null ? vehicle.Model.ModelName : null,
                    VehicleClass = vehicle.Model != null ? vehicle.Model.VehicleClass : null,
                    // Customer Information
                    Customer = vehicle.Customer != null ? new
                    {
                        vehicle.Customer.CustomerId,
                        vehicle.Customer.CustomerName,
                        vehicle.Customer.Address,
                        vehicle.Customer.Phone,
                        vehicle.Customer.Avatar,
                        Email = vehicle.Customer.User != null ? vehicle.Customer.User.Email : null
                    } : null,
                    // Policies
                    Policies = vehicle.Policies.Select(p => new
                    {
                        p.PolicyId,
                        p.PolicyNumber,
                        StartDate = p.PolicyStartDate,
                        EndDate = p.PolicyEndDate,
                        p.Status,
                        p.PremiumAmount,
                        CoverageAmount = p.DurationMonths
                    }).ToList(),
                    vehicle.CreatedDate,
                    vehicle.UpdatedDate
                };

                return Ok(new { success = true, data = vehicleDetail });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting vehicle detail: {ex.Message}");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        // POST: api/vehicleinformation - Create new vehicle
        [HttpPost]
        public async Task<ActionResult<object>> CreateVehicle([FromForm] VehicleCreateRequest request)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrEmpty(request.VehicleName) || string.IsNullOrEmpty(request.VehicleNumber))
                    return BadRequest(new { success = false, message = "Vehicle name and number are required" });

                // Get current customer - try JWT claim first, then fallback to request header
                int userId = 0;
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int claimUserId))
                {
                    userId = claimUserId;
                }
                else if (Request.Headers.TryGetValue("X-User-Id", out var userIdHeader) && 
                         int.TryParse(userIdHeader.ToString(), out int headerUserId))
                {
                    userId = headerUserId;
                }
                
                if (userId == 0)
                    return Unauthorized(new { success = false, message = "User not authenticated" });

                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (customer == null)
                    return NotFound(new { success = false, message = "Customer not found" });

                // Check unique constraints
                var existingVehicle = await _context.Vehicles
                    .FirstOrDefaultAsync(v =>
                        v.VehicleNumber.ToLower() == request.VehicleNumber.ToLower() ||
                        v.BodyNumber.ToLower() == request.BodyNumber.ToLower() ||
                        v.EngineNumber.ToLower() == request.EngineNumber.ToLower()
                    );

                if (existingVehicle != null)
                    return BadRequest(new { success = false, message = "Vehicle with this body number, engine number, or registration number already exists" });

                var vehicle = new Vehicle
                {
                    CustomerId = customer.CustomerId,
                    VehicleOwnerName = customer.CustomerName,
                    VehicleModelName = request.VehicleModel,
                    VehicleName = request.VehicleName,
                    VehicleType = request.VehicleType,
                    VehicleBrand = request.VehicleBrand,
                    VehicleSegment = request.VehicleSegment,
                    VehicleVersion = request.VehicleVersion,
                    VehicleRate = request.VehicleRate,
                    BodyNumber = request.BodyNumber,
                    EngineNumber = request.EngineNumber,
                    VehicleNumber = request.VehicleNumber,
                    RegistrationDate = request.RegistrationDate,
                    SeatCount = request.SeatCount,
                    ManufactureYear = request.ManufactureYear,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow
                };

                // Handle image upload
                if (request.VehicleImageFile != null && request.VehicleImageFile.Length > 0)
                {
                    try
                    {
                        var fileName = $"vehicle_{vehicle.CustomerId}_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N").Substring(0, 8)}_{request.VehicleImageFile.FileName}";
                        var uploadPath = Path.Combine(_hostEnvironment.ContentRootPath, "uploads", "vehicles");
                        Directory.CreateDirectory(uploadPath);
                        var filePath = Path.Combine(uploadPath, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await request.VehicleImageFile.CopyToAsync(stream);
                        }

                        vehicle.VehicleImage = $"/uploads/vehicles/{fileName}";
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Image upload failed: {ex.Message}");
                        // Continue without image
                    }
                }

                _context.Vehicles.Add(vehicle);
                await _context.SaveChangesAsync();

                var responseData = new {
                    VehicleId = vehicle.VehicleId,
                    CustomerId = vehicle.CustomerId,
                    ModelId = vehicle.ModelId,
                    VehicleName = vehicle.VehicleName,
                    VehicleType = vehicle.VehicleType,
                    VehicleBrand = vehicle.VehicleBrand,
                    VehicleSegment = vehicle.VehicleSegment,
                    VehicleVersion = vehicle.VehicleVersion,
                    VehicleRate = vehicle.VehicleRate,
                    BodyNumber = vehicle.BodyNumber,
                    EngineNumber = vehicle.EngineNumber,
                    VehicleNumber = vehicle.VehicleNumber,
                    RegistrationDate = vehicle.RegistrationDate?.ToString("yyyy-MM-dd"),
                    SeatCount = vehicle.SeatCount,
                    VehicleImage = vehicle.VehicleImage,
                    ManufactureYear = vehicle.ManufactureYear,
                    CreatedDate = vehicle.CreatedDate?.ToString("O"),
                    UpdatedDate = vehicle.UpdatedDate?.ToString("O")
                };

                return CreatedAtAction("GetVehicleDetail", new { id = vehicle.VehicleId },
                    new { 
                        success = true, 
                        message = "Vehicle created successfully", 
                        data = responseData
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating vehicle: {ex.Message} - {ex.InnerException?.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // PUT: api/vehicleinformation/5 - Update vehicle
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateVehicle(int id, [FromForm] VehicleUpdateRequest request)
        {
            try
            {
                var vehicle = await _context.Vehicles
                    .FirstOrDefaultAsync(v => v.VehicleId == id);

                if (vehicle == null)
                    return NotFound(new { success = false, message = "Vehicle not found" });

                // Verify ownership - try JWT claim first, then fallback to request header
                int userId = 0;
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int claimUserId))
                {
                    userId = claimUserId;
                }
                else if (Request.Headers.TryGetValue("X-User-Id", out var userIdHeader) && 
                         int.TryParse(userIdHeader.ToString(), out int headerUserId))
                {
                    userId = headerUserId;
                }

                if (userId > 0)
                {
                    var customer = await _context.Customers
                        .FirstOrDefaultAsync(c => c.UserId == userId);

                    if (customer != null && vehicle.CustomerId != customer.CustomerId)
                        return Forbid("You can only update your own vehicles");
                }

                // Check unique constraints for updated fields
                if (!string.IsNullOrEmpty(request.VehicleNumber) && request.VehicleNumber != vehicle.VehicleNumber)
                {
                    var existingVehicle = await _context.Vehicles
                        .FirstOrDefaultAsync(v => v.VehicleNumber.ToLower() == request.VehicleNumber.ToLower());
                    if (existingVehicle != null)
                        return BadRequest(new { success = false, message = "This registration number already exists" });
                }

                // Update fields
                vehicle.VehicleName = request.VehicleName ?? vehicle.VehicleName;
                vehicle.VehicleType = request.VehicleType ?? vehicle.VehicleType;
                vehicle.VehicleBrand = request.VehicleBrand ?? vehicle.VehicleBrand;
                vehicle.VehicleSegment = request.VehicleSegment ?? vehicle.VehicleSegment;
                vehicle.VehicleVersion = request.VehicleVersion ?? vehicle.VehicleVersion;
                vehicle.VehicleRate = request.VehicleRate ?? vehicle.VehicleRate;
                vehicle.BodyNumber = request.BodyNumber ?? vehicle.BodyNumber;
                vehicle.EngineNumber = request.EngineNumber ?? vehicle.EngineNumber;
                vehicle.VehicleNumber = request.VehicleNumber ?? vehicle.VehicleNumber;
                vehicle.RegistrationDate = request.RegistrationDate ?? vehicle.RegistrationDate;
                vehicle.SeatCount = request.SeatCount ?? vehicle.SeatCount;
                vehicle.ManufactureYear = request.ManufactureYear ?? vehicle.ManufactureYear;
                if (!string.IsNullOrEmpty(request.VehicleModel))
                    vehicle.VehicleModelName = request.VehicleModel;
                vehicle.UpdatedDate = DateTime.UtcNow;

                // Handle image upload
                if (request.VehicleImageFile != null && request.VehicleImageFile.Length > 0)
                {
                    try
                    {
                        // Delete old image if exists
                        if (!string.IsNullOrEmpty(vehicle.VehicleImage))
                        {
                            var oldImagePath = Path.Combine(_hostEnvironment.ContentRootPath, "uploads", vehicle.VehicleImage.TrimStart('/'));
                            if (System.IO.File.Exists(oldImagePath))
                                System.IO.File.Delete(oldImagePath);
                        }

                        var fileName = $"vehicle_{vehicle.CustomerId}_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N").Substring(0, 8)}_{request.VehicleImageFile.FileName}";
                        var uploadPath = Path.Combine(_hostEnvironment.ContentRootPath, "uploads", "vehicles");
                        Directory.CreateDirectory(uploadPath);
                        var filePath = Path.Combine(uploadPath, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await request.VehicleImageFile.CopyToAsync(stream);
                        }

                        vehicle.VehicleImage = $"/uploads/vehicles/{fileName}";
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Image upload failed: {ex.Message}");
                    }
                }

                _context.Vehicles.Update(vehicle);
                await _context.SaveChangesAsync();

                var responseData = new {
                    VehicleId = vehicle.VehicleId,
                    CustomerId = vehicle.CustomerId,
                    ModelId = vehicle.ModelId,
                    VehicleName = vehicle.VehicleName,
                    VehicleType = vehicle.VehicleType,
                    VehicleBrand = vehicle.VehicleBrand,
                    VehicleSegment = vehicle.VehicleSegment,
                    VehicleVersion = vehicle.VehicleVersion,
                    VehicleRate = vehicle.VehicleRate,
                    BodyNumber = vehicle.BodyNumber,
                    EngineNumber = vehicle.EngineNumber,
                    VehicleNumber = vehicle.VehicleNumber,
                    RegistrationDate = vehicle.RegistrationDate?.ToString("yyyy-MM-dd"),
                    SeatCount = vehicle.SeatCount,
                    VehicleImage = vehicle.VehicleImage,
                    ManufactureYear = vehicle.ManufactureYear,
                    CreatedDate = vehicle.CreatedDate?.ToString("O"),
                    UpdatedDate = vehicle.UpdatedDate?.ToString("O")
                };

                return Ok(new { success = true, message = "Vehicle updated successfully", data = responseData });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating vehicle: {ex.Message}");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        // DELETE: api/vehicleinformation/5 - Delete vehicle
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVehicle(int id)
        {
            try
            {
                var vehicle = await _context.Vehicles
                    .FirstOrDefaultAsync(v => v.VehicleId == id);

                if (vehicle == null)
                    return NotFound(new { success = false, message = "Vehicle not found" });

                // Verify ownership - try JWT claim first, then fallback to request header
                int userId = 0;
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int claimUserId))
                {
                    userId = claimUserId;
                }
                else if (Request.Headers.TryGetValue("X-User-Id", out var userIdHeader) && 
                         int.TryParse(userIdHeader.ToString(), out int headerUserId))
                {
                    userId = headerUserId;
                }

                if (userId > 0)
                {
                    var customer = await _context.Customers
                        .FirstOrDefaultAsync(c => c.UserId == userId);

                    if (customer != null && vehicle.CustomerId != customer.CustomerId)
                        return Forbid("You can only delete your own vehicles");
                }

                // Delete image if exists
                if (!string.IsNullOrEmpty(vehicle.VehicleImage))
                {
                    try
                    {
                        var imagePath = Path.Combine(_hostEnvironment.WebRootPath, vehicle.VehicleImage.TrimStart('/'));
                        if (System.IO.File.Exists(imagePath))
                            System.IO.File.Delete(imagePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Failed to delete image: {ex.Message}");
                    }
                }

                _context.Vehicles.Remove(vehicle);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Vehicle deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting vehicle: {ex.Message}");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        // GET: api/vehicleinformation/export/excel - Export vehicles to Excel
        [HttpGet("export/excel")]
        public async Task<ActionResult> ExportVehiclesToExcel()
        {
            try
            {
                // Set EPPlus license context
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                // Get all vehicles with policies
                var vehicles = await _context.Vehicles
                    .Include(v => v.Model)
                    .Include(v => v.Customer)
                    .Include(v => v.Policies)
                    .OrderByDescending(v => v.CreatedDate)
                    .ToListAsync();

                // Load template file path
                string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "frontend/templates/Vehicles.xlsx");
                
                if (!System.IO.File.Exists(templatePath))
                {
                    _logger.LogWarning($"Template file not found at: {templatePath}");
                    return NotFound(new { success = false, message = "Template file not found" });
                }

                // Load template file with shared access
                byte[] templateBytes;
                using (var fileStream = new FileStream(templatePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var memStream = new MemoryStream())
                {
                    fileStream.CopyTo(memStream);
                    templateBytes = memStream.ToArray();
                }
                
                using (var stream = new MemoryStream(templateBytes))
                using (var package = new ExcelPackage(stream))
                {
                    // Get the first worksheet
                    var worksheet = package.Workbook.Worksheets[0];

                    // Delete data rows (starting from row 6, keep header rows 1-5)
                    int rowCount = worksheet.Dimension?.Rows ?? 0;
                    if (rowCount > 6)
                    {
                        worksheet.DeleteRow(6, rowCount - 5);
                    }

                    // Add vehicle data starting from row 6
                    int row = 6;
                    int stt = 1;
                    foreach (var vehicle in vehicles)
                    {
                        // Calculate status based on policies
                        string status = "Inactive";
                        var nowDate = DateOnly.FromDateTime(DateTime.Now);
                        
                        if (vehicle.Policies != null && vehicle.Policies.Count > 0)
                        {
                            // Check if there's an active policy (comparing DateOnly with DateOnly)
                            var activePolicy = vehicle.Policies
                                .FirstOrDefault(p => p.PolicyStartDate <= nowDate && p.PolicyEndDate >= nowDate);
                            
                            if (activePolicy != null)
                            {
                                status = "Active";
                            }
                            else
                            {
                                // Check if there's an expired policy
                                var expiredPolicy = vehicle.Policies
                                    .FirstOrDefault(p => p.PolicyEndDate < nowDate);
                                
                                if (expiredPolicy != null)
                                {
                                    status = "Expired";
                                }
                            }
                        }

                        // Column A: STT (Sequential number)
                        var cellA = worksheet.Cells[row, 1];
                        cellA.Value = stt;
                        cellA.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        cellA.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                        
                        // Column B: VEHICLE NAME
                        var cellB = worksheet.Cells[row, 2];
                        cellB.Value = vehicle.VehicleName ?? "";
                        cellB.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        cellB.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                        
                        // Column C: PLATE
                        var cellC = worksheet.Cells[row, 3];
                        cellC.Value = vehicle.VehicleNumber ?? "";
                        cellC.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        cellC.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                        
                        // Column D: OWNER
                        var cellD = worksheet.Cells[row, 4];
                        cellD.Value = vehicle.VehicleOwnerName ?? "";
                        cellD.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        cellD.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                        
                        // Column E: BRAND
                        var cellE = worksheet.Cells[row, 5];
                        cellE.Value = vehicle.VehicleBrand ?? "";
                        cellE.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        cellE.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                        
                        // Column F: CLASS
                        var cellF = worksheet.Cells[row, 6];
                        cellF.Value = vehicle.VehicleSegment ?? "";
                        cellF.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        cellF.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                        
                        // Column G: TYPE
                        var cellG = worksheet.Cells[row, 7];
                        cellG.Value = vehicle.VehicleType ?? "";
                        cellG.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        cellG.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                        
                        // Column H: REGISTRATION DATE
                        var cellH = worksheet.Cells[row, 8];
                        if (vehicle.RegistrationDate.HasValue)
                        {
                            cellH.Value = vehicle.RegistrationDate.Value.ToString("yyyy-MM-dd");
                        }
                        else
                        {
                            cellH.Value = "";
                        }
                        cellH.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        cellH.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                        
                        // Column I: STATUS
                        var cellI = worksheet.Cells[row, 9];
                        cellI.Value = status;
                        cellI.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        cellI.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;

                        // Apply borders to all cells in the row
                        for (int col = 1; col <= 9; col++)
                        {
                            var cell = worksheet.Cells[row, col];
                            cell.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                            cell.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                            cell.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                            cell.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                        }

                        row++;
                        stt++;
                    }

                    // Apply borders to header row (row 5 to last row)
                    int lastRow = row - 1;
                    for (int col = 1; col <= 9; col++)
                    {
                        for (int headerRow = 5; headerRow <= lastRow; headerRow++)
                        {
                            var cell = worksheet.Cells[headerRow, col];
                            cell.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                            cell.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                            cell.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                            cell.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                        }
                    }

                    // Auto-fit columns
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    // Return the Excel file
                    var excelBytes = package.GetAsByteArray();
                    string fileName = $"Vehicles_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error exporting vehicles to Excel: {ex.Message}");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }
    }

    // Request models
    public class VehicleCreateRequest
    {
        public required string VehicleName { get; set; }
        public required string VehicleType { get; set; }
        public required string VehicleBrand { get; set; }
        public required string VehicleSegment { get; set; }
        public required string VehicleVersion { get; set; }
        public decimal VehicleRate { get; set; }
        public required string BodyNumber { get; set; }
        public required string EngineNumber { get; set; }
        public required string VehicleNumber { get; set; }
        public DateTime? RegistrationDate { get; set; }
        public int? SeatCount { get; set; }
        public int? ManufactureYear { get; set; }
        public string? VehicleModel { get; set; }
        public int? ModelId { get; set; }
        public IFormFile? VehicleImageFile { get; set; }
    }

    public class VehicleUpdateRequest
    {
        public required string VehicleName { get; set; }
        public required string VehicleType { get; set; }
        public required string VehicleBrand { get; set; }
        public required string VehicleSegment { get; set; }
        public required string VehicleVersion { get; set; }
        public decimal? VehicleRate { get; set; }
        public required string BodyNumber { get; set; }
        public required string EngineNumber { get; set; }
        public required string VehicleNumber { get; set; }
        public DateTime? RegistrationDate { get; set; }
        public int? SeatCount { get; set; }
        public int? ManufactureYear { get; set; }
        public string? VehicleModel { get; set; }
        public int? ModelId { get; set; }
        public IFormFile? VehicleImageFile { get; set; }
    }
}
