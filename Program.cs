using Microsoft.EntityFrameworkCore;
using VehicleInsuranceAPI.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
try
{
    if (!string.IsNullOrEmpty(connectionString))
    {
        builder.Services.AddDbContext<VehicleInsuranceContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure();
            }));
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Warning: Could not add DbContext: {ex.Message}");
}

var app = builder.Build();

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("Internal Server Error");
        });
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

// Serve static files from uploads directory
var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads",
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers["Cache-Control"] = "public,max-age=3600";
    }
});

app.UseRouting();
app.UseAuthorization();

// Routes
app.MapControllers();

// Serve templates
app.MapGet("/templates/{filename}", async (string filename, HttpContext context) =>
{
    var filePath = Path.Combine(app.Environment.ContentRootPath, "frontend", "templates", filename);
    if (File.Exists(filePath))
    {
        context.Response.ContentType = "text/html; charset=utf-8";
        var content = await File.ReadAllTextAsync(filePath);
        await context.Response.WriteAsync(content);
    }
    else
    {
        context.Response.StatusCode = 404;
    }
});

// Serve user pages
app.MapGet("/user/{filename}", async (string filename, HttpContext context) =>
{
    var filePath = Path.Combine(app.Environment.ContentRootPath, "frontend", "user", filename);
    if (File.Exists(filePath))
    {
        context.Response.ContentType = "text/html; charset=utf-8";
        var content = await File.ReadAllTextAsync(filePath);
        await context.Response.WriteAsync(content);
    }
    else
    {
        context.Response.StatusCode = 404;
    }
});

// Serve admin pages
app.MapGet("/admin/{filename}", async (string filename, HttpContext context) =>
{
    var filePath = Path.Combine(app.Environment.ContentRootPath, "frontend", "admin", filename);
    if (File.Exists(filePath))
    {
        context.Response.ContentType = "text/html; charset=utf-8";
        var content = await File.ReadAllTextAsync(filePath);
        await context.Response.WriteAsync(content);
    }
    else
    {
        context.Response.StatusCode = 404;
    }
});

// Serve staff pages
app.MapGet("/staff/{filename}", async (string filename, HttpContext context) =>
{
    var filePath = Path.Combine(app.Environment.ContentRootPath, "frontend", "staff", filename);
    if (File.Exists(filePath))
    {
        context.Response.ContentType = "text/html; charset=utf-8";
        var content = await File.ReadAllTextAsync(filePath);
        await context.Response.WriteAsync(content);
    }
    else
    {
        context.Response.StatusCode = 404;
    }
});

// Serve JS files
app.MapGet("/js/{filename}", async (string filename, HttpContext context) =>
{
    var filePath = Path.Combine(app.Environment.ContentRootPath, "frontend", "js", filename);
    if (File.Exists(filePath))
    {
        context.Response.ContentType = "application/javascript; charset=utf-8";
        var content = await File.ReadAllTextAsync(filePath);
        await context.Response.WriteAsync(content);
    }
    else
    {
        context.Response.StatusCode = 404;
    }
});

// Serve images
app.MapGet("/images/{*path}", async (string path, HttpContext context) =>
{
    var filePath = Path.Combine(app.Environment.ContentRootPath, "frontend", "images", path);
    if (File.Exists(filePath))
    {
        var ext = Path.GetExtension(filePath).ToLower();
        var contentType = ext switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".svg" => "image/svg+xml",
            _ => "image/*"
        };
        context.Response.ContentType = contentType;
        var content = await File.ReadAllBytesAsync(filePath);
        await context.Response.Body.WriteAsync(content);
    }
    else
    {
        context.Response.StatusCode = 404;
    }
});

// Test endpoint - KEEP THIS LAST
app.MapGet("/api/test", () => Results.Ok(new { message = "App works!" }));

// Home page
app.MapGet("/", async context =>
{
    var homePath = Path.Combine(app.Environment.ContentRootPath, "frontend", "user", "Home.html");
    if (File.Exists(homePath))
    {
        context.Response.ContentType = "text/html; charset=utf-8";
        var content = await File.ReadAllTextAsync(homePath);
        await context.Response.WriteAsync(content);
    }
    else
    {
        context.Response.StatusCode = 404;
        await context.Response.WriteAsync("Not found");
    }
});

try
{
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"FATAL ERROR: {ex}");
    System.Environment.Exit(1);
}





