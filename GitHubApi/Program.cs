using System;
using System.IO;
using System.Net.Http;
using System.Net.Mail;
using System.Text.Json;
using System.Threading.Tasks;

namespace GitHubApi
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var repos = new[]
            {
                "phongnguyend/Practical.CleanArchitecture",
                "phongnguyend/Practical.NET",
                "phongnguyend/EntityFrameworkCore.SqlServer.SimpleBulks"
            };


            while (true)
            {
                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/100.0.4896.127 Safari/537.36");

                foreach (var repo in repos)
                {
                    var url = $"https://api.github.com/repos/{repo}";
                    var fileName = repo.Replace("/", "_") + ".txt";
                    var httpResponse = await httpClient.GetAsync(url);
                    var jsonResponse = await httpResponse.Content.ReadAsStringAsync();
                    var response = JsonSerializer.Deserialize<ApiRepsonse>(jsonResponse);


                    var currentStars = File.Exists(fileName) ? File.ReadAllText(fileName) : "";

                    File.WriteAllText(fileName, response.stargazers_count.ToString());

                    var emailBody = $"Repo: {repo}. Previous Stars: {currentStars}. Current Stars: {response.stargazers_count}";

                    WriteLog(emailBody);

                    if (currentStars != response.stargazers_count.ToString())
                    {
                        try
                        {

                            await SendAsync(repo, emailBody);
                        }
                        catch (Exception ex)
                        {
                            WriteLog(ex.ToString());
                        }
                    }
                }

                await Task.Delay(15 * 60 * 1000);
            }

        }

        private static void WriteLog(string message)
        {
            Console.WriteLine($"{DateTime.Now}: {message}");
        }

        public static async Task SendAsync(string repo, string body)
        {
            var mail = new MailMessage
            {
                From = new MailAddress("abc@abc.com", "GitHub API"),
            };
            mail.To.Add("abc@abc.com");

            mail.Subject = $"GitHub: {repo}";

            mail.Body = body;

            mail.IsBodyHtml = true;

            var smtpClient = new SmtpClient("localhost");

            await smtpClient.SendMailAsync(mail);
        }
    }

    public class ApiRepsonse
    {
        public int stargazers_count { get; set; }
    }
}
