using Newtonsoft.Json;

namespace Platform.Legacy.Core.Models.API;

public class Response : IResponse
{
	/// <inheritdoc/>
	public bool Success { get; set; }

	/// <inheritdoc/>
	public int? OverridenStatusCode { get; set; }

	/// <inheritdoc/>
	public List<IErrorCode> Errors { get; set; }

	/// <inheritdoc/>
	public bool HasErrors => this.Errors.Count != 0;

	/// <inheritdoc/>
	public int ErrorCount => this.Errors.Count;

	protected Response()
	{
		this.Errors = [];
	}

	/// <summary>
	/// Create a new Successful Result
	/// </summary>
	/// <returns></returns>
	public static IResponse FromSuccess()
	{
		Response result = new() { Success = true, };
		return result;
	}

	/// <summary>
	/// Create a new result from an Error
	/// </summary>
	/// <param name="code"></param>
	/// <returns></returns>
	public static IResponse FromError(IErrorCode? code)
	{
		Response result = new();
		if (code is not null)
		{
			result.Errors.Add(code);
		}
		return result;
	}

	/// <summary>
	/// Return a result with an unspecified error
	/// </summary>
	/// <returns></returns>
	public static IResponse FromError()
	{
		return Response.FromError(Enums.ErrorCodes.Internal.UNSPECIFIED);
	}

	/// <inheritdoc/>
	public IResponse WithError(IErrorCode? code)
	{
		if (code is not null)
		{
			this.Errors.Add(code);
		}
		return this;
	}

	/// <inheritdoc/>
	public IResponse WithStatusCode(int statusCode)
	{
		this.OverridenStatusCode = statusCode;
		return this;
	}

	/// <inheritdoc/>
	public IResponse<T> WithData<T>(T? data)
	{
		return Response<T>.FromResponse(this).WithData(data);
	}

	/// <inheritdoc/>
	public int CalculateStatusCode()
	{
		if (this.OverridenStatusCode.HasValue)
		{
			return this.OverridenStatusCode.Value;
		}

		if (this.HasErrors)
		{
			//tiered
			bool has500 = false;
			bool has501 = false;
			bool has401 = false;
			bool has403 = false;
			bool has400 = false;

			foreach (IErrorCode code in this.Errors)
			{
				if (!code.HTTPStatusCode.HasValue)
				{
					continue;
				}

				has500 = has500 || code.HTTPStatusCode.Value == 500;
				has501 = has501 || code.HTTPStatusCode.Value == 501;
				has401 = has401 || code.HTTPStatusCode.Value == 401;
				has403 = has403 || code.HTTPStatusCode.Value == 403;
				has400 = has400 || code.HTTPStatusCode.Value == 400;
			}

			if (has500)
			{
				return 500;
			}
			else if (has501)
			{
				return 501;
			}
			else if (has401)
			{
				return 401;
			}
			else if (has403)
			{
				return 403;
			}
			else if (has400)
			{
				return 400;
			}
		}

		if (this.Success)
		{
			return 200;
		}

		return 400;
	}

	/// <inheritdoc/>
	public string Serialize()
	{
		return this.Serialize(new JsonSerializerSettings());
	}

	/// <inheritdoc/>
	public string Serialize(JsonSerializerSettings serializerSettings)
	{
		return JsonConvert.SerializeObject(value: this, type: this.GetType(), settings: serializerSettings);
	}

	/// <inheritdoc/>
	public new string ToString()
	{
		return this.Serialize(new JsonSerializerSettings());
	}

	/// <inheritdoc/>
	public IResponseMeta? Meta { get; set; }

	/// <inheritdoc/>
	public IResponse WithMeta(IResponseMeta? meta)
	{
		this.Meta = meta;
		return this;
	}
}

public class Response<T> : Response, IResponse<T>
{
	/// <inheritdoc/>
	public T? Data { get; set; }

	private Response(IResponse? result)
	{
		this.Success = result?.Success ?? false;
		this.OverridenStatusCode = result?.OverridenStatusCode;
		this.Errors = result?.Errors ?? [];
	}

	/// <summary>
	/// Create a new Result with Response Data from an existing Result
	/// </summary>
	/// <param name="result"></param>
	/// <returns></returns>
	public static IResponse<T> FromResponse(IResponse? result)
	{
		return new Response<T>(result);
	}

	/// <inheritdoc/>
	public IResponse<T> WithData(T? data)
	{
		this.Data = data;
		return this;
	}

	/// <inheritdoc/>
	public new IResponse<T> WithMeta(IResponseMeta? meta)
	{
		this.Meta = meta;
		return this;
	}

	/// <inheritdoc/>
	public new IResponse<T> WithError(IErrorCode? code)
	{
		if (code is not null)
		{
			this.Errors.Add(code);
		}
		return this;
	}

	/// <inheritdoc/>
	public new IResponse<T> WithStatusCode(int statusCode)
	{
		this.OverridenStatusCode = statusCode;
		return this;
	}
}
