using AutoInsight.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoInsight.YardEmployees.List
{
    public static class Endpoint
    {
        public static RouteGroupBuilder MapYardEmployeeListEndpoint(this RouteGroupBuilder group)
        {
            group.MapGet("/", HandleAsync)
                .WithSummary("List employees assigned to a yard")
                .WithDescription(
                    "Retrieves a paginated list of employees for the specified yard, ordered by their ID.\n\n" +
                    "**Path Parameters:**\n" +
                    "- `yardId` (UUID, required): Identifier of the yard whose employees will be listed.\n\n" +
                    "**Query Parameters:**\n" +
                    "- `cursor` (UUID, optional): Used for cursor-based pagination to fetch the next page.\n" +
                    "- `limit` (integer, optional, default=10, max=100): Maximum number of employees to return.\n\n" +
                    "**Example Request:**\n" +
                    "```bash\n" +
                    "GET /v2/yards/6b1b36c2-8f63-4c2b-b3df-9c5d9cfefb83/employees?limit=5\n" +
                    "```\n\n" +
                    "**Pagination Example:**\n" +
                    "```bash\n" +
                    "GET /v2/yards/6b1b36c2-8f63-4c2b-b3df-9c5d9cfefb83/employees?cursor=7fbd32a2-1b78-4a2e-bf53-83f1c1fdd92b&limit=5\n" +
                    "```\n\n" +
                    "**Possible Responses:**\n" +
                    "- `200 OK`: Returns a paginated list of employees.\n" +
                    "- `400 Bad Request`: Invalid yardId, cursor or limit.\n" +
                    "- `404 Not Found`: Yard not found.\n\n" +
                    "**Example Response (200):**\n" +
                    "```json\n" +
                    "{\n" +
                    "  \"data\": [\n" +
                    "    { \"id\": \"6b1b36c2-8f63-4c2b-b3df-9c5d9cfefb83\", \"name\": \"Maria Souza\", \"imageUrl\": \"https://cdn.example.com/avatar-maria.png\", \"role\": \"Admin\", \"userId\": \"firebase-user-123\" },\n" +
                    "    { \"id\": \"7fbd32a2-1b78-4a2e-bf53-83f1c1fdd92b\", \"name\": \"João Lima\", \"imageUrl\": null, \"role\": \"Member\", \"userId\": \"firebase-user-456\" }\n" +
                    "  ],\n" +
                    "  \"pageInfo\": {\n" +
                    "    \"nextCursor\": \"9f1f3a93-bf6d-4028-91cb-238aaf3b2368\",\n" +
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

            var query = db.YardEmployees.AsQueryable().Where(e => e.YardId == parsedYardId);

            if (cursorGuid.HasValue)
            {
                query = query.Where(y => y.Id.CompareTo(cursorGuid.Value) > 0);
            }

            query = query.OrderBy(y => y.Id).Take(limit + 1);

            var employees = await query.ToListAsync();

            bool hasNext = employees.Count > limit;
            Guid? nextCursor = null;

            if (hasNext)
            {
                nextCursor = employees.Last().Id;
                employees = employees.Take(limit).ToList();
            }

            var responseItems = employees
                .Select(e => new ResponseItem(e.Id, e.Name, e.ImageUrl, e.Role.ToString(), e.UserId))
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
