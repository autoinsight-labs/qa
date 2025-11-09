using AutoInsight.Auth;
using AutoInsight.Data;
using AutoInsight.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace AutoInsight.EmployeeInvites.Reject
{
    public static class Endpoint
    {
        public static RouteGroupBuilder MapEmployeeInviteRejectEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/{inviteId}/reject", HandleAsync)
                .WithSummary("Reject an Employee Invite")
                .WithDescription(
                    "Marks a pending Employee Invite as rejected by its unique ID. " +
                    "Requires the authenticated user's email to match the invite's email. " +
                    "If the invite does not exist, returns 404 Not Found. " +
                    "If the invite is not in 'Pending' status or the ID is invalid, returns 400 Bad Request.\n\n" +
                    "### Example Request\n" +
                    "```\n" +
                    "POST /v2/invites/f27f2f3a-5d9b-4e1a-9f23-bc17f1e7e200/reject\n" +
                    "```\n\n" +
                    "### Example Response\n" +
                    "```\n" +
                    "HTTP 200 OK\n" +
                    "```\n\n" +
                    "### Example Error Responses\n" +
                    "#### 400 Bad Request\n" +
                    "```json\n" +
                    "{ \"error\": \"Invite not available\" }\n" +
                    "```\n\n" +
                    "#### 403 Forbidden\n" +
                    "```json\n" +
                    "{ \"error\": \"You are not allowed to reject this invite.\" }\n" +
                    "```\n\n" +
                    "#### 404 Not Found\n" +
                    "```json\n" +
                    "{ \"error\": \"Invite not found\" }\n" +
                    "```"
                )
                .Produces(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status403Forbidden)
                .Produces(StatusCodes.Status404NotFound);

            return group;
        }

        private static async Task<IResult> HandleAsync(AppDbContext db, string inviteId, HttpContext httpContext)
        {
            if (!httpContext.TryGetAuthenticatedUser(out var user) || user is null)
            {
                return Results.Unauthorized();
            }

            if (!Guid.TryParse(inviteId, out var parsedInviteId))
            {
                return Results.BadRequest(new { error = "InviteId must be a valid UUID." });
            }

            var invite = await db.EmployeeInvites
                .FirstOrDefaultAsync(y => y.Id == parsedInviteId);
            if (invite is null)
            {
                return Results.NotFound(new { error = "Invite not found" });
            }

            if (invite.Status != InviteStatus.Pending)
            {
                return Results.BadRequest(new { error = "Invite not available" });
            }

            if (string.IsNullOrWhiteSpace(user.Email) || !string.Equals(invite.Email, user.Email, StringComparison.OrdinalIgnoreCase))
            {
                return Results.Json(new { error = "You are not allowed to reject this invite." }, statusCode: StatusCodes.Status403Forbidden);
            }

            invite.Status = InviteStatus.Rejected;

            await db.SaveChangesAsync();

            return Results.Ok();
        }
    }
}
