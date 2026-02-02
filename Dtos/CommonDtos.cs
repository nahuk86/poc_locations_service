namespace poc_locations_service.Dtos
{
    public record CreateCountryRequest(string CountryId, string Name);
    public record PatchCountryRequest(string? Name, string? Status);

    public record CreateMapRequest(string MapId, string Name, string? Description);
    public record PatchMapRequest(string? Name, string? Description, string? Status);

    public record BulkAssociateItem(Guid LocationId, string? Status);
    public record BulkAssociateRequest(string Mode, List<BulkAssociateItem> Items);

}
