using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class ServiceExtension
{
    public static void AddApplicationLayer(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();
        
        // AutoMapper removed - disabled due to .NET 10 reflection incompatibility, manual mapping is used
        services.AddValidatorsFromAssembly(assembly);
        services.AddMediatR(assembly);
    }
}