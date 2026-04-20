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
            var firstError = context.ModelState
                .Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault() ?? "Dados Inválidos.";

                context.Result = new BadRequestObjectResult(new { error = firstError });
        }
    }

    public void OnActionExecuting(ActionExecutingContext context)
    { }
}