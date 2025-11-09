using AutoInsight.Auth;
using AutoInsight.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace AutoInsight.EmployeeInvites.ListUser
{
    public static class Endpoint
    {
        public static RouteGroupBuilder MapEmployeeInviteListUserEndpoint(this RouteGroupBuilder group)
        {
            group.MapGet("/user", HandleAsync)
                .WithName("ListEmployeeInvitesByUser")
                .WithSummary("List Employee Invites for the authenticated user")
                .WithDescription(
                    "Retrieves a paginated list of Employee Invites associated with the authenticated user's email. " +
                    "Supports cursor-based pagination using the `cursor` and `limit` query parameters. " +
                    "If parameters are invalid, returns 400 Bad Request.\n\n" +
                    "### Example Request\n" +
                    "```\n" +
                    "GET /v2/invites/user?limit=2\n" +
                    "```\n\n" +
                    "### Example Response\n" +
                    "```json\n" +
                    "{\n" +
                    "  \"data\": [\n" +
                    "    {\n" +
                    "      \"id\": \"c6b61b2e-2af9-4b5a-b58a-3e09d0a933b1\",\n" +
                    "      \"email\": \"john.doe@example.com\",\n" +
                    "      \"role\": \"Admin\",\n" +
                    "      \"status\": \"Accepted\",\n" +
                    "      \"createdAt\": \"2025-11-03T12:45:30Z\",\n" +
                    "      \"acceptedAt\": \"2025-11-03T13:00:00Z\",\n" +
                    "      \"inviterId\": \"firebase-user-123\",\n" +
                    "      \"yard\": {\n" +
                    "        \"id\": \"9a4a2b66-2b29-4de7-82b2-8f3a3af88f66\",\n" +
                    "        \"name\": \"Central Yard\"\n" +
                    "      }\n" +
                    "    }\n" +
                    "  ],\n" +
                    "  \"pageInfo\": {\n" +
                    "    \"nextCursor\": \"c6b61b2e-2af9-4b5a-b58a-3e09d0a933b1\",\n" +
                    "    \"hasNext\": true\n" +
                    "  },\n" +
                    "  \"count\": 1\n" +
                    "}\n" +
                    "```"
                )
                .Produces<Response>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status401Unauthorized);

            return group;
        }

        private static async Task<IResult> HandleAsync(AppDbContext db, HttpContext httpContext, string? cursor = null, int limit = 10)
        {
            if (!httpContext.TryGetAuthenticatedUser(out var user) || user is null)
            {
                return Results.Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                return Results.BadRequest(new { error = "Authenticated user must have a valid email." });
            }

            if (limit <= 0 || limit > 100)
                return Results.BadRequest(new { error = "Limit must be between 1 and 100." });

            Guid? cursorGuid = null;
            if (!string.IsNullOrEmpty(cursor))
            {
                if (!Guid.TryParse(cursor, out var parsed))
                    return Results.BadRequest(new { error = "Cursor must be a valid UUID." });

                bool exists = await db.YardEmployees.AnyAsync(y => y.Id == parsed);
                if (!exists)
                    return Results.BadRequest(new { error = "Cursor not found." });

                cursorGuid = parsed;
            }

            var query = db.EmployeeInvites.Include(i => i.Yard).AsQueryable().Where(i => i.Email == user.Email);

            if (cursorGuid.HasValue)
            {
                query = query.Where(y => y.Id.CompareTo(cursorGuid.Value) > 0);
            }

            query = query.OrderBy(y => y.Id).Take(limit + 1);

            var invites = await query.ToListAsync();

            bool hasNext = invites.Count > limit;
            Guid? nextCursor = null;

            if (hasNext)
            {
                nextCursor = invites.Last().Id;
                invites = invites.Take(limit).ToList();
            }

            var responseItems = invites
                .Select(i => new ResponseItem(i.Id, i.Email, i.Role.ToString(), i.Status.ToString(), i.CreatedAt, i.AcceptedAt, i.InviterId, new YardResponse(i.Yard.Id, i.Yard.Name)))
                .ToList();

            var response = new Response(
                responseItems,
                new Common.Response.PageInfo(nextCursor, hasNext),
                responseItems.Count
            );

            return Results.Ok(response);
        }
    }
}
