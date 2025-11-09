using AutoInsight.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoInsight.Yards.Get
{
    public static class Endpoint
    {
        public static RouteGroupBuilder MapYardGetEndpoint(this RouteGroupBuilder group)
        {
            group.MapGet("/{yardId}", HandleAsync)
                .WithSummary("Retrieve details of a Yard by ID")
                .WithDescription(
                    "Fetches detailed information about a Yard by its unique identifier, including the employees and pending invites associated with it." +
                    "\n\n**Path Parameter:**\n" +
                    "- `yardId` (UUID): The unique identifier of the Yard.\n\n" +
                    "**Example Request:**\n" +
                    "```bash\n" +
                    "GET /v2/yards/6b1b36c2-8f63-4c2b-b3df-9c5d9cfefb83\n" +
                    "```\n\n" +
                    "**Possible Responses:**\n" +
                    "- `200 OK`: Yard found and returned.\n" +
                    "- `400 Bad Request`: Invalid Yard ID format.\n" +
                    "- `404 Not Found`: Yard not found.\n\n" +
                    "**Example Response (200):**\n" +
                    "```json\n" +
                    "{\n" +
                    "  \"id\": \"6b1b36c2-8f63-4c2b-b3df-9c5d9cfefb83\",\n" +
                    "  \"name\": \"Main Storage Yard\",\n" +
                    "  \"ownerId\": \"firebase-owner-123\",\n" +
                    "  \"capacity\": 120,\n" +
                    "  \"employees\": [\n" +
                    "    {\n" +
                    "      \"id\": \"3ae5f7c1-d4e6-44c1-8c36-7f34ab09e321\",\n" +
                    "      \"name\": \"Maria Souza\",\n" +
                    "      \"imageUrl\": null,\n" +
                    "      \"role\": \"Admin\",\n" +
                    "      \"userId\": \"firebase-user-123\"\n" +
                    "    }\n" +
                    "  ],\n" +
                    "  \"invites\": [\n" +
                    "    {\n" +
                    "      \"id\": \"f27f2f3a-5d9b-4e1a-9f23-bc17f1e7e200\",\n" +
                    "      \"email\": \"john.doe@example.com\",\n" +
                    "      \"role\": \"Member\",\n" +
                    "      \"status\": \"Pending\",\n" +
                    "      \"createdAt\": \"2025-11-03T12:45:30Z\",\n" +
                    "      \"acceptedAt\": null,\n" +
                    "      \"inviterId\": \"firebase-owner-123\"\n" +
                    "    }\n" +
                    "  ]\n" +
                    "}\n" +
                    "```"
                )
                                            .Produces<Response>(StatusCodes.Status200OK)
                                            .Produces(StatusCodes.Status400BadRequest)
                                            .Produces(StatusCodes.Status404NotFound);

            return group;
        }

        private static async Task<IResult> HandleAsync(AppDbContext db, string yardId)
        {
            Guid parsed;
            if (string.IsNullOrEmpty(yardId) || !Guid.TryParse(yardId, out parsed))
                return Results.BadRequest(new { error = "'Yard Id' must be a valid UUID." });

            var yard = await db.Yards
                .Include(x => x.Employees)
                .Include(x => x.Invites)
                .FirstOrDefaultAsync(y => y.Id == parsed);

            if (yard is null)
                return Results.NotFound(new { error = "Yard not found" });

            var response = new Response(
                        yard.Id,
            yard.Name,
            yard.OwnerId,
            yard.Capacity,
                        yard.Employees.Select(e =>
                                new EmployeeResponse(
                                    e.Id,
                                    e.Name,
                                    e.ImageUrl,
                                    e.Role.ToString(),
                                    e.UserId)
                                ).ToList(),
                        yard.Invites.Select(i =>
                                new InviteResponse(
                                    i.Id,
                                    i.Email,
                                    i.Role.ToString(),
                                    i.Status.ToString(),
                                    i.CreatedAt,
                                    i.AcceptedAt,
                                    i.InviterId)
                                ).ToList());
            return Results.Ok(response);
        }
    }
}
