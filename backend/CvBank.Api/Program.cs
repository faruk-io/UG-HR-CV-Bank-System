using CvBank.Api.Auth;
using CvBank.Api.Data;
using CvBank.Api.Infrastructure;
using CvBank.Api.Models;
using CvBank.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options => options.Filters.Add<ApiExceptionFilter>());
builder.Services.AddOpenApi();

builder.Services.AddDbContext<CvBankDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CvBank"))
           .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddSingleton<SettingsCache>();
builder.Services.AddSingleton<IFileStorage, FileStorage>();
builder.Services.AddScoped<IAuditWriter, AuditWriter>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<CandidateService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<EvaluationService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<ActivityLogService>();
builder.Services.AddScoped<UserService>();

const string AngularClient = "AngularClient";
// Comma-separated so localhost, the IP and the machine name can all be allowed at once.
var allowedOrigins = (builder.Configuration["Cors:AllowedOrigin"] ?? string.Empty)
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

if (allowedOrigins.Length == 0)
{
    allowedOrigins = ["http://localhost:4400"];
}

builder.Services.AddCors(options =>
{
    options.AddPolicy(AngularClient, policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod());
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(AuthService.ReadSigningKey(builder.Configuration, builder.Environment.ContentRootPath)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = AuthClaims.UserName,
            RoleClaimType = AuthClaims.Role
        };
    });

builder.Services.AddSingleton<IAuthorizationHandler, PermissionHandler>();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

using (var scope = app.Services.CreateScope())
{
    try
    {
        await DbInitializer.InitializeAsync(scope.ServiceProvider);
    }
    catch (Exception ex)
    {
        app.Logger.LogCritical(ex, "Database initialization failed at startup. App will continue running.");
    }
}

app.UseCors(AngularClient);
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

//using CvBank.Api.Auth;
//using CvBank.Api.Data;
//using CvBank.Api.Infrastructure;
//using CvBank.Api.Models;
//using CvBank.Api.Services;
//using Microsoft.AspNetCore.Authentication.JwtBearer;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.IdentityModel.Tokens;

//var builder = WebApplication.CreateBuilder(args);

//builder.Services.AddControllers(options => options.Filters.Add<ApiExceptionFilter>());
//builder.Services.AddOpenApi();

//builder.Services.AddDbContext<CvBankDbContext>(options =>
//    options.UseSqlServer(builder.Configuration.GetConnectionString("CvBank")));

//builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
//builder.Services.AddSingleton<SettingsCache>();
//builder.Services.AddSingleton<IFileStorage, FileStorage>();
//builder.Services.AddScoped<IAuditWriter, AuditWriter>();
//builder.Services.AddScoped<AuthService>();
//builder.Services.AddScoped<CandidateService>();
//builder.Services.AddScoped<DashboardService>();
//builder.Services.AddScoped<EvaluationService>();
//builder.Services.AddScoped<ReportService>();
//builder.Services.AddScoped<ActivityLogService>();
//builder.Services.AddScoped<UserService>();

//const string AngularClient = "AngularClient";
//// Comma-separated so localhost, the IP and the machine name can all be allowed at once.
//var allowedOrigins = (builder.Configuration["Cors:AllowedOrigin"] ?? string.Empty)
//    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

//if (allowedOrigins.Length == 0)
//{
//    allowedOrigins = ["http://localhost:4400"];
//}

//builder.Services.AddCors(options =>
//{
//    options.AddPolicy(AngularClient, policy => policy
//        .WithOrigins(allowedOrigins)
//        .AllowAnyHeader()
//        .AllowAnyMethod());
//});

//builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//    .AddJwtBearer(options =>
//    {
//        options.TokenValidationParameters = new TokenValidationParameters
//        {
//            ValidateIssuer = true,
//            ValidIssuer = builder.Configuration["Jwt:Issuer"],
//            ValidateAudience = true,
//            ValidAudience = builder.Configuration["Jwt:Audience"],
//            ValidateIssuerSigningKey = true,
//            IssuerSigningKey = new SymmetricSecurityKey(AuthService.ReadSigningKey(builder.Configuration, builder.Environment.ContentRootPath)),
//            ValidateLifetime = true,
//            ClockSkew = TimeSpan.FromSeconds(30),
//            NameClaimType = AuthClaims.UserName,
//            RoleClaimType = AuthClaims.Role
//        };
//    });

//builder.Services.AddSingleton<IAuthorizationHandler, PermissionHandler>();
//builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

//var app = builder.Build();

//if (app.Environment.IsDevelopment())
//{
//    app.MapOpenApi();
//}

//using (var scope = app.Services.CreateScope())
//{
//    await DbInitializer.InitializeAsync(scope.ServiceProvider);
//}

//app.UseCors(AngularClient);
//app.UseHttpsRedirection();
//app.UseAuthentication();
//app.UseAuthorization();
//app.MapControllers();

//app.Run();
