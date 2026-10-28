using System.Text;
using GymOS.Application.Interfaces;
using GymOS.Infrastructure.BackgroundJobs;
using GymOS.Infrastructure.Data;
using GymOS.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Quartz;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var useSqlServer = builder.Configuration.GetValue<bool>("Database:UseSqlServer", true);
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    if (!builder.Environment.IsDevelopment())
        throw new InvalidOperationException("Configure Jwt:Key con al menos 32 bytes fuera de desarrollo.");
    jwtKey = "GymOS-Development-Only-Key-32-Bytes-Minimum!";
}

// ──────────────────────────────────────────────────────────
// 1. DATABASE
// ──────────────────────────────────────────────────────────
builder.Services.AddDbContext<GymDbContext>(options =>
{
    if (useSqlServer)
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
            sqlOptions => sqlOptions.EnableRetryOnFailure());
    else
        options.UseInMemoryDatabase("GymOS-Development");
});

// ──────────────────────────────────────────────────────────
// 2. AUTHENTICATION - JWT + Google OAuth
// ──────────────────────────────────────────────────────────
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("AdminOrReceptionist", policy => policy.RequireRole("Admin", "Receptionist"));
});

// ──────────────────────────────────────────────────────────
// 3. SERVICES
// ──────────────────────────────────────────────────────────
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddHttpClient<IWhatsAppService, WhatsAppService>();

// ──────────────────────────────────────────────────────────
// 4. QUARTZ - Background Job scheduler
// ──────────────────────────────────────────────────────────
builder.Services.AddQuartz(q =>
{
    q.AddJob<MembershipExpiryNotificationJob>(opts => opts.WithIdentity(MembershipExpiryNotificationJob.Key));
    q.AddTrigger(opts => opts
        .ForJob(MembershipExpiryNotificationJob.Key)
        .WithIdentity("membership-expiry-trigger")
        .WithDescription("Runs daily at 9:00 AM to notify members with expiring memberships")
        .WithCronSchedule("0 0 9 * * ?", x => x.InTimeZone(TimeZoneInfo.Local)));
});
builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

// ──────────────────────────────────────────────────────────
// 5. API + CORS + SWAGGER
// ──────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "GymOS API",
        Version = "v1",
        Description = "Sistema de Gestión de Gimnasios - GymOS"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Enter: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("GymOSCorsPolicy", policy =>
    {
        policy.WithOrigins(
            "http://localhost:4200",
            "https://localhost:4200",
            "http://127.0.0.1:4200",
            "https://127.0.0.1:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

// ──────────────────────────────────────────────────────────
// 6. MIDDLEWARE PIPELINE
// ──────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "GymOS API v1"));
}

if (!string.Equals(app.Configuration["DisableHttpsRedirection"], "true", StringComparison.OrdinalIgnoreCase))
    app.UseHttpsRedirection();
app.UseCors("GymOSCorsPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Auto-apply migrations on startup
if (useSqlServer)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<GymDbContext>();
    await db.Database.MigrateAsync();
}
else
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<GymDbContext>();
    await db.Database.EnsureCreatedAsync();
    var developmentAdmin = await db.AppUsers.FirstOrDefaultAsync(u => u.Email == "admin@gymos.com");
    if (developmentAdmin is null)
    {
        developmentAdmin = new GymOS.Domain.Entities.AppUser
        {
            Email = "admin@gymos.com",
            FirstName = "Admin",
            LastName = "GymOS",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@1234"),
            Role = GymOS.Domain.Enums.UserRole.Admin,
            IsActive = true
        };
        db.AppUsers.Add(developmentAdmin);
    }
    developmentAdmin.PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@1234");
    developmentAdmin.IsActive = true;
    developmentAdmin.Role = GymOS.Domain.Enums.UserRole.Admin;
    await db.SaveChangesAsync();
}

await app.RunAsync();
