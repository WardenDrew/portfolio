using System.Reflection;
using Platform.Legacy.Common.Models;

// ReSharper disable InconsistentNaming

namespace Platform.Legacy.Common.Services;

/// <summary>
/// The error code provider service interface
/// </summary>
public interface IErrorCodeService
{
	/// <summary>
	/// Scan the given assemblies for error code providers
	/// </summary>
	/// <param name="assemblies"></param>
	/// <returns></returns>
	List<IErrorCode> ScanErrorCodeProviders(params Assembly[] assemblies);

	/// <summary>
	/// Scan the assemblies via assembly markers for error code providers
	/// </summary>
	/// <param name="assemblyMarkers"></param>
	/// <returns></returns>
	List<IErrorCode> ScanErrorCodeProviders(params Type[] assemblyMarkers);
}

/// <summary>
/// refactor note: remove
/// </summary>
public interface IErrorCodeProvider { }

/// <summary>
/// An error code provider interface for scanning
/// </summary>
/// <typeparam name="TClass"></typeparam>
public interface IErrorCodeProvider<TClass> : IErrorCodeProvider { }

/// <summary>
/// Concrete implementation of an error code provider class
/// </summary>
/// <typeparam name="TClass"></typeparam>
public class ErrorCodeProvider<TClass> : IErrorCodeProvider<TClass>
{
	/// <summary>
	/// Create a new error
	/// </summary>
	/// <param name="name"></param>
	/// <param name="message"></param>
	/// <returns></returns>
	// ReSharper disable once MemberCanBeProtected.Global
	public static IErrorCode Error(string name, string message)
	{
		return new ErrorCode(name: name, section: typeof(TClass).FullName ?? typeof(TClass).Name,
			englishTranslation: message, httpStatusCode: 400);
	}

	/// <summary>
	/// passthrough the support code text
	/// </summary>
	// ReSharper disable once MemberCanBeProtected.Global
	// ReSharper disable once InconsistentNaming
	public static string SUPPORT => ErrorCode.SUPPORT;
}

/// <summary>
/// The error code provider generic for ef core entities
/// </summary>
/// <typeparam name="TClass"></typeparam>
public class EntityErrorCodeProvider<TClass> : ErrorCodeProvider<TClass>
{
	/// <summary>
	/// The NOT Found code for this entity
	/// </summary>
	public static IErrorCode NOT_FOUND =>
		ErrorCodeProvider<TClass>.Error(
			name: nameof(EntityErrorCodeProvider<TClass>.NOT_FOUND),
			message: $"Could not find the requested \'{typeof(TClass).Name}\' either it does not exist, or you do not have permission to access it. {ErrorCodeProvider<TClass>.SUPPORT}"
		);

	/// <summary>
	/// The already exists code for this entity
	/// </summary>
	public static IErrorCode ALREADY_EXISTS =>
		ErrorCodeProvider<TClass>.Error(
			name: nameof(EntityErrorCodeProvider<TClass>.ALREADY_EXISTS),
			message: $"The \'{typeof(TClass).Name}\' you attempted to create already exists! {ErrorCodeProvider<TClass>.SUPPORT}"
		);

	/// <summary>
	/// The not public code for this entity
	/// </summary>
	public static IErrorCode NOT_PUBLIC =>
		ErrorCodeProvider<TClass>.Error(
			name: nameof(EntityErrorCodeProvider<TClass>.NOT_PUBLIC),
			message: $"Could not display the requested \'{typeof(TClass).Name}\'. Public permission was not granted. {ErrorCodeProvider<TClass>.SUPPORT}"
		);

	/// <summary>
	/// The not authorized code for this entity
	/// </summary>
	public static IErrorCode NOT_AUTHORIZED =>
		ErrorCodeProvider<TClass>.Error(
			name: nameof(EntityErrorCodeProvider<TClass>.NOT_AUTHORIZED),
			message: $"Could not access or modify the requested \'{typeof(TClass).Name}\'. Permission was not authorized. {ErrorCodeProvider<TClass>.SUPPORT}"
		);
}

/// <inheritdoc />
public class ErrorCodeService : IErrorCodeService
{
	/// <inheritdoc />
	public List<IErrorCode> ScanErrorCodeProviders(params Assembly[] assemblies)
	{
		List<IErrorCode> result = [];

		foreach (Assembly assembly in assemblies)
		{
			IEnumerable<TypeInfo> errorCodeProviderTypes = assembly.DefinedTypes.Where(x =>
				typeof(IErrorCodeProvider).IsAssignableFrom(x) && x is { IsInterface: false, IsAbstract: false, }
			);

			foreach (TypeInfo providerType in errorCodeProviderTypes)
			{
				if (providerType.ContainsGenericParameters)
				{
					throw new InvalidOperationException(
						$"Error Code provider classes must not be contained inside a generic class as generic classes cannot be instantiated through reflection: {providerType.FullName}"
					);
				}

				List<object?> fieldValues = [.. providerType
					.GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
					.Where(x => typeof(IErrorCode).IsAssignableFrom(x.FieldType))
					.Where(x => x.IsInitOnly)
					.Select(x => x.GetValue(null)),];

				result.AddRange(fieldValues.OfType<IErrorCode>());
			}
		}

		return result;
	}

	/// <inheritdoc />
	public List<IErrorCode> ScanErrorCodeProviders(params Type[] assemblyMarkers)
	{
		List<Assembly> assemblies = [];

		foreach (Type assemblyMarker in assemblyMarkers)
		{
			if (!assemblies.Contains(assemblyMarker.Assembly))
			{
				assemblies.Add(assemblyMarker.Assembly);
			}
		}

		return ScanErrorCodeProviders(assemblies.ToArray());
	}
}
