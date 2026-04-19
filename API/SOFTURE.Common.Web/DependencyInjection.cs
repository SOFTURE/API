using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using SOFTURE.Common.Web.Middlewares;

namespace SOFTURE.Common.Web
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddCommonCulture(this IServiceCollection services, CultureInfo culture)
        {
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            services.Configure<RequestLocalizationOptions>(options =>
            {
                options.DefaultRequestCulture = new RequestCulture(culture);
                options.SupportedCultures = [culture];
                options.SupportedUICultures = [culture];
            });

            return services;
        }

        public static IServiceCollection AddCommonDataProtection(this IServiceCollection services, string keysPath)
        {
            services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(keysPath));

            return services;
        }

        public static IApplicationBuilder UseCommonRequestContextLogging(this IApplicationBuilder app)
        {
            app.UseMiddleware<RequestContextLoggingMiddleware>();

            return app;
        }
    }
}
