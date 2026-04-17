using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BarberShop.API.Filters;

public class ValidationFilter : IActionFilter
{
    public void OnActionExecuted(ActionExecutedContext context)
    {
        if (!context.ModelState.IsValid)
        {
            // Pega apenas o primeiro erro de cada campo
            var errors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .Select(x => x.Value!.Errors.First().ErrorMessage)
                .ToList();

                context.Result = new BadRequestObjectResult(new { error = errors.First() });
        }
    }

    public void OnActionExecuting(ActionExecutingContext context)
    { }
}