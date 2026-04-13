using System.Text.Json.Serialization;

namespace ResidentialAreas.API.Helpers.LocationValidator
{
    public class CountryResponse
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Iso2 { get; set; }
        public string? Iso3 { get; set; }
        public string? Phonecode { get; set; }
        public string? Capital { get; set; }
        public string? Currency { get; set; }
        public string? Native { get; set; }
        public string? Region { get; set; }
        [JsonPropertyName("region_id")]
        public string? RegionId { get; set; }
        public string? Subregion { get; set; }
        [JsonPropertyName("subregion_id")]
        public string? SubregionId { get; set; }
        public string? Timezones { get; set; }
        public string? Latitude { get; set; }
        public string? Longitude { get; set; }
        public string? Emoji { get; set; }
    }


    public class StateResponse
    {
        public int Id { get; set; }
        public string? Name { get; set; }

        [JsonPropertyName("country_id")]
        public string? CountryId { get; set; }

        [JsonPropertyName("country_code")]
        public string? CountryCode { get; set; }
        public string? Iso2 { get; set; }
        public string? Latitude { get; set; }
        public string? Longitude { get; set; }
        public string? Timezone { get; set; }
    }

    public class CityResponse
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }
}
