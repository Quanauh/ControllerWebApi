using Microsoft.AspNetCore.Mvc;
using Serilog;
using Serilog.Events;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi;
using HuanBank.Api.Services;
using HuanBank.Api.DAO;
using HuanBank.Api.Validators;
using HuanBank.Api.Util;
using HuanBank.Api.Authorization;
using System.Security.Claims;
var builder = WebApplication.CreateBuilder(args);
if (builder.Environment.IsDevelopment())
{
    var envFile = Path.Combine(builder.Environment.ContentRootPath, ".env");
    if (File.Exists(envFile))
    {
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in File.ReadLines(envFile))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#')) continue;

            var separator = trimmed.IndexOf('=');
            if (separator <= 0)
                throw new InvalidOperationException("Invalid .env line: expected KEY=VALUE.");

            var name = trimmed[..separator].Trim();
            if (name != "Jwt__Key" && name != "ConnectionStrings__Oracle")
                throw new InvalidOperationException("Unsupported .env key: " + name);

            values[name.Replace("__", ":", StringComparison.Ordinal)] = trimmed[(separator + 1)..].Trim();
        }

        builder.Configuration.AddInMemoryCollection(values);
        builder.Configuration.AddEnvironmentVariables();
    }
}
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException("Jwt:Key must be configured with at least 32 UTF-8 bytes.");

var database = new Database(builder.Configuration);

// Add services to the container.
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .WriteTo.File(
        path: "logs/log_.txt",
        rollingInterval: RollingInterval.Day
    )
    .CreateLogger();
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddScoped<IAccountDAO,AccountDAO>();
builder.Services.AddSingleton(database);
builder.Services.AddScoped<IBankService,BankService>();
builder.Services.AddScoped<IRegisterAccountValidator, RegisterAccountValidator>();
builder.Services.AddScoped<IMoneyTransactionValidator, MoneyTransactionValidator>();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Nhap: Bearer {token}",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});
builder.Services.AddScoped<TokenService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IAuthorizationHandler, AccountAccessHandler>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var accountNumber = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                var tokenRole = context.Principal?.FindFirstValue(ClaimTypes.Role);
                var currentRole = string.IsNullOrWhiteSpace(accountNumber)
                    ? null
                    : context.HttpContext.RequestServices.GetRequiredService<IAccountDAO>()
                        .GetActiveRole(accountNumber);

                if (currentRole == null ||
                    (currentRole != BankRoles.Customer && currentRole != BankRoles.Admin) ||
                    !string.Equals(currentRole, tokenRole, StringComparison.Ordinal))
                    context.Fail("Account is inactive or its role has changed.");

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(BankPolicies.CustomerOnly, policy =>
        policy.RequireRole(BankRoles.Customer));
    options.AddPolicy(BankPolicies.AdminOnly, policy =>
        policy.RequireRole(BankRoles.Admin));
    options.AddPolicy(BankPolicies.AccountAccess, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole(BankRoles.Customer, BankRoles.Admin);
        policy.AddRequirements(new AccountAccessRequirement());
    });
});
builder.Services.AddLogging(b =>
    b.AddSerilog(dispose: true)
);  
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();
app.UseAuthentication();   // xac dinh "ban la ai" tu token
app.UseAuthorization();

app.MapControllers();

app.Run();
