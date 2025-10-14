namespace HoneyBot.Service;

// Record simplifies creating data-only objects
public record GeoLocationInfo(string Country, string City, double Latitude, double Longitude);

public interface IGeolocationService
{
    Task<GeoLocationInfo?> GetGeoInfoAsync(string ip);
}

public class GeolocationService : IGeolocationService
{
    private readonly HttpClient _httpClient;

    public GeolocationService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<GeoLocationInfo?> GetGeoInfoAsync(string ip)
    {
        // Don't query for local or invalid IPs
        if (ip is null || ip == "::1" || ip.StartsWith("127.0"))
        {
            return null;
        }

        try
        {
            var response = await _httpClient.GetFromJsonAsync<IpApiDto>($"http://ip-api.com/json/{ip}?fields=status,country,city,lat,lon");
            if (response?.Status == "success")
            {
                return new GeoLocationInfo(response.Country, response.City, response.Lat, response.Lon);
            }
            return null;
        }
        catch (Exception ex)
        {
            // Log the exception for debugging, but don't crash the app
            Console.WriteLine($"[Geolocation Error] Could not get info for IP {ip}: {ex.Message}");
            return null;
        }
    }

    // A private DTO to match the structure of the ip-api.com JSON response
    private record IpApiDto(string Status, string Country, string City, double Lat, double Lon);
}