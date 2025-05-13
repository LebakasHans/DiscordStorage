using DiscoWeb.Dtos;
using DiscoWeb.Errors;
using FluentResults;
using FluentResults.Extensions.AspNetCore;
using Microsoft.AspNetCore.Mvc;

namespace DiscoWeb.ResultConversion;

public class CustomFluentResultsProfile : DefaultAspNetCoreResultEndpointProfile
{
    public override ActionResult TransformFailedResultToActionResult(FailedResultToActionResultTransformationContext context)
    {
        var result = context.Result;

        if (result.HasError<ValidationError>(out var validationErrors))
        {
            var error = validationErrors.First();
            return new BadRequestObjectResult(new SimpleResponse
            {
                Message = error.Message,
                Status = error.ErrorCode
            });
        }

        if (result.HasError<NotFoundError>(out var notFoundErrors))
        {
            var error = notFoundErrors.First();
            return new NotFoundObjectResult(new SimpleResponse
            {
                Message = error.Message,
                Status = error.ErrorCode
            });
        }

        if (result.HasError<InternalServerError>(out var serverErrors))
        {
            var error = serverErrors.First();
            return new ObjectResult(new SimpleResponse
            {
                Message = error.Message,
                Status = error.ErrorCode
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }

        var genericError = result.Errors.FirstOrDefault();
        var statusCode = genericError?.Metadata.TryGetValue("HttpStatusCode", out var status) == true && status is int httpStatus ? httpStatus : StatusCodes.Status500InternalServerError;

        return new StatusCodeResult(statusCode);
    }

    public override ActionResult TransformOkValueResultToActionResult<T>(OkResultToActionResultTransformationContext<Result<T>> context)
    {
        if (context.Result.Value is string)
        {
            return new OkObjectResult(new SimpleResponse
            {
                Message = context.Result.Value.ToString() ?? string.Empty,
                Status = "200"
            });
        }

        return base.TransformOkValueResultToActionResult(context);
    }
}
