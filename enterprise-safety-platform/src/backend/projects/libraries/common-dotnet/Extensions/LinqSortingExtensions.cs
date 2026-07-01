using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Platform.Common.Extensions;

/// <summary>
/// Sorting extensions for LINQ (EF Core Compatible)
/// </summary>
public static class LinqSortingExtensions
{
	private static IOrderedQueryable<TSource> _buildOrderedQueryable<TSource>(
		MethodInfo method, 
		IQueryable<TSource> query,
		string propertyName)
	{
		Type entityType = typeof(TSource);

		PropertyInfo? propertyInfo = entityType.GetProperty(
			name: propertyName, 
			bindingAttr: BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
		if (propertyInfo == null)
		{
			throw new ArgumentOutOfRangeException(
				paramName: nameof(propertyName), 
				message: $"Unknown column:{propertyName}");
		}

		ParameterExpression arg = Expression.Parameter(
			type: entityType, 
			name: "x");
		MemberExpression property = Expression.Property(
			expression: arg, 
			propertyName: propertyName);
		LambdaExpression selector = Expression.Lambda(
			body: property, 
			parameters: [arg,]);

		MethodInfo genericMethod = method.MakeGenericMethod(
			entityType, 
			propertyInfo.PropertyType);

		return genericMethod.Invoke(
					obj: genericMethod,
					parameters: [query, selector,])
				as IOrderedQueryable<TSource>
			?? throw new InvalidCastException("Could not cast the ordered queryable when paginatin!");
	}
	
	private static readonly MethodInfo s_OrderBy = typeof(Queryable)
		.GetMethods()
		.Single(m => m is
		{
			Name: "OrderBy", 
			IsGenericMethodDefinition: true,
		} && m.GetParameters().Length == 2);

	private static readonly MethodInfo s_OrderByDescending = typeof(Queryable)
		.GetMethods()
		.Single(m => m is
		{
			Name: "OrderByDescending", 
			IsGenericMethodDefinition: true,
		} && m.GetParameters().Length == 2);

	private static readonly MethodInfo s_ThenBy = typeof(Queryable)
		.GetMethods()
		.Single(m => m is
		{
			Name: "ThenBy", 
			IsGenericMethodDefinition: true,
		} && m.GetParameters().Length == 2);

	private static readonly MethodInfo s_ThenByDescending = typeof(Queryable)
		.GetMethods()
		.Single(m => m is
		{
			Name: "ThenByDescending", 
			IsGenericMethodDefinition: true,
		} && m.GetParameters().Length == 2);

	/// <summary>
	/// Order By
	/// </summary>
	/// <param name="query"></param>
	/// <param name="propertyName"></param>
	/// <param name="descending"></param>
	/// <returns></returns>
	public static IOrderedQueryable<TSource> OrderBy<TSource>(
		this IQueryable<TSource> query, 
		string propertyName,
		bool descending = false)
	{
		if (descending)
		{
			return LinqSortingExtensions._buildOrderedQueryable(
				method: LinqSortingExtensions.s_OrderByDescending, 
				query: query, 
				propertyName: propertyName);
		}
		
		return LinqSortingExtensions._buildOrderedQueryable(
			method: LinqSortingExtensions.s_OrderBy, 
			query: query, 
			propertyName: propertyName);
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="query"></param>
	/// <param name="propertyName"></param>
	/// <param name="descending"></param>
	/// <returns></returns>
	public static IOrderedQueryable<TSource> ThenBy<TSource>(
		this IQueryable<TSource> query, 
		string propertyName,
		bool descending = false)
	{
		if (descending)
		{
			return LinqSortingExtensions._buildOrderedQueryable(
				method: LinqSortingExtensions.s_ThenByDescending, 
				query: query, 
				propertyName: propertyName);
		}

		return LinqSortingExtensions._buildOrderedQueryable(
			method: LinqSortingExtensions.s_ThenBy, 
			query: query, 
			propertyName: propertyName);
	}
}
