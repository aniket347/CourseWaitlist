using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
namespace CourseWaitlist.Api.Auth;

public sealed class ApiExceptionFilter : IAsyncExceptionFilter
{
    public Task OnExceptionAsync(ExceptionContext context)
    {
        context.Result = new ObjectResult(new { error = "An unexpected error occurred." }) { StatusCode = StatusCodes.Status500InternalServerError };
        context.ExceptionHandled = true;
        return Task.CompletedTask;
    }
}
