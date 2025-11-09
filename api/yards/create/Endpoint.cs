using AutoInsight.Auth;
using AutoInsight.Data;
using AutoInsight.Models;
using FluentValidation;

namespace AutoInsight.Yards.Create
{
    public static class Endpoint
    {
        public static RouteGroupBuilder MapYardCreateEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/", HandleAsync)
                .WithName("CreateYard")
                .WithSummary("Create a new yard and owner admin")
                .WithDescription(
                    "Creates a new yard and automatically registers the authenticated user as the first Admin employee for that yard." +
                    "\n\n**Request Body:**\n" +
                    "```json\n" +
                    "{\n" +
                    "  \"name\": \"Main Yard\",\n" +
                    "  \"ownerName\": \"Maria Souza\",\n" +
                    "  \"capacity\": 120\n" +
                    "}\n" +
                    "```" +
                    "\n\n**Responses:**\n" +
                    "- `201 Created`: Yard created successfully (returns yard details).\n" +
                    "- `400 Bad Request`: Validation errors in the payload.\n" +
                    "- `401 Unauthorized`: Missing or invalid bearer token." +
                    "\n\n**Example Response (201):**\n" +
                    "```json\n" +
                    "{\n" +
                    "  \"id\": \"7f5c1b8a-49df-4c4b-8b5f-bb56b0d1c8aa\",\n" +
                    "  \"name\": \"Main Yard\",\n" +
                    "  \"ownerId\": \"firebase-user-123\",\n" +
                    "  \"capacity\": 120\n" +
                    "}\n" +
                    "```"
                )
                .Produces<Response>(StatusCodes.Status201Created)
                .ProducesValidationProblem()
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status401Unauthorized);

            return group;
        }

        private class Validator : AbstractValidator<Request>
        {
            public Validator()
            {
                RuleFor(x => x.Name).NotEmpty();
                RuleFor(x => x.OwnerName).NotEmpty();
                RuleFor(x => x.Capacity).GreaterThan(0);
            }
        }

        private static async Task<IResult> HandleAsync(Request request, AppDbContext db, HttpContext httpContext)
        {
            if (!httpContext.TryGetAuthenticatedUser(out var user) || user is null)
            {
                return Results.Unauthorized();
            }

            var validation = await new Validator().ValidateAsync(request);
            if (!validation.IsValid)
            {
                return Results.ValidationProblem(validation.ToDictionary());
            }

            var yard = new Yard
            {
                Name = request.Name,
                OwnerId = user.UserId,
                Capacity = request.Capacity
            };

            db.Yards.Add(yard);

            var employee = new YardEmployee
            {
                Name = request.OwnerName,
                Role = EmployeeRole.Admin,
                UserId = user.UserId,
                Yard = yard,
                YardId = yard.Id,
            };

            db.YardEmployees.Add(employee);

            await db.SaveChangesAsync();

            var response = new Response(yard.Id, yard.Name, yard.OwnerId, yard.Capacity);
            return Results.Created($"/v2/yards/{yard.Id}", response);
        }
    }
}
