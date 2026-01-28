using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using VehicleInsuranceAPI.Data;

var builder = WebApplication.CreateBuilder(args);

// --- Services ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Missing ConnectionStrings:DefaultConnection in appsettings.json / environment variables.");
}

builder.Services.AddDbContext<VehicleInsuranceContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS: dev-friendly
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// --- Middleware ---
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseCors("AllowAll");

// ✅ Serve static files from: VehicleInsuranceAPI/frontend
var frontendPath = Path.Combine(builder.Environment.ContentRootPath, "frontend");
if (!Directory.Exists(frontendPath))
{
    throw new DirectoryNotFoundException(
        $"Static frontend folder not found: {frontendPath}. Expected: VehicleInsuranceAPI/frontend");
}

// ✅ Make "/" show frontend/user/home.html
// Approach: redirect "/" -> "/user/home.html"
app.MapGet("/", () => Results.Redirect("/user/home.html"));

// ✅ Serve files in /frontend as web root
// Example: frontend/user/home.html => http://localhost:5169/user/home.html
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(frontendPath),
    RequestPath = ""
});

app.UseAuthorization();

// --- API Endpoints ---
app.MapGet("/api/test", () => Results.Ok("App works!"));
app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

app.MapControllers();

app.Run();
