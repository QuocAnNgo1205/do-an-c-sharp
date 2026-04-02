using Microsoft.OpenApi;
using VinhKhanhFoodTour.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using NetTopologySuite.Geometries;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddDbContext<AppDbContext>(options =>
    options
        .UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            x => x.UseNetTopologySuite()
        )
);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Vinh Khanh Food Tour API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Nhap JWT token vao day (khong can go tu 'Bearer').",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    // .NET 10 + OpenAPI 4: phai truyen `document` vao OpenApiSecuritySchemeReference,
    // neu khong Swagger UI se hien "Authorized" nhung KHONG gui header Authorization.
    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", document),
            new List<string>()
        }
    });

});

var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.IncludeErrorDetails = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key),
        NameClaimType = ClaimTypes.Name,
        RoleClaimType = ClaimTypes.Role,
        ClockSkew = TimeSpan.FromMinutes(5)
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var authHeader = context.Request.Headers.Authorization.ToString();
            if (!string.IsNullOrWhiteSpace(authHeader))
            {
                var token = authHeader.Trim();

                while (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    token = token["Bearer ".Length..].Trim();
                }

                context.Token = token;
                Console.WriteLine(
                    $"[JWT] {context.Request.Method} {context.Request.Path} — co Authorization, do dai token (sau Bearer) = {token.Length}");
            }
            else
            {
                Console.WriteLine(
                    $"[JWT] {context.Request.Method} {context.Request.Path} — KHONG co header Authorization");
            }

            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"[JWT AUTH FAILED] {context.Exception.Message}");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var sub = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);
            var roles = string.Join(",", context.Principal?.FindAll(ClaimTypes.Role).Select(c => c.Value) ?? []);
            Console.WriteLine($"[JWT OK] User sub={sub}; roles=[{roles}]");
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            var path = context.HttpContext.Request.Path;
            var hasAuth = context.HttpContext.Request.Headers.Authorization.Count > 0;
            Console.WriteLine(
                $"[JWT CHALLENGE] Path={path}; HasAuthorizationHeader={hasAuth}; " +
                $"Error='{context.Error}'; Description='{context.ErrorDescription}' " +
                $"(rong = thuong la thieu token hop le hoac chua gui header)");
            return Task.CompletedTask;
        }
    };
});

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.ConfigObject.PersistAuthorization = true;
});

// Seed the database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();

    // Ensure database is created
    context.Database.EnsureCreated();

    // Call the initializer
    DbInitializer.Initialize(context);
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/test", () => "Tuyet voi! Server Vinh Khanh Food Tour dang chay ngon lanh!");

app.Run();