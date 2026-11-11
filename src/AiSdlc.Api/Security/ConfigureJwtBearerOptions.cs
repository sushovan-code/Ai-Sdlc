using System.Text;
using AiSdlc.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AiSdlc.Api.Security;

/// <summary>Builds token validation from <see cref="JwtOptions"/> so issuing and validating share one key.</summary>
internal sealed class ConfigureJwtBearerOptions(IOptions<JwtOptions> jwt) : IConfigureNamedOptions<JwtBearerOptions>
{
    private static readonly TimeSpan ClockSkew = TimeSpan.FromMinutes(1);

    public void Configure(string? name, JwtBearerOptions options)
    {
        if (name != JwtBearerDefaults.AuthenticationScheme)
        {
            return;
        }

        var settings = jwt.Value;
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = settings.Issuer,
            ValidateAudience = true,
            ValidAudience = settings.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)),
            ClockSkew = ClockSkew,
            NameClaimType = JwtClaims.Name,
            RoleClaimType = JwtClaims.Role,
        };
        options.Events = new JwtBearerEvents { OnTokenValidated = AccountStatusCheck.ValidateAsync };
    }

    public void Configure(JwtBearerOptions options) => Configure(JwtBearerDefaults.AuthenticationScheme, options);
}
