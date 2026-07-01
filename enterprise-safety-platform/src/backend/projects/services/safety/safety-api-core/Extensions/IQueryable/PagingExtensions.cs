using System.Linq.Expressions;
using System.Reflection;

namespace Platform.Legacy.Core.Extensions.IQueryable;

public static class PagingExtensions
{
	// This whole chunk is extensions for Result Paging

	public static IOrderedQueryable<T> OrderBy<T>(this IQueryable<T> source, string property)
	{
		return PagingExtensions.ApplyOrder(source: source, property: property, methodName: "OrderBy");
	}

	public static IOrderedQueryable<T> OrderByDescending<T>(this IQueryable<T> source, string property)
	{
		return PagingExtensions.ApplyOrder(source: source, property: property, methodName: "OrderByDescending");
	}

	public static IOrderedQueryable<T> ThenBy<T>(this IOrderedQueryable<T> source, string property)
	{
		return PagingExtensions.ApplyOrder(source: source, property: property, methodName: "ThenBy");
	}

	public static IOrderedQueryable<T> ThenByDescending<T>(this IOrderedQueryable<T> source, string property)
	{
		return PagingExtensions.ApplyOrder(source: source, property: property, methodName: "ThenByDescending");
	}

	private static IOrderedQueryable<T> ApplyOrder<T>(IQueryable<T> source, string property, string methodName)
	{
		string[] props = property.Split('.');
		Type type = typeof(T);
		ParameterExpression arg = Expression.Parameter(type: type, name: "x");
		Expression expr = arg;
		foreach (string prop in props)
		{
			// use reflection (not ComponentModel) to mirror LINQ
			PropertyInfo? pi = type.GetProperty(
				name: prop,
				bindingAttr: BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance
			);
			if (pi is null)
			{
				continue;
			}

			expr = Expression.Property(expression: expr, property: pi);
			type = pi.PropertyType;
		}
		Type delegateType = typeof(Func<,>).MakeGenericType(typeof(T), type);
		LambdaExpression lambda = Expression.Lambda(delegateType: delegateType, body: expr, arg);

		object? result = typeof(Queryable)
			.GetMethods()
			.Single(method =>
				method.Name == methodName
				&& method.IsGenericMethodDefinition
				&& method.GetGenericArguments().Length == 2
				&& method.GetParameters().Length == 2
			)
			.MakeGenericMethod(typeof(T), type)
			.Invoke(obj: null, parameters: [source, lambda,]);
		if (result is not IOrderedQueryable<T> orderedQueryableResult)
		{
			throw new InvalidCastException("Somehow our ApplyOrder has gone horribly wrong!");
		}

		return orderedQueryableResult;
	}

	public static async Task<IResponse> PageResultsAsync<T>(
		this IQueryable<T> list,
		PagingParameters? paging,
		CancellationToken cancellationToken = default
	)
	{
		ResponseMeta meta = new() { NumTotal = await list.CountAsync(cancellationToken), };

		if (paging != null)
		{
			if (string.IsNullOrWhiteSpace(paging.OrderByField))
			{
				return Response.FromError(Enums.ErrorCodes.Paging.ORDERBY_MISSING);
			}

			string lowerName = paging.OrderByField.ToLower();
			if (lowerName.Contains("password") || lowerName.Contains("concurrency") || lowerName.Contains("security"))
			{
				return Response.FromError(Enums.ErrorCodes.Paging.ORDERBY_PROHIBITED_FIELD);
			}

			if (paging.SkipNum < 0)
			{
				return Response.FromError(Enums.ErrorCodes.Paging.ORDERBY_SKIPNUM_OUT_OF_RANGE);
			}

			if (paging.TakeNum < 1)
			{
				return Response.FromError(Enums.ErrorCodes.Paging.ORDERBY_TAKENUM_OUT_OF_RANGE);
			}

			try
			{
				if (paging.Ascending)
				{
					list = list.OrderBy(paging.OrderByField);
				}
				else
				{
					list = list.OrderByDescending(paging.OrderByField);
				}
			}
			catch (ArgumentNullException)
			{
				return Response.FromError(Enums.ErrorCodes.Paging.ORDERBY_FIELD_DOES_NOT_EXIST);
			}

			list = list.Skip(paging.SkipNum);
			list = list.Take(paging.TakeNum);
		}

		List<T>? final;
		try
		{
			final = await list.ToListAsync(cancellationToken);
		}
		catch (InvalidOperationException)
		{
			return Response.FromError(Enums.ErrorCodes.Paging.ORDERBY_FIELD_DOES_NOT_EXIST);
		}

		meta.NumReturned = final.Count;

		if (paging != null)
		{
			meta.NumSkipped = paging.SkipNum;
		}
		else
		{
			meta.NumSkipped = 0;
		}

		return Response.FromSuccess().WithData(final).WithMeta(meta);
	}

	// End of extensions for Result Paging
}
