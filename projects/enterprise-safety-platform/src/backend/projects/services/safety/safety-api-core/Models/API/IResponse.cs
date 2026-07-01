using Newtonsoft.Json;

namespace Platform.Legacy.Core.Models.API;

public interface IResponse
{
	/// <summary>
	/// Whether the call was Successful or not
	/// </summary>
	bool Success { get; set; }

	/// <summary>
	/// An Explicitly set status code to return
	/// </summary>
	int? OverridenStatusCode { get; set; }

	/// <summary>
	/// Errors encounted during processing of the call
	/// </summary>
	List<IErrorCode> Errors { get; set; }

	/// <summary>
	/// Whether there are any errors or not
	/// </summary>
	bool HasErrors { get; }

	/// <summary>
	/// The number of errors
	/// </summary>
	int ErrorCount { get; }

	/// <summary>
	/// Add data to the result
	/// </summary>
	/// <typeparam name="T">The type of data being returned</typeparam>
	/// <param name="data">The data being returned</param>
	/// <returns></returns>
	IResponse<T> WithData<T>(T data);

	/// <summary>
	/// Add an error code to the result
	/// </summary>
	/// <param name="error"></param>
	/// <returns></returns>
	IResponse WithError(IErrorCode? error);

	/// <summary>
	/// Explicitly set the status code for the result, rather than letting it be determined automatically
	/// </summary>
	/// <param name="statusCode"></param>
	/// <returns></returns>
	IResponse WithStatusCode(int statusCode);

	/// <summary>
	/// Used by the Middleware to determine the status code
	/// </summary>
	/// <returns></returns>
	int CalculateStatusCode();

	/// <summary>
	/// Serialize the Response using the default json serialization settings
	/// </summary>
	/// <returns></returns>
	string Serialize();

	/// <summary>
	/// Serialize the Response with the specified json serialization settings
	/// </summary>
	/// <param name="serializerSettings"></param>
	/// <returns></returns>
	string Serialize(JsonSerializerSettings serializerSettings);

	/// <summary>
	/// Override of ToString, should generally call Serialize(). Uses the default json serialization settings
	/// </summary>
	/// <returns></returns>
	string ToString();

	/// <summary>
	/// Metadata to return. Currently is useful for array responses
	/// </summary>
	IResponseMeta? Meta { get; set; }

	/// <summary>
	/// Set the metadata in the result
	/// </summary>
	/// <param name="meta"></param>
	/// <returns></returns>
	IResponse WithMeta(IResponseMeta? meta);
}

public interface IResponse<T> : IResponse
{
	/// <summary>
	/// Data to return
	/// </summary>
	T? Data { get; set; }

	/// <summary>
	/// Set the data in the result
	/// </summary>
	/// <param name="data"></param>
	/// <returns></returns>
	IResponse<T> WithData(T? data);

	/// <summary>
	/// Set the metadata in the result
	/// </summary>
	/// <param name="meta"></param>
	/// <returns></returns>
	new IResponse<T> WithMeta(IResponseMeta? meta);

	/// <summary>
	/// Add an error code to the result
	/// </summary>
	/// <param name="error"></param>
	/// <returns></returns>
	new IResponse<T> WithError(IErrorCode? error);

	/// <summary>
	/// Explicitly set the status code for the result, rather than letting it be determined automatically
	/// </summary>
	/// <param name="statusCode"></param>
	/// <returns></returns>
	new IResponse<T> WithStatusCode(int statusCode);
}
