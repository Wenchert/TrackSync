using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

public static class NetworkHelper
{
    public static async Task<string> GetExternalIpAddressAsync()
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(5);
            return await httpClient.GetStringAsync("https://api.ipify.org");
        }
        catch
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    return ip.ToString();
            }
            return "127.0.0.1";
        }
    }
}