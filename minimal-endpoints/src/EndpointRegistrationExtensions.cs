using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace IronWatch.MediatR.MinimalEndpoints;

public static class EndpointRegistrationExtensions
{
	public static IServiceCollection AddRegisteredEndpointTracker(this IServiceCollection services)
	{
		return services.AddSingleton<RegisteredEndpointsService>();
	}
	
	public static IEndpointRouteBuilder BuildEndpoints(this IEndpointRouteBuilder endpointRouteBuilder, Assembly assembly)
	{
		try
		{
			_ = endpointRouteBuilder.ServiceProvider.GetRequiredService<IMediator>();
		}
		catch (InvalidOperationException)
		{
			throw new InvalidOperationException("Critical error, refusing to build endpoitns as an IMediator was not found in the service provider. The endpoints this method would build will not work!");
		}
		
		IEnumerable<Type> endpointTypes = assembly.GetExportedTypes()
			.Where(x => x.GetCustomAttributes<ApiEndpointAttribute>().Any());

		IEnumerable<Type> responseModelTypes = assembly.GetExportedTypes()
			.Where(x => x.GetCustomAttributes<ResponseModelAttribute>().Any());

		MethodInfo? registerEndpointMethodInfo = typeof(EndpointRegistrationExtensions)
					.GetMethod(nameof(RegisterEndpoint), BindingFlags.NonPublic | BindingFlags.Static);
		if (registerEndpointMethodInfo is null)
		{
			throw new ApplicationException($"Could not find the method {nameof(RegisterEndpoint)}");
		}

        MethodInfo? registerEndpointWithFormMethodInfo = typeof(EndpointRegistrationExtensions)
                    .GetMethod(nameof(RegisterEndpointWithForm), BindingFlags.NonPublic | BindingFlags.Static);
        if (registerEndpointWithFormMethodInfo is null)
        {
            throw new ApplicationException($"Could not find the method {nameof(RegisterEndpointWithForm)}");
        }

        MethodInfo? registerEndpointWithFormChildPropertyMethodInfo = typeof(EndpointRegistrationExtensions)
                    .GetMethod(nameof(RegisterEndpointWithFormChildProperty), BindingFlags.NonPublic | BindingFlags.Static);
        if (registerEndpointWithFormChildPropertyMethodInfo is null)
        {
            throw new ApplicationException($"Could not find the method {nameof(RegisterEndpointWithFormChildProperty)}");
        }

        MethodInfo? registerEndpointWithFormChildPropertyAndParamMethodInfo = typeof(EndpointRegistrationExtensions)
                    .GetMethod(nameof(RegisterEndpointWithFormChildPropertyAndParam), BindingFlags.NonPublic | BindingFlags.Static);
        if (registerEndpointWithFormChildPropertyAndParamMethodInfo is null)
        {
            throw new ApplicationException($"Could not find the method {nameof(RegisterEndpointWithFormChildPropertyAndParam)}");
        }

        RegisteredEndpointsService? registeredEndpointsService = endpointRouteBuilder.ServiceProvider.GetService<RegisteredEndpointsService>();

		Dictionary<Type, Type> responseModelsForRequests = new();

		foreach (Type responseModelType in responseModelTypes)
		{
			ResponseModelAttribute? responseModelAttribute = responseModelType.GetCustomAttribute<ResponseModelAttribute>();
			if (responseModelAttribute is null)
			{
				throw new ApplicationException($"Response model type does not have a response model attribute but ended up in the responseModelTypes list: {responseModelType.Name}");
			}

			responseModelsForRequests.Add(responseModelAttribute.RequestType, responseModelType);
		}

		foreach (Type endpointType in endpointTypes)
		{
			ApiEndpointAttribute? endpointAttribute = endpointType.GetCustomAttribute<ApiEndpointAttribute>();
			if (endpointAttribute is null)
			{
				throw new ApplicationException($"Endpoint class does not have an endpoint attribute but ended up in the endpointTypes list: {endpointType.Name}");
			}

			Type? requestHandlerInterface = endpointType.GetInterfaces()
				.Where(x => x.IsGenericType)
				.Where(x => x.GetGenericTypeDefinition() == typeof(IRequestHandler<,>))
				.FirstOrDefault();
			if (requestHandlerInterface is null)
			{
                throw new ApplicationException($"Endpoint class does not inherit {typeof(IRequestHandler<,>).FullName}: {endpointType.Name}");
            }

			List<Type> genericArguments = requestHandlerInterface.GetGenericArguments().ToList();
			if (genericArguments.Count != 2)
			{
                throw new ApplicationException($"Endpoint class does not pass exactly 2 generic type arguments to IRequestHandler: {endpointType.Name}");
            }

			if (genericArguments[1] != typeof(IResult))
			{
                throw new ApplicationException($"Endpoint class does not specify IResult as the TResponse generic type argument in IRequestHandler: {endpointType.Name}");
            }

			RegisteredEndpoint registeredEndpoint = new()
			{
				Method = endpointAttribute.Method,
				Route = endpointAttribute.Route,
				HandlerType = endpointType,
				HandlerAttributes = endpointType.GetCustomAttributes().ToList(),
				RequestType = genericArguments[0],
				RequestAttributes = genericArguments[0].GetCustomAttributes().ToList()
            };

            AsFormAttribute? asFormAttribute = registeredEndpoint.RequestAttributes
				.Where(x => typeof(AsFormAttribute).IsAssignableFrom(x.GetType()))
				.Cast<AsFormAttribute>()
				.FirstOrDefault();

            if (asFormAttribute is not null)
            {
                if (asFormAttribute.FormPropertyName is null && asFormAttribute.ParamPropertyName is null)
                {
                    _ = registerEndpointWithFormMethodInfo.MakeGenericMethod(registeredEndpoint.RequestType)
                    .Invoke(null,
                    [
                        endpointRouteBuilder,
                        registeredEndpoint,
                        asFormAttribute,
                        responseModelsForRequests.GetValueOrDefault(endpointType)
                    ]);
                }
                else if (asFormAttribute.FormPropertyName is not null && asFormAttribute.ParamPropertyName is not null)
                {
                    PropertyInfo? formProperty = registeredEndpoint.RequestType.GetProperty(asFormAttribute.FormPropertyName);
                    if (formProperty is null)
                    {
                        throw new InvalidOperationException($"Could not find a property for form binding with name {asFormAttribute.FormPropertyName} on {registeredEndpoint.RequestType.FullName}");
                    }

                    PropertyInfo? paramProperty = registeredEndpoint.RequestType.GetProperty(asFormAttribute.ParamPropertyName);
                    if (paramProperty is null)
                    {
                        throw new InvalidOperationException($"Could not find a property for param binding with name {asFormAttribute.ParamPropertyName} on {registeredEndpoint.RequestType.FullName}");
                    }

                    _ = registerEndpointWithFormChildPropertyAndParamMethodInfo.MakeGenericMethod(registeredEndpoint.RequestType, formProperty.PropertyType, paramProperty.PropertyType)
                    .Invoke(null,
                    [
                        endpointRouteBuilder,
                        registeredEndpoint,
                        asFormAttribute,
                        formProperty,
                        paramProperty,
                        responseModelsForRequests.GetValueOrDefault(endpointType)
                    ]);
                }
                else if (asFormAttribute.FormPropertyName is not null && asFormAttribute.ParamPropertyName is null)
                {
                    PropertyInfo? formProperty = registeredEndpoint.RequestType.GetProperty(asFormAttribute.FormPropertyName);
                    if (formProperty is null)
                    {
                        throw new InvalidOperationException($"Could not find a property for form binding with name {asFormAttribute.FormPropertyName} on {registeredEndpoint.RequestType.FullName}");
                    }

                    _ = registerEndpointWithFormChildPropertyMethodInfo.MakeGenericMethod(registeredEndpoint.RequestType, formProperty.PropertyType)
                    .Invoke(null,
                    [
                        endpointRouteBuilder,
                        registeredEndpoint,
                        asFormAttribute,
                        formProperty,
                        responseModelsForRequests.GetValueOrDefault(endpointType)
                    ]);
                }
                else
                {
					throw new InvalidOperationException($"{nameof(AsFormAttribute)} Unsupported use of param property only!");
				}
            }
            else
            {
                _ = registerEndpointMethodInfo.MakeGenericMethod(registeredEndpoint.RequestType)
					.Invoke(null,
					[
						endpointRouteBuilder,
						registeredEndpoint,
						responseModelsForRequests.GetValueOrDefault(endpointType)
					]);
            }

			if (registeredEndpointsService is not null)
			{
				registeredEndpointsService.Add(registeredEndpoint);
			}
		}

		return endpointRouteBuilder;
	}

