using HoneyBot;
using HoneyBot.Middlewares;
using HoneyBot.Models;
using HoneyBot.Service;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));

// Register custom services
builder.Services.AddSingleton<IAttackAnalysisService, AttackAnalysisService>();
builder.Services.AddHttpClient<IGeolocationService, GeolocationService>(); // <-- تسجيل خدمة الموقع
builder.Services.AddMemoryCache();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseHonyBotMiddleware();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // لخدمة أي صفحات وهمية من wwwroot
app.MapControllers();

// Database Seeding Logic...
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();

    // Ensure at least one honeypot exists for incident logging
    if (!db.Honeypots.Any())
    {
        db.Honeypots.Add(new Honeypot
        {
            Name = "Default Honeypot",
            UrlPath = "/",
            CreatedAt = DateTime.UtcNow
        });
        db.SaveChanges();
    }
}

app.Run();