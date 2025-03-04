using System;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

class Program
{
    private static int score = 0;
    private static string consoleNumber, consoleCode, playerName;
    private static int port;
    private static readonly HttpClient httpClient = new HttpClient();

    public static void Main()
    {
        Console.WriteLine("Old Console Started! \n");
        consoleNumber = $"Old-{Guid.NewGuid()}";
        Console.WriteLine($"Generated Console Number: {consoleNumber}");

        consoleCode = $"O{new Random().Next(100000, 999999)}";
        Console.WriteLine($"Generated Console Code: {consoleCode}");

        Console.Write("Enter Player Name (optional): ");
        playerName = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(playerName)) playerName = "N/A";

        port = GetAvailablePort();
        Console.WriteLine($"Old Console {consoleNumber} Started on Port {port}!");

        Task.Run(() => IncreaseScoreAutomatically());
        Task.Run(() => SendUpdatesToDashboard());

        CreateHostBuilder(port).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(int port) =>
        Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseUrls($"http://localhost:{port}")
                          .Configure(app =>
                          {
                              app.Run(async context =>
                              {
                                  if (context.Request.Path == "/details")
                                  {
                                      var response = JsonSerializer.Serialize(new
                                      {
                                          Number = consoleNumber,
                                          Code = consoleCode,
                                          PlayerName = playerName,
                                          Score = score,
                                          Status = "Running",
                                          IsOld = true,
                                          Port = port
                                      });

                                      await context.Response.WriteAsync(response);
                                  }
                              });
                          });
            });

    private static void IncreaseScoreAutomatically()
    {
        while (true)
        {
            score++;
            Console.WriteLine($"Console {consoleNumber} | Score: {score}");
            Task.Delay(2000).Wait();
        }
    }

    private static async Task SendUpdatesToDashboard()
    {
        while (true)
        {
            try
            {
                var data = new
                {
                    Number = consoleNumber,
                    Code = consoleCode,
                    PlayerName = playerName,
                    Score = score,
                    IsOld = true,
                    Port = port
                };

                var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
                await httpClient.PostAsync("http://localhost:5000/update", content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to update Main Dashboard: {ex.Message}");
            }

            await Task.Delay(5000);
        }
    }

    private static int GetAvailablePort()
    {
        using (TcpListener listener = new TcpListener(System.Net.IPAddress.Loopback, 0))
        {
            listener.Start();
            int assignedPort = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return assignedPort;
        }
    }
}
