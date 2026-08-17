using FitProgress.Application.IService.Users;
using FitProgress.Application.Results;
using FitProgress.Domain.Contracts.V1.Users.Requests;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FitProgress.Api.Controllers.V1;

[ApiController]
[Route("api/v1/users")]
public sealed class UsersController(IUserService userService) : ControllerBase
{
    private readonly IUserService _userService = userService;

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _userService.CreateUserAsync(request, cancellationToken);

        return result switch
        {
            CreateUserResult.Success success =>
                StatusCode(StatusCodes.Status201Created, success.User),

            CreateUserResult.ValidationFailed validationFailed =>
                BuildValidationProblem(validationFailed),

            CreateUserResult.EmailAlreadyInUse =>
                Conflict(new ProblemDetails
                {
                    Title = "E-mail já está em uso.",
                    Status = StatusCodes.Status409Conflict,
                }),

            _ => throw new InvalidOperationException(
                $"Resultado não mapeado: {result.GetType().Name}"),
        };
    }

    private IActionResult BuildValidationProblem(CreateUserResult.ValidationFailed validationFailed)
    {
        var modelState = new ModelStateDictionary();

        foreach (var error in validationFailed.Errors)
        {
            modelState.AddModelError(error.Field, error.Message);
        }

        return ValidationProblem(modelState);
    }
}
