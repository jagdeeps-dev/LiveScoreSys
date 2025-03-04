using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

public class Program
{
    private static List<ConsoleGame> consoles = new List<ConsoleGame>();
    private static HttpClient httpClient = new HttpClient();
    private static Dictionary<string, DateTime> lastUpdateTimes = new Dictionary<string, DateTime>();

    public static void Main()
    {
        Console.WriteLine("Main Board Started!");
        Task.Run(() => StartServer());
        Task.Run(() => FetchOldConsoles());
        RunBoard();
    }

    private static void StartServer()
    {
        Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseUrls("http://localhost:5000")
                          .Configure(app =>
                          {
                              app.UseRouting();
                              app.UseEndpoints(endpoints =>
                              {
                                  // API to receive data from consoles
                                  endpoints.MapPost("/update", async context =>
                                  {
                                      try
                                      {
                                          var request = await JsonSerializer.DeserializeAsync<ConsoleGame>(context.Request.Body);
                                          if (request != null)
                                          {
                                              var existingConsole = consoles.FirstOrDefault(c => c.Number == request.Number);
                                              if (existingConsole != null)
                                              {
                                                  existingConsole.Score = request.Score;
                                                  existingConsole.Status = "Running";
                                                  lastUpdateTimes[existingConsole.Number] = DateTime.Now;
                                              }
                                              else
                                              {
                                                  consoles.Add(request);
                                                  lastUpdateTimes[request.Number] = DateTime.Now;
                                              }
                                              await context.Response.WriteAsync("Console Data Updated!");
                                          }
                                      }
                                      catch (Exception ex)
                                      {
                                          await context.Response.WriteAsync("Error processing request: " + ex.Message);
                                      }
                                  });
                              });
                          });
            }).Build().Run();
    }

    private static async Task FetchOldConsoles()
    {
        while (true)
        {
            foreach (var console in consoles.Where(c => c.IsOld))
            {
                try
                {
                    var response = await httpClient.GetStringAsync($"http://localhost:{console.Port}/details");
                    var updatedConsole = JsonSerializer.Deserialize<ConsoleGame>(response);

                    console.Score = updatedConsole.Score;
                    console.Status = "Running"; // If request succeeds, it's running
                }
                catch
                {
                    console.Status = "Stopped"; // If request fails, assume stopped
                }
            }
            await Task.Delay(5000);
        }
    }

    private static void RunBoard()
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("Main Dashboard - Live Scores");
            Console.WriteLine("================================");

            foreach (var c in consoles)
            {
                if (!c.IsOld && lastUpdateTimes.ContainsKey(c.Number) &&
                    (DateTime.Now - lastUpdateTimes[c.Number]).TotalSeconds > 10)
                {
                    c.Status = "Stopped";
                }

                string consoleType = c.IsOld ? "Old Console" : "New Console";
                Console.WriteLine($"[{consoleType}] Console {c.Number} | Code: {c.Code} | Player: {c.PlayerName} | Score: {c.Score} | Status: {c.Status}");
            }

            Task.Delay(2000).Wait();
        }
    }
}

public class ConsoleGame
{
    public string Number { get; set; }
    public string Code { get; set; }
    public string PlayerName { get; set; }
    public int Score { get; set; } = 0;
    public string Status { get; set; } = "Running";
    public bool IsOld { get; set; }
    public int Port { get; set; }
}
