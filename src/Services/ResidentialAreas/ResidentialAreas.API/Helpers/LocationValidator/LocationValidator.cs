using System.Text.Json;
using System.Text.Json.Serialization;

namespace ResidentialAreas.API.Helpers.LocationValidator
{
    public class LocationValidator: ILocationValidator
    {
        private readonly HttpClient _httpClient;
        private readonly HttpClient _zippopotamClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LocationValidator> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public LocationValidator(HttpClient httpClient,HttpClient zippopotamClient, IConfiguration configuration, ILogger<LocationValidator> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            var baseUrl = _configuration.GetSection("LocationValidator:BaseUrl").Value;
            var apiKey = _configuration.GetSection("LocationValidator:ApiKey").Value;
            _httpClient.BaseAddress = baseUrl != null ? new Uri(baseUrl) : null;
            _httpClient.DefaultRequestHeaders.Add("X-CSCAPI-KEY", apiKey);
            _logger = logger;

            _zippopotamClient = zippopotamClient;
            _zippopotamClient.BaseAddress = new Uri("http://api.zippopotam.us/");

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString 
            };
        }
        
        public async Task<bool> IsValidLocationAsync(string country, string state, string city, string postalCode, CancellationToken cancellationToken = default)
        {
            // Country validation


            var response = await _httpClient.GetAsync("countries");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to validate location. Status Code: {StatusCode}, Reason: {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
                return await Task.FromResult(false);
            }
            string responseContent = await response.Content.ReadAsStringAsync();
            List<CountryResponse>? countries = JsonSerializer.Deserialize<List<CountryResponse>>(responseContent, _jsonOptions);
            if (countries == null || !countries.Any())
            {
                _logger.LogWarning("No countries found in the response.");
                return await Task.FromResult(false);
            }
            var matchedCountry = countries.FirstOrDefault(c => string.Equals(c.Name, country, StringComparison.OrdinalIgnoreCase) || string.Equals(c.Iso2, country, StringComparison.OrdinalIgnoreCase) || string.Equals(c.Iso3, country, StringComparison.OrdinalIgnoreCase));
            if (matchedCountry == null)
            {
                _logger.LogWarning("No matching country found for {Country}", country);
                return await Task.FromResult(false);
            }


            // State Validation


            var stateResponse = await _httpClient.GetAsync($"countries/{matchedCountry.Iso2}/states");
            if (!stateResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to validate state. Status Code: {StatusCode}, Reason: {ReasonPhrase}", stateResponse.StatusCode, stateResponse.ReasonPhrase);
                return await Task.FromResult(false);
            }
            string stateResponseContent = await stateResponse.Content.ReadAsStringAsync();
            List<StateResponse>? states = JsonSerializer.Deserialize<List<StateResponse>>(stateResponseContent, _jsonOptions);
            if (states == null || !states.Any())
            {
                _logger.LogWarning("No states found in the response for country {Country}", matchedCountry.Name);
                return await Task.FromResult(false);
            }
            var matchedState = states.FirstOrDefault(s => string.Equals(s.Name, state, StringComparison.OrdinalIgnoreCase) || string.Equals(s.Iso2, state, StringComparison.OrdinalIgnoreCase));
            if (matchedState == null)
            {
                _logger.LogWarning("No matching state found for {State} in country {Country}", state, matchedCountry.Name);
                return await Task.FromResult(false);
            }



            // City Validation Need Paid Plan

            var cityResponse = await _httpClient.GetAsync($"countries/{matchedCountry.Iso2}/cities");
            if (!cityResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to validate city. Status Code: {StatusCode}, Reason: {ReasonPhrase}", cityResponse.StatusCode, cityResponse.ReasonPhrase);
                return await Task.FromResult(false);
            }

            string cityResponseContent = await cityResponse.Content.ReadAsStringAsync();
            List<CityResponse>? cities = JsonSerializer.Deserialize<List<CityResponse>>(cityResponseContent, _jsonOptions);
            if (cities == null || !cities.Any())
            {
                _logger.LogWarning("No cities found in the response for state {State} in country {Country}", matchedState.Name, matchedCountry.Name);
                return await Task.FromResult(false);
            }
            var matchedCity = cities.FirstOrDefault(c => string.Equals(c.Name, city, StringComparison.OrdinalIgnoreCase));
            if (matchedCity == null)
            {
                _logger.LogWarning("No matching city found for {City} in state {State} and country {Country}", city, matchedState.Name, matchedCountry.Name);
                return await Task.FromResult(false);
            }


            // Postal code validation


            var postalCodeResponse = await _zippopotamClient.GetAsync($"{matchedCountry.Iso2}/{postalCode}");
            if (!postalCodeResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("No matching postal code found for {PostalCode} and country {Country}", postalCode, matchedCountry.Name);
                return await Task.FromResult(false);
            }

            return await Task.FromResult(true);
        }
        
    }
}