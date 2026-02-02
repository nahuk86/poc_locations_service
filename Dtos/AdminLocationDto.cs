namespace poc_locations_service.Dtos
{
    public sealed class AdminLocationDto
    {
        public Guid location_id { get; init; }
        public string? external_reference { get; init; }

        public string name { get; init; } = default!;
        public string address_line1 { get; init; } = default!;
        public string? address_line2 { get; init; }

        public string city { get; init; } = default!;
        public string? region { get; init; }
        public string? postal_code { get; init; }

        public decimal latitude { get; init; }
        public decimal longitude { get; init; }

        public string? phone { get; init; }
        public string? email { get; init; }
        public string? website_url { get; init; }
        public string? hours_text { get; init; }

        public string status { get; init; } = "ACTIVE";
        public DateTime created_at { get; init; }
        public DateTime updated_at { get; init; }
    }
}
