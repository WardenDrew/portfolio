namespace Platform.Legacy.Core.Mediator.Pipelines;

public class RequestValidationPipeline
	<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators) : IPipelineBehavior<TRequest, TResponse>
	where TRequest : IRequest<TResponse>
	where TResponse : class, IResponse
{
	public async Task<TResponse> Handle(
		TRequest request,
		RequestHandlerDelegate<TResponse> next,
		CancellationToken cancellationToken
	)
	{
		if (validators.Any())
		{
			ValidationContext<TRequest> context = new(request);

			List<FluentValidation.Results.ValidationFailure> failures = [.. validators
				.Select(x => x.Validate(context))
				.SelectMany(x => x.Errors)
				.Where(x => x is not null),];

			if (failures.Count != 0)
			{
				if (
					Response.FromError(RequestValidationPipelineErrors.VALIDATION_FAILED).WithData(failures)
					is TResponse tResponse
				)
				{
					return tResponse;
				}
				else
				{
					throw new InvalidCastException(
						"Failed to process the IResponse as a TResponse in the RequestValidationPipeline!"
					);
				}
			}
		}
		return await next();
	}
}

public class RequestValidationPipelineErrors : IErrorCodeProvider
{
	public static readonly IErrorCode VALIDATION_FAILED = new ErrorCode(
		name: nameof(RequestValidationPipelineErrors.VALIDATION_FAILED),
		section: nameof(RequestValidationPipelineErrors),
		englishTranslation: $"Request Model Validation Failed. {ErrorCode.SUPPORT}",
		httpStatusCode: 400
	);
}
