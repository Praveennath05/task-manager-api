using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using TaskManager.Application.Common;

namespace TaskManager.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        // ── MEDIATR ────────────────────────────────────────
        // Scans this assembly, registers all handlers automatically
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        // ─────────────────────────────────────────────────

        // ── FLUENT VALIDATION ──────────────────────────────
        // Scans this assembly, finds all classes that inherit
        // AbstractValidator<T> and registers them automatically
        // CreateTaskValidator, UpdateTaskValidator, RegisterValidator,
        // LoginValidator — all found and registered with one line
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        // ─────────────────────────────────────────────────

        // ── PIPELINE BEHAVIOR ──────────────────────────────
        // Registers ValidationBehavior into MediatR's pipeline
        // Every request now flows: Request → ValidationBehavior → Handler
        // Open generic registration — works for ALL commands/queries
        // without registering each one individually
        services.AddTransient(
            typeof(IPipelineBehavior<,>),
            typeof(ValidationBehavior<,>));
        // ─────────────────────────────────────────────────

        return services;
    }
}