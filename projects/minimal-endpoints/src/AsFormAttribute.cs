using System;

namespace IronWatch.MediatR.MinimalEndpoints;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class AsFormAttribute : Attribute
{
    public string? FormPropertyName { get; set; }
    public string? ParamPropertyName { get; set; }
    public bool EnableAntiForgery { get; set; }

    public AsFormAttribute(string formPropertyName, string paramPropertyName, bool enableAntiForgery = false)
    {
        FormPropertyName = formPropertyName;
        ParamPropertyName = paramPropertyName;
        EnableAntiForgery = enableAntiForgery;
    }

    public AsFormAttribute(string formPropertyName, bool enableAntiForgery = false)
    {
        FormPropertyName = formPropertyName;
        EnableAntiForgery = enableAntiForgery;
    }

    public AsFormAttribute(bool enableAntiForgery = false)
    {
        EnableAntiForgery = enableAntiForgery;
    }
}
