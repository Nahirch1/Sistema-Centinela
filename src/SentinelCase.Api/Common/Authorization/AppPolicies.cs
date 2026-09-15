namespace SentinelCase.Api.Common.Authorization;

public static class AppPolicies
{
    public const string CanCreateIncident = "CanCreateIncident";
    public const string CanManageIncidentStatus = "CanManageIncidentStatus";
    public const string CanAssignIncident = "CanAssignIncident";
    public const string RequireAssetApiKey = "RequireAssetApiKey";
}
