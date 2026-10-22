using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using UtilitiesPortal.Api.Auth;
using UtilitiesPortal.Api.Data;
using UtilitiesPortal.Api.Errors;
using UtilitiesPortal.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Errors: exceptions become clean JSON responses (see Errors/GlobalExceptionHandler.cs)
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Database (connection string comes from user secrets locally, App Service settings on Azure)
builder.Services.AddDbContext<UtilitiesPortalContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("UtilitiesPortal")));

// Billing settings (rate and due days), checked when the app starts
builder.Services.AddOptions<BillingOptions>()
    .Bind(builder.Configuration.GetSection("Billing"))
    .Validate(o => o.RatePerKwh >= 0 && o.RatePerKwh <= 9999.9999m && o.DueDays >= 0,
        "Billing settings are invalid: RatePerKwh must be between 0 and 9999.9999 and DueDays must not be negative.")
    .ValidateOnStart();

// Login tokens (JWT)
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
          ?? throw new InvalidOperationException("The Jwt settings section is missing.");
if (string.IsNullOrWhiteSpace(jwt.Key) || jwt.Key.Length < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key is missing or too short. Set it with dotnet user-secrets (at least 32 characters).");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false; // keep the short claim names (sub, email, role)
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            RoleClaimType = AppClaims.Role,
            NameClaimType = AppClaims.Subject
        };
    });

builder.Services.AddAuthorization();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

// Our own services
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IMeterService, MeterService>();
builder.Services.AddScoped<IReadingService, ReadingService>();
builder.Services.AddScoped<IBillingService, BillingService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IServiceRequestService, ServiceRequestService>();
builder.Services.AddScoped<IOutageService, OutageService>();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseExceptionHandler(); // first, so it can catch errors from everything below

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("Frontend");

app.UseAuthentication(); // who are you (reads the token)
app.UseAuthorization();  // are you allowed (checks [Authorize])

app.MapControllers();

app.Run();
