using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace WebApiCore.Identity
{
    public static class JWTConfig
    {
        public static void AddJwtConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            var tokenValidationParametersSection = configuration.GetSection("TokenValidationParametersSettings");
            services.Configure<TokenValidationParametersSettings>(tokenValidationParametersSection);

            var tokenValidationParameters = tokenValidationParametersSection.Get<TokenValidationParametersSettings>();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
             .AddJwtBearer(bearerOptions => {
                 bearerOptions.RequireHttpsMetadata = true;
                 bearerOptions.SaveToken = true;
                 bearerOptions.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters()
                 {
                     ValidateIssuerSigningKey = true,
                     IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(tokenValidationParameters.IssuerSigningKey)),
                     ValidateIssuer = true,
                     ValidIssuer = tokenValidationParameters.ValidIssuer,
                     ValidateAudience = true,
                     ValidAudience = tokenValidationParameters.ValidAudience
                 };
             });
        }

        public static void UseAuthConfiguration(this IApplicationBuilder app)
        {
            app.UseAuthentication();
            app.UseAuthorization();
        }

    }

    public class TokenValidationParametersSettings
    {

        public string IssuerSigningKey { get; set; }
        public int Expiration { get; set; }
        public string ValidIssuer { get; set; }
        public string ValidAudience { get; set; }
    }

}
