using Microsoft.AspNetCore.Mvc;
using static Microsoft.AspNetCore.Http.StatusCodes;
using System.Text.Json;
using Theoremone.SmartAc.Application.Exceptions;
using Theoremone.SmartAc.Application.Wrappers;

namespace Theoremone.SmartAc.Middlewares
{
    public class ApiErrorHandler
    {
        private readonly RequestDelegate _next;
        private ILogger<ApiErrorHandler> _logger;

        public ApiErrorHandler(RequestDelegate next, ILoggerFactory loggerFactory)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger<ApiErrorHandler>();
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception error)
            {
                _logger.LogError(error, error.Message);
                var response = context.Response;
                response.ContentType = "application/json";
                var responseModelString = string.Empty;
                var responseModel = new Response<string>()
                {
                    Succeeded = false,
                    Data = "Operation failed.",
                    Messages = new List<string>()
                };
                switch (error)
                {
                    case ValidationProblemException e:
                        response.StatusCode = Status400BadRequest;
                        Dictionary<string, string[]> dict = new() {
                            {e.Key, new []{e.Message}}
                        };
                        responseModelString = JsonSerializer.Serialize(new ValidationProblemDetails(dict));
                        await response.WriteAsync(responseModelString);
                        break;
                    case ProblemException e:
                        response.StatusCode = Status401Unauthorized;
                        var problemDetail = new ProblemDetails()
                        {
                            Detail = "Something is wrong on the information provided, please review.",
                            Status = Status401Unauthorized
                        };
                        responseModelString = JsonSerializer.Serialize(problemDetail);
                        await response.WriteAsync(responseModelString);
                        break;
                    case NotFoundException e:
                        responseModel.Messages.Add(e.Message);
                        response.StatusCode = Status404NotFound;
                        responseModel.Data = null;
                        await response.WriteAsJsonAsync(responseModel);
                        break;
                    case ValidationException e:
                        response.StatusCode = Status400BadRequest;
                        responseModel.Messages.AddRange(e.Errors);
                        await response.WriteAsJsonAsync(responseModel);
                        break;
                    default:
                        response.StatusCode = Status500InternalServerError;
                        await response.WriteAsJsonAsync(responseModel);
                        break;
                }
            }
        }
    }
}
