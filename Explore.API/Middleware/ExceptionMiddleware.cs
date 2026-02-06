using Newtonsoft.Json;
using System.Net;
using Explore.Application.Exceptions;

namespace Explore.API.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }
        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Une exception non gérée s'est produite: {Message}", ex.Message);
                await HandleExceptionAsync(httpContext, ex);
            }
        }
        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            HttpStatusCode statusCode;
            // HttpStatusCode statusCode = HttpStatusCode.InternalServerError;
            string result = JsonConvert.SerializeObject(new ErrorDetails()
            {
                ErrorMessage = exception.Message,
                ErrorType = "Failure",
                StackTrace = exception.StackTrace
            });

            switch (exception)
            {
                // Maybe use Messages class where there is const for error messages! inside the static folder

                case BadRequestException badRequestException:
                    statusCode = HttpStatusCode.BadRequest;
                    _logger.LogWarning("Invalid Request: {Message}", exception.Message);
                    break;
                case ValidationException validationException:
                    statusCode = HttpStatusCode.BadRequest;
                    result = JsonConvert.SerializeObject(validationException.Errors);
                    _logger.LogWarning("Validation Error: {Message}", exception.Message);
                    break;
                case NotFoundException notFoundException:
                    statusCode = HttpStatusCode.NotFound;
                    _logger.LogWarning("Ressource Not Found: {Message}", exception.Message);
                    break;
                default:
                    statusCode = HttpStatusCode.InternalServerError;
                    _logger.LogError("Internal Server Error: {Message}", exception.Message);
                    break;
            }

            context.Response.StatusCode = (int)statusCode;
            return context.Response.WriteAsync(result);
        }
    }
    public class ErrorDetails
    {
        public string ErrorType { get; set; }
        public string ErrorMessage { get; set; }
        public string? StackTrace { get; set; }
    }
}

