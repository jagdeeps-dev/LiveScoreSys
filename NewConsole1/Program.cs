using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class NewConsole
{
    private static HttpClient client = new HttpClient();
    private static int score = 0;
    private static string consoleNumber, consoleCode, playerName;

    static async Task Main()
    {
        Console.WriteLine("New Console Started! \n");
        consoleNumber = $"New-{Guid.NewGuid()}";
        consoleCode = $"N{new Random().Next(100000, 999999)}";

        Console.WriteLine($"Generated Console Number: {consoleNumber}");
        Console.WriteLine($"Generated Console Code: {consoleCode}");

        Console.Write("Enter Player Name (optional): ");
        playerName = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(playerName)) playerName = "N/A";

        Console.WriteLine("Press '+' to increase score...");

        while (true)
        {
            var key = Console.ReadKey(true).Key;
            if (key == ConsoleKey.Add || key == ConsoleKey.OemPlus)
            {
                score++;
                Console.WriteLine($"Console {consoleNumber} | Score: {score}");
                await SendScoreUpdate();
            }
        }
    }

    private static async Task SendScoreUpdate()
    {
        try
        {
            var data = new
            {
                Number = consoleNumber,
                Code = consoleCode,
                PlayerName = playerName,
                Score = score,
                IsOld = false
            };

            var json = JsonSerializer.Serialize(data);
            var response = await client.PostAsync("http://localhost:5000/update",
                new StringContent(json, Encoding.UTF8, "application/json"));

            if (response.IsSuccessStatusCode)
                Console.WriteLine("Score Updated!");
            else
                Console.WriteLine($"Failed to update score. Status: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending score update: {ex.Message}");
        }
    }
}
