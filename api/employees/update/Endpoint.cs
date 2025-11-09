using AutoInsight.Auth;
using AutoInsight.Data;
using AutoInsight.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AutoInsight.YardEmployees.Update
{
    public static class Endpoint
    {
        public static RouteGroupBuilder MapYardEmployeeUpdateEndpoint(this RouteGroupBuilder group)
        {
            group.MapPatch("/{employeeId}", HandleAsync)
                .WithSummary("Update an employee's details inside a yard")
                .WithDescription(
                    "Performs a partial update on the information of an employee assigned to the specified yard. Role changes are restricted to admins, while name and image updates may be performed by admins or the employee themselves.\n\n" +
                    "**Path Parameters:**\n" +
                    "- `yardId` (UUID, required): Identifier of the yard.\n" +
                    "- `employeeId` (UUID, required): Identifier of the employee to update.\n\n" +
                    "**Request Body (partial fields optional):**\n" +
                    "```json\n" +
                    "{\n" +
                    "  \"name\": \"Maria Souza\",\n" +
                    "  \"imageUrl\": \"https://cdn.example.com/avatar-maria.png\",\n" +
                    "  \"role\": \"Admin\"\n" +
                    "}\n" +
                    "```\n\n" +
                    "**Possible Responses:**\n" +
                    "- `200 OK`: Employee successfully updated.\n" +
                    "- `400 Bad Request`: Invalid yardId, employeeId or payload.\n" +
                    "- `401 Unauthorized`: Missing or invalid bearer token.\n" +
                    "- `403 Forbidden`: Requester lacks permission to update the employee.\n" +
                    "- `404 Not Found`: Yard or employee not found.\n" +
                    "- `422 Unprocessable Entity`: Validation errors for provided fields.\n\n" +
                    "**Example Response (200):**\n" +
                    "```json\n" +
                    "{\n" +
                    "  \"id\": \"7fbd32a2-1b78-4a2e-bf53-83f1c1fdd92b\",\n" +
                    "  \"name\": \"Maria Souza\",\n" +
                    "  \"imageUrl\": \"https://cdn.example.com/avatar-maria.png\",\n" +
                    "  \"role\": \"Admin\",\n" +
                    "  \"userId\": \"firebase-user-123\"\n" +
                    "}\n" +
                    "```"
                )
                .Produces<Response>(StatusCodes.Status200OK)
                .ProducesValidationProblem()
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status403Forbidden)
                .Produces(StatusCodes.Status404NotFound);
            return group;
        }

        public record Request(string? Name, string? ImageUrl, string? Role);
        private class Validator : AbstractValidator<Request>
        {
            public Validator()
            {
                When(x => x.Name is not null, () =>
                {
                    RuleFor(x => x.Name)
                                    .NotEmpty();
                });

                When(x => x.ImageUrl is not null, () =>
                {
                    RuleFor(x => x.ImageUrl)
                        .NotEmpty()
                        .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
                        .WithMessage("'ImageUrl' must be a valid URL.");
                });

                When(x => x.Role is not null, () =>
                {
                    RuleFor(x => x.Role).NotEmpty().Must(BeAValidRole)
                                                        .WithMessage("Model must be one of: Admin, Member"); ;
                });

            }

            public bool BeAValidRole(string? role) =>
                            Enum.TryParse<EmployeeRole>(role, true, out _);
        }

        private static async Task<IResult> HandleAsync(Request request, AppDbContext db, string yardId, string employeeId, HttpContext httpContext)
        {
            if (!httpContext.TryGetAuthenticatedUser(out var user) || user is null)
            {
                return Results.Unauthorized();
            }

            if (!Guid.TryParse(yardId, out var parsedYardId))
            {
                return Results.BadRequest(new { error = "YardId must be a valid UUID." });
            }

            var yard = await db.Yards.FirstOrDefaultAsync(y => y.Id == parsedYardId);
            if (yard is null)
                return Results.NotFound(new { error = "Yard not found" });

            if (!Guid.TryParse(employeeId, out var parsedEmployeeId))
                return Results.BadRequest(new { error = "'Employee Id' must be a valid UUID." });

            var validation = await new Validator().ValidateAsync(request);
            if (!validation.IsValid)
            {
                return Results.ValidationProblem(validation.ToDictionary());
            }

            var employee = await db.YardEmployees.FirstOrDefaultAsync(v => v.Id == parsedEmployeeId && v.YardId == parsedYardId);
            if (employee is null)
            {
                return Results.NotFound(new { error = "Employee not found" });
            }

            var requester = await db.YardEmployees.FirstOrDefaultAsync(e => e.YardId == parsedYardId && e.UserId == user.UserId);
            if (requester is null)
            {
                return Results.Json(new { error = "You must belong to this yard to update employees." }, statusCode: StatusCodes.Status403Forbidden);
            }

            var isAdmin = requester.Role == EmployeeRole.Admin;
            var isSelf = requester.UserId == employee.UserId;

            if (request.Role is not null && !isAdmin)
            {
                return Results.Json(new { error = "Only yard admins can change employee roles." }, statusCode: StatusCodes.Status403Forbidden);
            }

            if ((request.Name is not null || request.ImageUrl is not null) && !(isAdmin || isSelf))
            {
                return Results.Json(new { error = "Only admins or the employee can update name or image." }, statusCode: StatusCodes.Status403Forbidden);
            }

            if (request.Name is not null) employee.Name = request.Name;
            if (request.ImageUrl is not null) employee.ImageUrl = request.ImageUrl;
            if (request.Role is not null) employee.Role = Enum.Parse<EmployeeRole>(request.Role);

            await db.SaveChangesAsync();

            var response = new Response(employee.Id, employee.Name, employee.ImageUrl, employee.Role.ToString(), employee.UserId);
            return Results.Ok(response);
        }
    }
}
