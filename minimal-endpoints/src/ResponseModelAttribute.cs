using System;

namespace IronWatch.MediatR.MinimalEndpoints;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ResponseModelAttribute : Attribute
{
	public Type RequestType { get; set; }

	public ResponseModelAttribute(Type requestType)
	{
		this.RequestType = requestType;
	}
}
