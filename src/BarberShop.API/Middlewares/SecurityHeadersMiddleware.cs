namespace BarberShop.API.Middlewares;

public class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // Impede que o browser adivinhe o tipo do conteúdo
        // Protege contra ataques de MIME sniffing
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";

        // Impede que a página seja carregada em iframe
        // Protege contra ataques de clickjacking
        context.Response.Headers["X-Frame-Options"] = "DENY";

        // Não envia o referrer para outros domínios
        // Evita vazamento de URLs internas
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Remove o header que revela que é ASP.NET Core
        // Dificulta que atacantes identifiquem a tecnologia usada
        context.Response.Headers.Remove("Server");
        context.Response.Headers.Remove("X-Powered-By");

        await next(context);
    }
}