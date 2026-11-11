using System.Security.Cryptography;
using AiSdlc.Application.Abstractions;
using AiSdlc.Application.Auth;
using AiSdlc.Infrastructure.Auditing;
using AiSdlc.Infrastructure.Identity;
using AiSdlc.Infrastructure.Persistence;
using AiSdlc.Infrastructure.Projects;
using AiSdlc.Infrastructure.Security;
using AiSdlc.Infrastructure.Seeding;
using AiSdlc.Infrastructure.Storage;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AiSdlc.Infrastructure;

public static class DependencyInjection
{
    public const string DefaultConnectionName = "Default";

    private const int MaxFailedAccessAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    /// <summary>Registers the adapters behind the application ports: SQLite, Identity, JWT, audit log, seeding.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);

        AddPersistence(services, configuration);
        AddIdentity(services);
        AddJwt(services, configuration);

        services.AddScoped<IUserAccounts, IdentityUserAccounts>();
        services.AddScoped<IAuditLog, AuditLog>();
        services.AddScoped<IProjectRepository, ProjectRepository>();

        services.AddOptions<StorageOptions>()
            .Bind(configuration.GetSection(StorageOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<IFileStorage, LocalFileStorage>();

        services.AddOptions<SeedOptions>().Bind(configuration.GetSection(SeedOptions.SectionName));
        services.AddHostedService<DatabaseInitializer>();

        return services;
    }

    private static void AddPersistence(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(DefaultConnectionName)
            ?? throw new InvalidOperationException($"Connection string '{DefaultConnectionName}' is not configured.");

        EnsureDatabaseDirectory(connectionString);
        services.AddDbContext<AiSdlcDbContext>(options => options.UseSqlite(connectionString));
    }

    private static void EnsureDatabaseDirectory(string connectionString)
    {
        var dataSource = new SqliteConnectionStringBuilder(connectionString).DataSource;
        var directory = Path.GetDirectoryName(Path.GetFullPath(dataSource));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static void AddIdentity(IServiceCollection services)
    {
        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = AccountLimits.MinPasswordLength;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = MaxFailedAccessAttempts;
                options.Lockout.DefaultLockoutTimeSpan = LockoutDuration;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AiSdlcDbContext>();
    }

    private static void AddJwt(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .PostConfigure(EnsureSigningKey)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<ITokenService, JwtTokenService>();
    }

    private static void EnsureSigningKey(JwtOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.SigningKey) && options.AllowEphemeralKey)
        {
            options.SigningKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(JwtOptions.EphemeralKeyBytes));
        }
    }
}
