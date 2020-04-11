using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Serilog;
using Serilog.Context;

namespace NetExtensions
{
    // source: https://www.carlrippon.com/adding-useful-information-to-asp-net-core-web-api-serilog-logs/
    public class SerilogRequestLogger
    {
        private readonly RequestDelegate _next;

        public SerilogRequestLogger(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (httpContext == null) throw new ArgumentNullException(nameof(httpContext));

            // Push the user name into the log context so that it is included in all log entries
            LogContext.PushProperty("UserName", httpContext.User.Identity.Name);

            // Getting the request body is a little tricky because it's a stream
            // So, we need to read the stream and then rewind it back to the beginning
            var requestBody = "";
            httpContext.Request.EnableBuffering();
            var body = httpContext.Request.Body;
            var buffer = new byte[Convert.ToInt32(httpContext.Request.ContentLength)];
            await httpContext.Request.Body.ReadAsync(buffer, 0, buffer.Length);
            requestBody = Encoding.UTF8.GetString(buffer);
            body.Seek(0, SeekOrigin.Begin);
            httpContext.Request.Body = body;

            Log.ForContext("RequestHeaders", httpContext.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()), true)
                .ForContext("RequestBody", requestBody)
                .Warning("Request information {RequestMethod} {RequestPath} information", httpContext.Request.Method, httpContext.Request.Path);


            // The reponse body is also a stream so we need to:
            // - hold a reference to the original response body stream
            // - re-point the response body to a new memory stream
            // - read the response body after the request is handled into our memory stream
            // - copy the response in the memory stream out to the original response stream
            await using var responseBodyMemoryStream = new MemoryStream();
            var originalResponseBodyReference = httpContext.Response.Body;
            httpContext.Response.Body = responseBodyMemoryStream;

            await _next(httpContext);

            httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(httpContext.Response.Body).ReadToEndAsync().ConfigureAwait(false);
            httpContext.Response.Body.Seek(0, SeekOrigin.Begin);

            Log.ForContext("RequestBody", requestBody)
                .ForContext("ResponseBody", responseBody)
                .Warning("Response information {RequestMethod} {RequestPath} {Host} {statusCode}",
                    httpContext.Request.Method,
                    httpContext.Request.Path,
                    httpContext.Request.GetDisplayUrl(),
                    httpContext.Response.StatusCode);
            await responseBodyMemoryStream.CopyToAsync(originalResponseBodyReference);
        }
    }
}
