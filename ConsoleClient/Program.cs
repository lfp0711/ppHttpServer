using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleClient
{
    class Program
    {

        static async Task Main()
        {
            int threadCount = 20;

            Program p = new Program();
            // Send the request X times in parallel
            Task.WhenAll(Enumerable.Range(1, threadCount).Select(i => p.GetAsync())).GetAwaiter().GetResult();
        }

        private async Task GetAsync()
        {
            int count = 30;
            HttpClient client = new HttpClient();
            var byteArray = Encoding.ASCII.GetBytes("jack:123456");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

            DateTime startTime = DateTime.Now;
            for (int i = 0; i < count; i++)
            {
                HttpResponseMessage response = await client.GetAsync("http://127.0.0.1:8080/hello?name=t" + Thread.CurrentThread.ManagedThreadId);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                // Above three lines can be replaced with new helper method below
                // string responseBody = await client.GetStringAsync(uri);

                Console.WriteLine(Thread.CurrentThread.ManagedThreadId + " - " + i + ": " + responseBody);
            }
            Console.WriteLine(Thread.CurrentThread.ManagedThreadId + ", Time: " + (DateTime.Now.Subtract(startTime)));
        }
    }
}
