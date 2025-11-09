using AutoInsight.YardEmployees.List;
using AutoInsight.YardEmployees.Get;
using AutoInsight.YardEmployees.Delete;
using AutoInsight.YardEmployees.Update;

namespace AutoInsight.YardEmployees
{
    public static class YardEmployeesEnpoints
    {
        public static RouteGroupBuilder MapYardEmployeeEnpoints(this RouteGroupBuilder group)
        {
            var employeeGroup = group.MapGroup("/yards/{yardId}/employees").WithTags("employee");

            employeeGroup
                 .MapYardEmployeeListEndpoint()
                 .MapYardEmployeeGetEndpoint()
                 .MapYardEmployeeDeleteEndpoint()
                 .MapYardEmployeeUpdateEndpoint();

            return group;
        }
    }
}
