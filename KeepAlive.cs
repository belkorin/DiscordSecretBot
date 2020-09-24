using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace bottest
{
    public class KeepAlive
    {
        public static async void Ping()
        {
            var httpClient = new HttpClient();

            while(true)
            {
                var zzz = await (await httpClient.GetAsync($"http://${Environment.GetEnvironmentVariable("PROJECT_DOMAIN")}.glitch.me/")).Content.ReadAsStringAsync();

                await Task.Delay(TimeSpan.FromMinutes(4));
            }
        }
    }
}
