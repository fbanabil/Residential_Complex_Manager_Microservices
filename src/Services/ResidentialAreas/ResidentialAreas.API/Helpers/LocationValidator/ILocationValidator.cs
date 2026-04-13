namespace ResidentialAreas.API.Helpers.LocationValidator
{
    public interface ILocationValidator
    {
        public Task<bool> IsValidLocationAsync(string country, string state, string city, string postalCode, CancellationToken cancellationToken = default);
    }
}
