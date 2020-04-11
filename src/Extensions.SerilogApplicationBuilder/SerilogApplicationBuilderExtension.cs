using Microsoft.AspNetCore.Builder;
using Serilog;

namespace NetExtensions
{
    public static class SerilogApplicationBuilderExtension
    {
        /// <summary>
        /// From Serilog.AspNetCore:
        /// Adds middleware for streamlined request logging. Instead of writing HTTP request information
        /// like method, path, timing, status code and exception details
        /// in several events, this middleware collects information during the request (including from
        /// <see cref="T:Serilog.IDiagnosticContext" />), and writes a single event at request completion. Add this
        /// in <c>Startup.cs</c> before any handlers whose activities should be logged.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <param name="configureOptions">A <see cref="T:System.Action`1" /> to configure the provided <see cref="T:Serilog.AspNetCore.RequestLoggingOptions" />.</param>
         /// <param name="useMiddleware">Extended logging</param>
        /// <returns>The application builder.</returns>
        public static IApplicationBuilder AddSerilogRequestLogging(this IApplicationBuilder app, bool useMiddleware = true)
        {
            if (useMiddleware) app.UseMiddleware<SerilogRequestLogger>();
            return app.UseSerilogRequestLogging();
        }
    }
}
