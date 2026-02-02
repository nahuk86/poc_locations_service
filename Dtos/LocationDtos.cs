namespace poc_locations_service.Dtos
{
    public record CreateLocationRequest(
        string? ExternalReference,
        string Name,
        string AddressLine1,
        string? AddressLine2,
        string City,
        string? Region,
        string? PostalCode,
        decimal Latitude,
        decimal Longitude,
        string? Phone,
        string? Email,
        string? WebsiteUrl,
        string? HoursText,
        string[]? Countries,
        string[]? Maps
    );

    public record PatchLocationRequest(
        string? ExternalReference,
        string? Name,
        string? AddressLine1,
        string? AddressLine2,
        string? City,
        string? Region,
        string? PostalCode,
        decimal? Latitude,
        decimal? Longitude,
        string? Phone,
        string? Email,
        string? WebsiteUrl,
        string? HoursText,
        string? Status
    );

    public record UpsertLocationItem(
        string ExternalReference,
        string Name,
        string AddressLine1,
        string? AddressLine2,
        string City,
        string? Region,
        string? PostalCode,
        decimal Latitude,
        decimal Longitude,
        string? Phone,
        string? Email,
        string? WebsiteUrl,
        string? HoursText,
        string? AssociationStatus,
        string[]? Countries
    );

    public record BulkUpsertRequest(List<UpsertLocationItem> Items);

}
