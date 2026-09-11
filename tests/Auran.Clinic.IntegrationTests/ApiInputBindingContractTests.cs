using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Auran.Clinic.IntegrationTests;

public sealed class ApiInputBindingContractTests
{
    [Fact]
    public void ControllerRoutes_DoNotUseRouteParameters()
    {
        var violations = GetControllerActions()
            .SelectMany(item =>
            {
                var controllerTemplates = item.Controller
                    .GetCustomAttributes<RouteAttribute>()
                    .Select(attribute => attribute.Template);
                var actionTemplates = item.Action
                    .GetCustomAttributes<HttpMethodAttribute>()
                    .Select(attribute => attribute.Template)
                    .Where(template => !string.IsNullOrWhiteSpace(template))
                    .Select(template => template!);

                return controllerTemplates
                    .Concat(actionTemplates)
                    .Where(template => template.Contains('{', StringComparison.Ordinal))
                    .Select(template => $"{item.Controller.Name}.{item.Action.Name}: {template}");
            })
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Route parameters are not allowed. Violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void ControllerActions_UseAtMostOneAllowedClientBindingSource()
    {
        var violations = new List<string>();

        foreach (var (controller, action) in GetControllerActions())
        {
            var clientParameters = action.GetParameters()
                .Where(parameter => parameter.ParameterType != typeof(CancellationToken))
                .Where(parameter => parameter.GetCustomAttribute<FromServicesAttribute>() is null)
                .ToArray();

            var sources = new List<string>();
            foreach (var parameter in clientParameters)
            {
                var source = GetBindingSource(parameter);
                if (source is null)
                {
                    violations.Add($"{controller.Name}.{action.Name}: parameter '{parameter.Name}' has no explicit Body/Query/Form binding.");
                    continue;
                }

                if (source == "Route")
                {
                    violations.Add($"{controller.Name}.{action.Name}: parameter '{parameter.Name}' uses route binding.");
                    continue;
                }

                sources.Add(source);
            }

            var distinctSources = sources.Distinct(StringComparer.Ordinal).ToArray();
            if (distinctSources.Length > 1)
            {
                violations.Add(
                    $"{controller.Name}.{action.Name}: mixes binding sources {string.Join(", ", distinctSources)}.");
            }
        }

        Assert.True(
            violations.Count == 0,
            $"Each endpoint must use one client input style only: Body, Query, or Form. Violations: {string.Join(" | ", violations)}");
    }

    private static IEnumerable<(Type Controller, MethodInfo Action)> GetControllerActions()
    {
        return typeof(Program).Assembly
            .GetTypes()
            .Where(type => type is { IsAbstract: false, IsPublic: true }
                           && typeof(ControllerBase).IsAssignableFrom(type))
            .SelectMany(controller => controller
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(method => method.GetCustomAttributes<HttpMethodAttribute>().Any())
                .Select(action => (controller, action)));
    }

    private static string? GetBindingSource(ParameterInfo parameter)
    {
        if (parameter.GetCustomAttribute<FromBodyAttribute>() is not null)
            return "Body";
        if (parameter.GetCustomAttribute<FromQueryAttribute>() is not null)
            return "Query";
        if (parameter.GetCustomAttribute<FromFormAttribute>() is not null)
            return "Form";
        if (parameter.GetCustomAttribute<FromRouteAttribute>() is not null)
            return "Route";

        return null;
    }
}
