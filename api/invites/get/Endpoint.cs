using AutoInsight.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoInsight.EmployeeInvites.Get
{
    public static class Endpoint
    {
        public static RouteGroupBuilder MapEmployeeInviteGetEndpoint(this RouteGroupBuilder group)
        {
            group.MapGet("/{inviteId}", HandleAsync)
                .WithSummary("Retrieve an Employee Invite by ID")
                .WithDescription(
                    "Retrieves a specific Employee Invite by its unique identifier. " +
                    "If the invite does not exist, returns 404 Not Found. If the provided ID is invalid, returns 400 Bad Request.\n\n" +
                    "### Example Request\n" +
                    "```\n" +
                    "GET /v2/invites/f27f2f3a-5d9b-4e1a-9f23-bc17f1e7e200\n" +
                    "```\n\n" +
                    "### Example Response\n" +
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
                .Produces<Response>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status404NotFound);

            return group;
        }

        private static async Task<IResult> HandleAsync(AppDbContext db, string inviteId)
        {
            Guid parsedInviteId;
            if (string.IsNullOrEmpty(inviteId) || !Guid.TryParse(inviteId, out parsedInviteId))
                return Results.BadRequest(new { error = "'Invite Id' must be a valid UUID." });

            var invite = await db.EmployeeInvites
                            .Include(i => i.Yard)
                            .FirstOrDefaultAsync(y => y.Id == parsedInviteId);

            if (invite is null)
                return Results.NotFound(new { error = "Invite not found" });

            var response = new Response(invite.Id,
                        invite.Email,
                        invite.Role.ToString(),
                        invite.Status.ToString(),
                        invite.CreatedAt,
                        invite.AcceptedAt,
                        invite.InviterId,
                        new YardResponse(invite.Yard.Id, invite.Yard.Name)
              );
            return Results.Ok(response);
        }
    }
}
