using Microsoft.EntityFrameworkCore;
using WeddingInvite.Api.Middleware;
using WeddingInvite.Infrastructure;
using WeddingInvite.Infrastructure.Persistence;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

if (args.Contains("--migrate-only", StringComparer.OrdinalIgnoreCase))
{
    builder.Services.AddDbContext<WeddingInviteDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
    var migrateApp = builder.Build();
    using (var scope = migrateApp.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<WeddingInviteDbContext>();
        await dbContext.Database.MigrateAsync();
    }
    return;
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.DocInclusionPredicate((docName, apiDesc) => !apiDesc.ActionDescriptor.EndpointMetadata.Any(m => m is Microsoft.AspNetCore.Mvc.ApiExplorerSettingsAttribute a && a.IgnoreApi));
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT token"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
builder.Services.AddMemoryCache();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
        {
            if (string.IsNullOrWhiteSpace(origin)) return false;
            var normalized = origin.TrimEnd('/');
            try
            {
                using var scope = builder.Services.BuildServiceProvider().CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<WeddingInviteDbContext>();
                var cache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();
                var key = $"cors:allowed:{normalized}";
                if (cache.TryGetValue(key, out bool cachedAllowed)) return cachedAllowed;
                var allowed = db.AdminUserFrontendUrls.Any(x => x.IsActive && x.Url == normalized);
                cache.Set(key, allowed, TimeSpan.FromMinutes(5));
                return allowed;
            }
            catch
            {
                return false;
            }
        })
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<WeddingInviteDbContext>();
    try
    {
        await dbContext.Database.MigrateAsync();
    }
    catch
    {
    }
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/health/live", () => Results.Ok(new { status = "Alive" }));
app.MapGet("/health/ready", async (WeddingInviteDbContext dbContext, IMemoryCache memoryCache, CancellationToken cancellationToken) =>
{
    const string key = "health:db";
    if (!memoryCache.TryGetValue(key, out _))
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(5));
        try
        {
            await dbContext.Database.ExecuteSqlRawAsync("SELECT 1", cts.Token);
            memoryCache.Set(key, true, TimeSpan.FromDays(7));
        }
        catch
        {
            return Results.Json(new { status = "NotReady", database = "Error" }, statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }
    return Results.Ok(new { status = "Ready", database = "Connected" });
});

app.Run();
