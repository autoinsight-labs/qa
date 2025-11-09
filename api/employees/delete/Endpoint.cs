using AutoInsight.Auth;
using AutoInsight.Data;
using AutoInsight.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoInsight.YardEmployees.Delete
{
    public static class Endpoint
    {
        public static RouteGroupBuilder MapYardEmployeeDeleteEndpoint(this RouteGroupBuilder group)
        {
            group.MapDelete("/{employeeId}", HandleAsync)
                .WithSummary("Remove an employee from a yard")
                .WithDescription(
                    "Deletes an employee that belongs to the specified yard. Only admins for the yard may perform this action.\n\n" +
                    "**Path Parameters:**\n" +
                    "- `yardId` (UUID, required): Identifier of the yard.\n" +
                    "- `employeeId` (UUID, required): Identifier of the employee to delete.\n\n" +
                    "**Example Request:**\n" +
                    "```bash\n" +
                    "DELETE /v2/yards/6b1b36c2-8f63-4c2b-b3df-9c5d9cfefb83/employees/7fbd32a2-1b78-4a2e-bf53-83f1c1fdd92b\n" +
                    "```\n\n" +
                    "**Possible Responses:**\n" +
                    "- `200 OK`: Employee successfully removed.\n" +
                    "- `400 Bad Request`: Invalid yardId or employeeId.\n" +
                    "- `401 Unauthorized`: Missing or invalid bearer token.\n" +
                    "- `403 Forbidden`: Requester is not an admin for the yard.\n" +
                    "- `404 Not Found`: Yard or employee not found.\n\n" +
                    "**Example Response (200):**\n" +
                    "```json\n" +
                    "{\n" +
                    "  \"id\": \"7fbd32a2-1b78-4a2e-bf53-83f1c1fdd92b\"\n" +
                    "}\n" +
                    "```"
                )
                .Produces<Response>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status403Forbidden)
                .Produces(StatusCodes.Status404NotFound);

            return group;
        }

        private static async Task<IResult> HandleAsync(AppDbContext db, string yardId, string employeeId, HttpContext httpContext)
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

            var requester = await db.YardEmployees.FirstOrDefaultAsync(e => e.YardId == parsedYardId && e.UserId == user.UserId);
            if (requester is null || requester.Role != EmployeeRole.Admin)
            {
                return Results.Json(new { error = "Only yard admins can delete employees." }, statusCode: StatusCodes.Status403Forbidden);
            }

            Guid parsedEmployeeId;
            if (string.IsNullOrEmpty(employeeId) || !Guid.TryParse(employeeId, out parsedEmployeeId))
                return Results.BadRequest(new { error = "'Employee Id' must be a valid UUID." });

            var vehicle = await db.YardEmployees.FirstOrDefaultAsync(v => v.Id == parsedEmployeeId);

            if (vehicle is null)
            {
                return Results.NotFound(new { error = "YardEmployee not found" });
            }

            db.YardEmployees.Remove(vehicle);
            await db.SaveChangesAsync();

            return Results.Ok(new Response(vehicle.Id));
        }
    }
}
