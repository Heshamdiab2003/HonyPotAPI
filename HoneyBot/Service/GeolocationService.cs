namespace HoneyBot.Service;

// Record simplifies creating data-only objects
//public record GeoLocationInfo(string Country, string City, double Latitude, double Longitude);

//public interface IGeolocationService
//{
//    Task<GeoLocationInfo?> GetGeoInfoAsync(string ip);
//}

//public class GeolocationService : IGeolocationService
//{
//    private readonly HttpClient _httpClient;

//    public GeolocationService(HttpClient httpClient)
//    {
//        _httpClient = httpClient;
//    }

//    public async Task<GeoLocationInfo?> GetGeoInfoAsync(string ip)
//    {
//        // Don't query for local or invalid IPs
//        if (ip is null || ip == "::1" || ip.StartsWith("127.0"))
//        {
//            return null;
//        }

//        try
//        {
//            var response = await _httpClient.GetFromJsonAsync<IpApiDto>($"http://ip-api.com/json/{ip}?fields=status,country,city,lat,lon");
//            if (response?.Status == "success")
//            {
//                return new GeoLocationInfo(response.Country, response.City, response.Lat, response.Lon);
//            }
//            return null;
//        }
//        catch (Exception ex)
//        {
//            // Log the exception for debugging, but don't crash the app
//            Console.WriteLine($"[Geolocation Error] Could not get info for IP {ip}: {ex.Message}");
//            return null;
//        }
//    }

//    // A private DTO to match the structure of the ip-api.com JSON response
//    private record IpApiDto(string Status, string Country, string City, double Lat, double Lon);
//}

public record GeoLocationInfo(string Country, string City, double Latitude, double Longitude);

public interface IGeolocationService
{
    // يجب أن تكون الدالة Async لأنها تنتظر ردًا من الإنترنت
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
        // لا نرسل طلبات للـ IPs المحلية أو الفارغة
        if (ip is null || ip == "::1" || ip.StartsWith("127.0"))
        {
            return null;
        }

        try
        {
            // إرسال الطلب إلى الخدمة الخارجية وانتظار الرد
            var response = await _httpClient.GetFromJsonAsync<IpApiDto>($"http://ip-api.com/json/{ip}?fields=status,country,city,lat,lon");

            // إذا كان الرد ناجحًا، قم بإنشاء كائن بالمعلومات
            if (response?.Status == "success")
            {
                return new GeoLocationInfo(response.Country, response.City, response.Lat, response.Lon);
            }
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Geolocation Error] لم يتمكن من جلب معلومات للـ IP {ip}: {ex.Message}");
            return null;
        }
    }

    // كائن خاص لمطابقة شكل الرد القادم من ip-api.com
    private record IpApiDto(string Status, string Country, string City, double Lat, double Lon);
}