	private static string RelativePathCombine(params string[] paths)
	{
		List<string> parts = new();
		foreach (string path in paths)
		{
			parts.AddRange(path
			.Replace('\\', '/')
			.Trim('/')
			.ToLower()
			.Split('/'));
		}

		string result = String.Empty;

		foreach (string part in parts)
		{
			if (string.IsNullOrWhiteSpace(part))
			{
				continue;
			}

			result += $"/{HttpUtility.UrlEncode(part)}";
		}

		return result;
	}

	private static RouteHandlerBuilder RegisterEndpointWithFormChildPropertyAndParam<TRequest, TForm, TParam>(
		IEndpointRouteBuilder endpointRouteBuilder,
		RegisteredEndpoint registeredEndpoint,
		AsFormAttribute asFormAttribute,
		PropertyInfo formPropertyInfo,
        PropertyInfo paramPropertyInfo,
        Type? responseModelType)
		where TRequest : IRequest<IResult>
		where TForm : class
        where TParam : class
    {
		RouteHandlerBuilder routeHandlerBuilder = endpointRouteBuilder.MapMethods(
                registeredEndpoint.Route,
                new List<string> { registeredEndpoint.Method },
                async (
                    CancellationToken cancellationToken,
                    IMediator mediator,
                    [FromForm] TForm formObject,
                    [AsParameters] TParam paramObject
                ) =>
                {
					TRequest request = Activator.CreateInstance<TRequest>();
                    formPropertyInfo.SetValue(request, formObject, null);
                    paramPropertyInfo.SetValue(request, paramObject, null);

                    return await mediator.Send(request, cancellationToken);
                });

        if (!asFormAttribute.EnableAntiForgery)
        {
            routeHandlerBuilder = routeHandlerBuilder.DisableAntiforgery();
        }

        return routeHandlerBuilder.AttachMetadata(registeredEndpoint, responseModelType);
    }

