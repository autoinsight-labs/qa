using AutoInsight.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoInsight.EmployeeInvites.List
{
    public static class Endpoint
    {
        public static RouteGroupBuilder MapEmployeeInviteListEndpoint(this RouteGroupBuilder group)
        {
            group.MapGet("/", HandleAsync)
                .WithSummary("List Employee Invites for a Yard")
                .WithDescription(
                    "Retrieves a paginated list of Employee Invites associated with a specific Yard. " +
                    "Supports cursor-based pagination using the `cursor` and `limit` query parameters. " +
                    "If the Yard does not exist or parameters are invalid, returns appropriate error responses.\n\n" +
                    "### Query Parameters\n" +
                    "- `yardId` (required): The UUID of the Yard to fetch invites from.\n" +
                    "- `cursor` (optional): The UUID cursor indicating the starting point for the next page.\n" +
                    "- `limit` (optional): The number of invites to return (default 10, max 100).\n\n" +
                    "### Example Request\n" +
                    "```\n" +
                    "GET /v2/invites?yardId=9a4a2b66-2b29-4de7-82b2-8f3a3af88f66&limit=2\n" +
                    "```\n\n" +
                    "### Example Response\n" +
                    "```json\n" +
                    "{\n" +
                    "  \"data\": [\n" +
                    "    {\n" +
                    "      \"id\": \"c6b61b2e-2af9-4b5a-b58a-3e09d0a933b1\",\n" +
                    "      \"email\": \"alice@example.com\",\n" +
                    "      \"role\": \"Admin\",\n" +
                    "      \"status\": \"Accepted\",\n" +
                    "      \"createdAt\": \"2025-11-03T12:45:30Z\",\n" +
                    "      \"acceptedAt\": \"2025-11-03T13:00:00Z\",\n" +
                    "      \"inviterId\": \"firebase-user-123\"\n" +
                    "    },\n" +
                    "    {\n" +
                    "      \"id\": \"d2fa5c39-4c19-4b0a-ae44-dcd4b74a4b2a\",\n" +
                    "      \"email\": \"bob@example.com\",\n" +
                    "      \"role\": \"Member\",\n" +
                    "      \"status\": \"Pending\",\n" +
                    "      \"createdAt\": \"2025-11-03T13:10:00Z\",\n" +
                    "      \"acceptedAt\": null,\n" +
                    "      \"inviterId\": \"firebase-user-123\"\n" +
                    "    }\n" +
                    "  ],\n" +
                    "  \"pageInfo\": {\n" +
                    "    \"nextCursor\": \"d2fa5c39-4c19-4b0a-ae44-dcd4b74a4b2a\",\n" +
                    "    \"hasNext\": true\n" +
                    "  },\n" +
                    "  \"count\": 2\n" +
                    "}\n" +
                    "```"
                )
                .Produces<Response>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status404NotFound);

            return group;
        }

        private static async Task<IResult> HandleAsync(AppDbContext db, string yardId, string? cursor = null, int limit = 10)
        {
            if (!Guid.TryParse(yardId, out var parsedYardId))
            {
                return Results.BadRequest(new { error = "YardId must be a valid UUID." });
            }

            var yard = await db.Yards.FirstOrDefaultAsync(y => y.Id == parsedYardId);
            if (yard is null)
                return Results.NotFound(new { error = "Yard not found" });

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

            var query = db.EmployeeInvites.AsQueryable().Where(i => i.YardId == parsedYardId);

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
                .Select(e => new ResponseItem(e.Id, e.Email, e.Role.ToString(), e.Status.ToString(), e.CreatedAt, e.AcceptedAt, e.InviterId))
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
