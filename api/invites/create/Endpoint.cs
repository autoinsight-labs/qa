using AutoInsight.Auth;
using AutoInsight.Data;
using AutoInsight.Models;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace AutoInsight.EmployeeInvites.Create
{
    public static class Endpoint
    {
        public static RouteGroupBuilder MapEmployeeInviteCreateEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/", HandleAsync)
                .WithSummary("Send an employee invite for a yard")
                .WithDescription(
                    "Creates a new invite for the specified yard after validating the yard exists and the authenticated requester is an Admin employee." +
                    "\n\n**Path Parameters:**\n" +
                    "- `yardId` (UUID, required): Yard that will own the invite." +
                    "\n\n**Request Body:**\n" +
                    "```json\n" +
                    "{\n" +
                    "  \"email\": \"john.doe@example.com\",\n" +
                    "  \"role\": \"Member\"\n" +
                    "}\n" +
                    "```" +
                    "\n\n**Responses:**\n" +
                    "- `201 Created`: Invite created successfully (returns invite details and yard info).\n" +
                    "- `400 Bad Request`: Validation errors or invalid UUIDs.\n" +
                    "- `401 Unauthorized`: Missing or invalid authentication.\n" +
                    "- `403 Forbidden`: Requester belongs to the yard but is not an admin.\n" +
                    "- `404 Not Found`: Yard not found." +
                    "\n\n**Example Response (201):**\n" +
                    "```json\n" +
                    "{\n" +
                    "  \"id\": \"f27f2f3a-5d9b-4e1a-9f23-bc17f1e7e200\",\n" +
                    "  \"email\": \"john.doe@example.com\",\n" +
                    "  \"role\": \"Member\",\n" +
                    "  \"status\": \"Pending\",\n" +
                    "  \"createdAt\": \"2025-11-03T12:45:30Z\",\n" +
                    "  \"acceptedAt\": null,\n" +
                    "  \"inviterId\": \"firebase-user-123\",\n" +
                    "  \"yard\": {\n" +
                    "    \"id\": \"9a4a2b66-2b29-4de7-82b2-8f3a3af88f66\",\n" +
                    "    \"name\": \"Central Yard\"\n" +
                    "  }\n" +
                    "}\n" +
                    "```"
                )
                .Produces<Response>(StatusCodes.Status201Created)
                .ProducesValidationProblem()
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status403Forbidden)
                .Produces(StatusCodes.Status404NotFound);

            return group;
        }

        private class Validator : AbstractValidator<Request>
        {
            public Validator()
            {
                RuleFor(x => x.Email).EmailAddress();
                RuleFor(x => x.Role).NotEmpty().Must(BeAValidRole)
                                .WithMessage("Role must be one of: Admin, Member"); ;
            }

            private bool BeAValidRole(string role) =>
                            Enum.TryParse<EmployeeRole>(role, true, out _);
        }

        private static async Task<IResult> HandleAsync(Request request, AppDbContext db, string yardId, HttpContext httpContext)
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

            var validation = await new Validator().ValidateAsync(request);
            if (!validation.IsValid)
            {
                return Results.ValidationProblem(validation.ToDictionary());
            }

            var inviter = await db.YardEmployees.FirstOrDefaultAsync(y => y.YardId == parsedYardId && y.UserId == user.UserId);
            if (inviter is null)
            {
                return Results.Json(new { error = "You must belong to this yard to send invites." }, statusCode: StatusCodes.Status403Forbidden);
            }

            if (inviter.Role != EmployeeRole.Admin)
            {
                return Results.Json(new { error = "Only yard admins can send invites." }, statusCode: StatusCodes.Status403Forbidden);
            }

            var invite = new EmployeeInvite
            {
                Email = request.Email,
                Role = Enum.Parse<EmployeeRole>(request.Role, true),
                InviterId = inviter.UserId,
                YardId = parsedYardId,
                Yard = yard,
            };

            db.EmployeeInvites.Add(invite);

            await db.SaveChangesAsync();

            var response = new Response(invite.Id,
                        invite.Email,
                        invite.Role.ToString(),
                        invite.Status.ToString(),
                        invite.CreatedAt,
                        invite.AcceptedAt,
                        invite.InviterId,
                        new YardResponse(yard.Id, yard.Name)
                    );
            return Results.Created($"/v2/invites/{invite.Id}", response);
        }
    }
}
