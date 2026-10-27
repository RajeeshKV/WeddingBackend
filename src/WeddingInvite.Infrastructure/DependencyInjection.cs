using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using WeddingInvite.Application.Interfaces;
using WeddingInvite.Application.Options;
using WeddingInvite.Infrastructure.Authentication;
using WeddingInvite.Infrastructure.Cache;
using WeddingInvite.Infrastructure.Persistence;
using WeddingInvite.Infrastructure.Services;

namespace WeddingInvite.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.Configure<CloudinaryOptions>(configuration.GetSection("Cloudinary"));
        services.Configure<BrevoOptions>(configuration.GetSection("Brevo"));

        services.AddDbContext<WeddingInviteDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddMemoryCache();
        services.AddHttpContextAccessor();
        services.AddSingleton<ICacheService, CacheService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ISuperUserAuthService, SuperUserAuthService>();
        services.AddScoped<ITenantAdminService, TenantAdminService>();
        services.AddScoped<ICloudinaryService, CloudinaryService>();
        services.AddHostedService<EmailOutboxBackgroundService>();

        var jwt = configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        services.AddAuthorization();
        return services;
    }
}
