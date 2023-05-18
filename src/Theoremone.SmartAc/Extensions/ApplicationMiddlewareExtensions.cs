using Theoremone.SmartAc.Middlewares;

namespace Theoremone.SmartAc.Extensions
{
    public static class ApplicationMiddlewareExtensions
    {
        public static void UseApiErrorHandler(this IApplicationBuilder app) =>
            app.UseMiddleware<ApiErrorHandler>();
    }
}