    private static RouteHandlerBuilder RegisterEndpointWithFormChildProperty<TRequest, TForm>(
        IEndpointRouteBuilder endpointRouteBuilder,
        RegisteredEndpoint registeredEndpoint,
        AsFormAttribute asFormAttribute,
        PropertyInfo formPropertyInfo,
        Type? responseModelType)
        where TRequest : IRequest<IResult>
        where TForm : class
    {
        RouteHandlerBuilder routeHandlerBuilder = endpointRouteBuilder.MapMethods(
                registeredEndpoint.Route,
                new List<string> { registeredEndpoint.Method },
                async (
                    CancellationToken cancellationToken,
                    IMediator mediator,
                    [FromForm] TForm formObject
                ) =>
                {
                    TRequest request = Activator.CreateInstance<TRequest>();
                    formPropertyInfo.SetValue(request, formObject, null);

                    return await mediator.Send(request, cancellationToken);
                });

        if (!asFormAttribute.EnableAntiForgery)
        {
            routeHandlerBuilder = routeHandlerBuilder.DisableAntiforgery();
        }

        return routeHandlerBuilder.AttachMetadata(registeredEndpoint, responseModelType);
    }

    private static RouteHandlerBuilder RegisterEndpointWithForm<TForm>(
        IEndpointRouteBuilder endpointRouteBuilder,
        RegisteredEndpoint registeredEndpoint,
        AsFormAttribute asFormAttribute,
        Type? responseModelType)
        where TForm : IRequest<IResult>
    {
        RouteHandlerBuilder routeHandlerBuilder = endpointRouteBuilder.MapMethods(
                registeredEndpoint.Route,
                new List<string> { registeredEndpoint.Method },
                async (
                    CancellationToken cancellationToken,
                    IMediator mediator,
                    [FromForm] TForm form
                ) =>
                {
                    return await mediator.Send(form, cancellationToken);
                });

        if (!asFormAttribute.EnableAntiForgery)
        {
            routeHandlerBuilder = routeHandlerBuilder.DisableAntiforgery();
        }

        return routeHandlerBuilder.AttachMetadata(registeredEndpoint, responseModelType);

    }

    private static RouteHandlerBuilder RegisterEndpoint<TRequest>(
		IEndpointRouteBuilder endpointRouteBuilder,
		RegisteredEndpoint registeredEndpoint,
		Type? responseModelType)
		where TRequest : IRequest<IResult>
	{
		RouteHandlerBuilder routeHandlerBuilder = endpointRouteBuilder.MapMethods(
                registeredEndpoint.Route,
                new List<string> { registeredEndpoint.Method },
                async (
                    CancellationToken cancellationToken,
                    IMediator mediator,
                    [AsParameters] TRequest request
                ) =>
                {
                    return await mediator.Send(request, cancellationToken);
                });

		return routeHandlerBuilder.AttachMetadata(registeredEndpoint, responseModelType);
    }

	public static RouteHandlerBuilder AttachMetadata(
        this RouteHandlerBuilder routeHandlerBuilder,
        RegisteredEndpoint registeredEndpoint,
        Type? responseModelType)
	{
        List<Attribute> metadataAttributes = registeredEndpoint.HandlerAttributes
            .Where(x => typeof(IRouteMetadataAttribute).IsAssignableFrom(x.GetType()))
            .ToList();
        routeHandlerBuilder.WithMetadata(metadataAttributes);

        // Open API pieces
        routeHandlerBuilder = routeHandlerBuilder
            .Produces(200, responseModelType);

        return routeHandlerBuilder;
    }
}
