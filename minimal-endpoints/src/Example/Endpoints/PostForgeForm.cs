using IronWatch.MediatR.MinimalEndpoints;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Example.Endpoints;

[ApiPost("/forge")]
public class PostForgeForm : IRequestHandler<PostForgeFormRequest, IResult>
{
    public Task<IResult> Handle(PostForgeFormRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Results.Ok(new PostForgeFormResponse()
        {
            ForgeSummary = $"{request.Metal} {request.Weight}oz"
        }));
    }
}

[AsForm(enableAntiForgery: true)]
public class PostForgeFormRequest : IRequest<IResult>
{
    public required string Metal { get; set; }
    public required int Weight { get; set; }
}

public class PostForgeFormResponse
{
    public required string ForgeSummary { get; set; }
}
