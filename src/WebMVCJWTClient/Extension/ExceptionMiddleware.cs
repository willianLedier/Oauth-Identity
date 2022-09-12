using Microsoft.AspNetCore.Http;
using System;
using System.Net;
using System.Threading.Tasks;
using WebMVCJWTClient.Services;

namespace WebMVCJWTClient.Extension
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private static IAutenticacaoService _autenticacaoService;

        public ExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext, IAutenticacaoService autenticacaoService)
        {
            _autenticacaoService = autenticacaoService;

            try
            {
                await _next(httpContext);
            }
            catch (CustomHttpRequestException ex)
            {
                HandleRequestExceptionAsync(httpContext, ex.StatusCode);
            }
            //catch (ApiException ex)
            //{
            //    HandleRequestExceptionAsync(httpContext, ex.StatusCode);
            //}
            //catch (BrokenCircuitException)
            //{
            //    HandleCircuitBreakerExceptionAsync(httpContext);
            //}
            catch (Exception ex)
            {
                httpContext.Response.Redirect("/sistema-indisponivel");
                httpContext.Response.StatusCode = (int)500;
            }

        }

        private static void HandleRequestExceptionAsync(HttpContext context, HttpStatusCode statusCode)
        {
            if (statusCode == HttpStatusCode.Unauthorized)
            {
                if (_autenticacaoService.TokenExpirado())
                {
                    if (_autenticacaoService.RefreshTokenValido().Result)
                    {
                        context.Response.Redirect(context.Request.Path);
                        return;
                    }
                }

                _autenticacaoService.Logout();
                context.Response.Redirect($"/login?ReturnUrl={context.Request.Path}");
                return;
            }

            context.Response.StatusCode = (int)statusCode;
        }

        private static void HandleCircuitBreakerExceptionAsync(HttpContext context)
        {
            context.Response.Redirect("/sistema-indisponivel");
        }
    }
}
