using AutoInsight.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoInsight.YardEmployees.Get
{
    public static class Endpoint
    {
        public static RouteGroupBuilder MapYardEmployeeGetEndpoint(this RouteGroupBuilder group)
        {
            group.MapGet("/{employeeId}", HandleAsync)
                .WithSummary("Retrieve details for a yard employee")
                .WithDescription(
                    "Fetches the information of a specific employee assigned to the given yard.\n\n" +
                    "**Path Parameters:**\n" +
                    "- `yardId` (UUID, required): Identifier of the yard.\n" +
                    "- `employeeId` (UUID, required): Identifier of the employee to retrieve.\n\n" +
                    "**Example Request:**\n" +
                    "```bash\n" +
                    "GET /v2/yards/6b1b36c2-8f63-4c2b-b3df-9c5d9cfefb83/employees/7fbd32a2-1b78-4a2e-bf53-83f1c1fdd92b\n" +
                    "```\n\n" +
                    "**Possible Responses:**\n" +
                    "- `200 OK`: Employee found and returned.\n" +
                    "- `400 Bad Request`: Invalid yardId or employeeId.\n" +
                    "- `404 Not Found`: Yard or employee not found.\n\n" +
                    "**Example Response (200):**\n" +
                    "```json\n" +
                    "{\n" +
                    "  \"id\": \"7fbd32a2-1b78-4a2e-bf53-83f1c1fdd92b\",\n" +
                    "  \"name\": \"João Lima\",\n" +
                    "  \"imageUrl\": null,\n" +
                    "  \"role\": \"Member\",\n" +
                    "  \"userId\": \"firebase-user-123\"\n" +
                    "}\n" +
                    "```"
                )
                .Produces<Response>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status404NotFound);

            return group;
        }

        private static async Task<IResult> HandleAsync(AppDbContext db, string yardId, string employeeId)
        {
            if (!Guid.TryParse(yardId, out var parsedYardId))
            {
                return Results.BadRequest(new { error = "YardId must be a valid UUID." });
            }

            var yard = await db.Yards.FirstOrDefaultAsync(y => y.Id == parsedYardId);
            if (yard is null)
                return Results.NotFound(new { error = "Yard not found" });

            Guid parsedEmployeeId;
            if (string.IsNullOrEmpty(employeeId) || !Guid.TryParse(employeeId, out parsedEmployeeId))
                return Results.BadRequest(new { error = "'Employee Id' must be a valid UUID." });

            var employee = await db.YardEmployees
                .FirstOrDefaultAsync(y => y.Id == parsedEmployeeId);

            if (employee is null)
                return Results.NotFound(new { error = "Employee not found" });

            var response = new Response(employee.Id, employee.Name, employee.ImageUrl, employee.Role.ToString(), employee.UserId);
            return Results.Ok(response);
        }
    }
}
