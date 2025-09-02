namespace Exercise1;

using System;
using System.Net.Http;
using System.Text.Json;

class Program
{
    private static readonly HttpClient httpClient = new HttpClient();

    static async Task Main(string[] args)
    {
        int concurrency = 5;

        // do it 1000 times
        for (var x = 0; x < 1000 / concurrency; x++)
        {
            var tasks = new List<Task>();

            for (var i = 0; i < concurrency; i++)
            {
                tasks.Add(MakeRequest(x * concurrency + i));
            }

            await Task.WhenAll(tasks);
        }
    }

    static async Task MakeRequest(int attemptNumber)
    {
        try
        {
            var response = await httpClient.GetAsync("http://192.168.88.135/api/openchest");
            response.EnsureSuccessStatusCode();

            string jsonResponse = await response.Content.ReadAsStringAsync();

            var openChestResponse = JsonSerializer.Deserialize<OpenChestResponse>(jsonResponse);
            Console.WriteLine($"Attempt {attemptNumber} {DateTime.UtcNow.ToShortTimeString()} - ChestIsOpening = {openChestResponse!.IsOpening}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failure on attempt {attemptNumber}");
            Console.WriteLine(ex.Message);
        }
    }
}

class OpenChestResponse
{
    public bool IsOpening { get; set; }
}

class DoorUnlockResponse
{
    public bool IsUnlocked { get; set; }
}