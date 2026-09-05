using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace UserService.Api.Swagger;

/// <summary>
/// Only marks an operation as requiring the "Bearer" scheme in Swagger when the endpoint
/// actually has [Authorize] and isn't overridden by [AllowAnonymous] — otherwise every
/// endpoint (including public ones like register/login) would show a misleading padlock.
/// </summary>
public class AuthorizeCheckOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;

        var requiresAuth = metadata.OfType<AuthorizeAttribute>().Any()
            && !metadata.OfType<AllowAnonymousAttribute>().Any();

        if (!requiresAuth)
        {
            return;
        }

        operation.Security.Add(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        });
    }
}
