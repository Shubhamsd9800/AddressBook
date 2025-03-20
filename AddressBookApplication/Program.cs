using System.Text;
using BusinessLayer.Helper;
using BusinessLayer.Interface;
using BusinessLayer.Service;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RepositoryLayer.Context;
using RepositoryLayer.Interface;
using RepositoryLayer.Model;
using RepositoryLayer.Service;
using StackExchange.Redis;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.OpenApi.Models;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddScoped<IAddressBookBL, AddressBookBL>();
builder.Services.AddScoped<IAddressBookRL, AddressBookRL>();
builder.Services.AddScoped<IUserBL, UserBL>();
builder.Services.AddScoped<IUserRL, UserRL>();


builder.Services.AddEndpointsApiExplorer();
// Add Swagger documentation
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AddressBook API",
        Version = "v1",
        Description = "API for Address Book Management with Authentication, Redis, and RabbitMQ.",
        Contact = new OpenApiContact
        {
            Name = "Somesh Purwar",
            Email = "somesh.purwar20@gmail.com"
        }
    });

    // Enable JWT Authentication in Swagger UI
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Enter 'Bearer YOUR_TOKEN' in the field below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});


builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Helper Classes
builder.Services.AddSingleton<JwtHelper>();
builder.Services.AddSingleton<PasswordHasher>();
builder.Services.AddSingleton<EmailService>();

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]))
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"❌ Authentication failed: {context.Exception.Message}");
                return Task.CompletedTask;
            },
            OnMessageReceived = context =>
            {
                var token = context.Request.Headers["Authorization"].ToString();
                Console.WriteLine($"🔹 Received Token: {token}");

                if (string.IsNullOrEmpty(token))
                {
                    Console.WriteLine("❌ Token is missing in the request.");
                }

                return Task.CompletedTask;
            },
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";

                Console.WriteLine("❌ JWT Challenge: Unauthorized - Token missing or invalid.");

                await context.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    message = "Unauthorized: Invalid or missing token",
                    data = (string)null
                });
            }
        };
    });

var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
if (string.IsNullOrEmpty(redisConnectionString))
{
    throw new ArgumentNullException("Redis connection string is missing in appsettings.json.");
}

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
    options.InstanceName = "AddressBookCache_";
});

// User session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // 30-minute session timeout
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// RabbitMQ
builder.Services.AddSingleton<RabbitMQPublisher>();
builder.Services.AddHostedService<RabbitMQConsumer>();





var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// Configure the HTTP request pipeline.

app.UseSession();

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
