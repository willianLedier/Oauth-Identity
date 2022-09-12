using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebMVCJWTClient.Identity;
using WebMVCJWTClient.Services;

namespace WebMVCJWTClient.Configuration
{
    public static class DependencyInjectionConfig
    {
        public static void RegisterServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddHttpClient<IAutenticacaoService, AutenticacaoService>();
            services.AddScoped<IAspNetUser, AspNetUser>();

        }
    }
}