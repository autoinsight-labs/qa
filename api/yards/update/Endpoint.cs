using AutoInsight.Auth;
using AutoInsight.Data;
using AutoInsight.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AutoInsight.Yards.Update
{
    public static class Endpoint
    {
        public static RouteGroupBuilder MapYardUpdateEndpoint(this RouteGroupBuilder group)
        {
            group.MapPatch("/{yardId}", HandleAsync)
                .WithSummary("Update a Yard partially")
                .WithDescription(
                    "Updates one or more fields of an existing Yard identified by its ID. Only employees with the Admin role for the yard may update it, and the OwnerId can only be changed by the current owner.\n\n" +
                    "This endpoint supports partial updates — only the provided fields will be modified.\n\n" +
                    "**Path Parameter:**\n" +
                    "- `yardId` (UUID): The unique identifier of the Yard to update.\n\n" +
                    "**Request Body Example:**\n" +
                    "```json\n" +
                    "{\n" +
                    "  \"name\": \"Updated Yard Name\",\n" +
                    "  \"ownerId\": \"firebase-user-123\",\n" +
                    "  \"capacity\": 150\n" +
                    "}\n" +
                    "```\n\n" +
                    "**Possible Responses:**\n" +
                    "- `200 OK`: Returns the updated Yard.\n" +
                    "- `400 Bad Request`: Invalid Yard ID or payload.\n" +
                    "- `401 Unauthorized`: Missing or invalid bearer token.\n" +
                    "- `403 Forbidden`: User is not an Admin for the yard or not the owner when attempting to transfer ownership.\n" +
                    "- `404 Not Found`: Yard does not exist.\n" +
                    "**Example Successful Response (200):**\n" +
                    "```json\n" +
                    "{\n" +
                    "  \"id\": \"9a3b2b1d-7e54-4b5a-93f3-5a4bfa351b1d\",\n" +
                    "  \"name\": \"Updated Yard Name\",\n" +
                    "  \"ownerId\": \"firebase-user-123\",\n" +
                    "  \"capacity\": 150\n" +
                    "}\n" +
                    "```"
                )
                .Produces<Response>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status403Forbidden)
                .Produces(StatusCodes.Status404NotFound)
                .ProducesValidationProblem();
            return group;
        }

        // TODO: Validate JWT to check if the user changing the ownerId is the owner
        private class Validator : AbstractValidator<Request>
        {
            public Validator()
            {
                When(x => x.Name is not null, () =>
                {
                    RuleFor(x => x.Name)
                                        .NotEmpty()
                                        .WithMessage("Name cannot be empty");
                });
                When(x => x.OwnerId is not null, () =>
                {
                    RuleFor(x => x.OwnerId)
                        .NotEmpty()
                        .MaximumLength(128);
                });
                When(x => x.Capacity is not null, () =>
                {
                    RuleFor(x => x.Capacity!.Value)
                        .GreaterThan(0)
                        .WithMessage("Capacity must be greater than zero.");
                });
            }
        }

        private static async Task<IResult> HandleAsync(Request request, AppDbContext db, string yardId, HttpContext httpContext)
        {
            if (!httpContext.TryGetAuthenticatedUser(out var user) || user is null)
                return Results.Unauthorized();

            if (!Guid.TryParse(yardId, out var parsedYardId))
                return Results.BadRequest(new { error = "'Yard Id' must be a valid UUID." });

            var validation = await new Validator().ValidateAsync(request);
            if (!validation.IsValid)
            {
                return Results.ValidationProblem(validation.ToDictionary());
            }

            var yard = await db.Yards.FirstOrDefaultAsync(y => y.Id == parsedYardId);
            if (yard is null)
            {
                return Results.NotFound(new { error = "Yard not found" });
            }

            var employee = await db.YardEmployees.FirstOrDefaultAsync(e => e.YardId == parsedYardId && e.UserId == user.UserId);
            if (employee is null || employee.Role != EmployeeRole.Admin)
            {
                return Results.Json(new { error = "Only yard admins can update this yard." }, statusCode: StatusCodes.Status403Forbidden);
            }

            if (request.OwnerId is not null)
            {
                if (yard.OwnerId != user.UserId)
                {
                    return Results.Json(new { error = "Only the current owner can transfer ownership." }, statusCode: StatusCodes.Status403Forbidden);
                }

                yard.OwnerId = request.OwnerId;
            }

            if (request.Name is not null)
            {
                yard.Name = request.Name;
            }

            if (request.Capacity is not null)
            {
                yard.Capacity = request.Capacity.Value;
            }

            await db.SaveChangesAsync();

            var response = new Response(yard.Id, yard.Name, yard.OwnerId, yard.Capacity);
            return Results.Ok(response);
        }
    }
}
