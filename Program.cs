<<<<<<< HEAD
using Microsoft.EntityFrameworkCore;
using VehicleInsuranceAPI.Data;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<VehicleInsuranceContext>(options =>
    options.UseSqlServer(connectionString));


=======
var builder = WebApplication.CreateBuilder(args);

>>>>>>> 8120502c19ece383406e34570cee8b6d23fa61f9
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(x => x
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader()
);
<<<<<<< HEAD
=======

// Phục vụ các tệp tĩnh từ thư mục frontend
>>>>>>> 8120502c19ece383406e34570cee8b6d23fa61f9
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "frontend")),
    RequestPath = ""
});

<<<<<<< HEAD
// Route mặc định
=======
// Route /index phục vụ index.html
>>>>>>> 8120502c19ece383406e34570cee8b6d23fa61f9
app.MapGet("/index", async (HttpContext context) =>
{
    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.SendFileAsync(Path.Combine(app.Environment.ContentRootPath, "frontend", "index.html"));
});

app.UseAuthorization();
<<<<<<< HEAD

app.MapControllers();

app.Run();
=======
app.MapControllers();
app.Run();
>>>>>>> 8120502c19ece383406e34570cee8b6d23fa61f9
