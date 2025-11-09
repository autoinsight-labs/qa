using AutoInsight.EmployeeInvites.Create;
using AutoInsight.EmployeeInvites.Get;
using AutoInsight.EmployeeInvites.List;
using AutoInsight.EmployeeInvites.Delete;
using AutoInsight.EmployeeInvites.Accept;
using AutoInsight.EmployeeInvites.Reject;
using AutoInsight.EmployeeInvites.ListUser;

namespace AutoInsight.EmployeeInvites
{
    public static class EmployeeInvitesEnpoints
    {
        public static RouteGroupBuilder MapEmployeeInviteEnpoints(this RouteGroupBuilder group)
        {
            var yardEmployeeInviteGroup = group.MapGroup("/yards/{yardId}/invites").WithTags("invite");
            var employeeInviteGroup = group.MapGroup("/invites").WithTags("invite");

            yardEmployeeInviteGroup
                            .MapEmployeeInviteCreateEndpoint()
                            .MapEmployeeInviteListEndpoint();

            employeeInviteGroup
                            .MapEmployeeInviteGetEndpoint()
                            .MapEmployeeInviteDeleteEndpoint()
                            .MapEmployeeInviteAcceptEndpoint()
                            .MapEmployeeInviteRejectEndpoint()
                            .MapEmployeeInviteListUserEndpoint();

            return group;
        }
    }
}
