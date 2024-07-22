using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AltinFiyatlari
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Çevre değişkenlerinden API anahtarlarını alıyoruz
            string metalsApiKey = Environment.GetEnvironmentVariable("METALS_API_KEY");

            if (string.IsNullOrEmpty(metalsApiKey))
            {
                Console.WriteLine("Metals API anahtarı bulunamadı. Lütfen 'METALS_API_KEY' çevre değişkenini tanımlayın.");
                return;
            }

            // API URL'lerini burada tanımlıyoruz
            string metalsApiUrl = $"https://metals-api.com/api/latest?access_key={metalsApiKey}&base=USD&symbols=XAU";
            string exchangeRateApiUrl = "https://v6.exchangerate-api.com/v6/c1aa0f2b60a2149ca2e441e9/latest/USD";

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    // Metals API çağrı
                    HttpResponseMessage metalsResponse = await client.GetAsync(metalsApiUrl);
                    metalsResponse.EnsureSuccessStatusCode();

                    string metalsResponseBody = await metalsResponse.Content.ReadAsStringAsync();

                    // JSON verisini serileştiriyoruz ve daha düzenli bir şekilde yazdırıyoruz
                    var metalsJson = JObject.Parse(metalsResponseBody);
                    var goldPricePerOunceToken = metalsJson["rates"]?["USDXAU"];

                    if (goldPricePerOunceToken == null)
                    {
                        Console.WriteLine("Altın fiyatı bulunamadı.");
                        return;
                    }

                    Console.WriteLine($"Altın fiyatı (ons başına USD): {goldPricePerOunceToken}");

                    // Parse edilen fiyatın doğru olup olmadığını kontrol ediyoruz
                    if (!decimal.TryParse(goldPricePerOunceToken.ToString(), out var goldPricePerOunce))
                    {
                        Console.WriteLine("Altın fiyatı doğru şekilde parse edilemedi.");
                        return;
                    }


                    // 1 ons = 31.1035 gram
                    decimal gramsPerOunce = 31.1035m;
                    decimal goldPricePerGram = goldPricePerOunce / gramsPerOunce;

                    Console.WriteLine($"1 gram altının USD cinsinden fiyatı: {goldPricePerGram:F2} USD");

                    // Döviz kuru API'sini kullanarak USD/TL kurunu alıyoruz
                    HttpResponseMessage exchangeRateResponse = await client.GetAsync(exchangeRateApiUrl);
                    exchangeRateResponse.EnsureSuccessStatusCode();

                    string exchangeRateResponseBody = await exchangeRateResponse.Content.ReadAsStringAsync();

                    var exchangeRateJson = JObject.Parse(exchangeRateResponseBody);
                    var usdToTryRate = exchangeRateJson["conversion_rates"]?["TRY"];

                    if (usdToTryRate == null)
                    {
                        Console.WriteLine("USD/TL kuru bulunamadı.");
                        return;
                    }

                    // Parse edilen kurun doğru olup olmadığını kontrol ediyoruz
                    if (!decimal.TryParse(usdToTryRate.ToString(), out var usdToTry))
                    {
                        Console.WriteLine("USD/TL kuru doğru şekilde parse edilemedi.");
                        return;
                    }

                    // USD cinsinden gram altın fiyatını TL'ye çeviriyoruz
                    decimal goldPricePerGramInTry = goldPricePerGram * usdToTry;

                    Console.WriteLine($"1 gram altının TL cinsinden fiyatı: {goldPricePerGramInTry:F2} TL");
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine("\nException Caught!");
                    Console.WriteLine("Message :{0} ", e.Message);
                }
                catch (JsonReaderException e)
                {
                    Console.WriteLine("\nException Caught!");
                    Console.WriteLine("Invalid JSON format: {0}", e.Message);
                }
                catch (Exception e)
                {
                    Console.WriteLine("\nException Caught!");
                    Console.WriteLine("An error occurred: {0}", e.Message);
                }
            }
        }
    }
}