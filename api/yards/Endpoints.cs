using AutoInsight.Yards.Create;
using AutoInsight.Yards.List;
using AutoInsight.Yards.Get;
using AutoInsight.Yards.Delete;
using AutoInsight.Yards.Update;
using AutoInsight.Yards.CapacityForecast;

namespace AutoInsight.Yards
{
    public static class YardEnpoints
    {
        public static RouteGroupBuilder MapYardEnpoints(this RouteGroupBuilder group)
        {
            var yardGroup = group.MapGroup("/yards").WithTags("yard");

            yardGroup.MapYardCreateEndpoint()
                .MapYardListEndpoint()
                .MapYardGetEndpoint()
                .MapYardDeleteEndpoint()
                .MapYardUpdateEndpoint()
                .MapYardCapacityForecastEndpoint();

            return group;
        }
    }
}
