using System;
using System.Diagnostics.CodeAnalysis;

namespace IronWatch.MediatR.MinimalEndpoints;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ApiEndpointAttribute : Attribute
{
    public string Method { get; set; }
	public string Route { get; set; }
	//public string? NameOverride { get; set; }
    //public string? RouteOverride { get; set; }
    //public BindType BindType { get; set; }

    public ApiEndpointAttribute(string method, [StringSyntax("Route")] string route)//string? routeOverride, BindType bindType)
    {
        this.Method = method;
		this.Route = route;
		//this.RouteOverride = routeOverride;
		//this.BindType = bindType;
    }
}

public class ApiGet : ApiEndpointAttribute
{

    public ApiGet([StringSyntax("Route")] string route) 
        : base("GET", route)//, BindType.QUERY)
	{}
}

public class ApiHead : ApiEndpointAttribute
{

	public ApiHead([StringSyntax("Route")] string route)
		: base("HEAD", route)//, BindType.QUERY)
	{}
}

public class ApiPost : ApiEndpointAttribute
{

	public ApiPost([StringSyntax("Route")] string route)
		: base("POST", route)//, BindType.BODY)
	{}
}

public class ApiPut : ApiEndpointAttribute
{

	public ApiPut([StringSyntax("Route")] string route)
		: base("PUT", route)//, BindType.BODY)
	{}
}

public class ApiDelete : ApiEndpointAttribute
{

	public ApiDelete([StringSyntax("Route")] string route)
		: base("DELETE", route)//, BindType.BODY)
	{}
}

public class ApiConnect : ApiEndpointAttribute
{

	public ApiConnect([StringSyntax("Route")] string route)
		: base("CONNECT", route)//, BindType.QUERY)
	{}
}

public class ApiOptions : ApiEndpointAttribute
{

	public ApiOptions([StringSyntax("Route")] string route)
		: base("OPTIONS", route)//, BindType.QUERY)
	{}
}

public class ApiTrace : ApiEndpointAttribute
{

	public ApiTrace([StringSyntax("Route")] string route)
		: base("TRACE", route)//, BindType.QUERY)
	{}
}

public class ApiPatch : ApiEndpointAttribute
{

	public ApiPatch([StringSyntax("Route")] string route)
		: base("PATCH", route)//, BindType.BODY)
	{}
}