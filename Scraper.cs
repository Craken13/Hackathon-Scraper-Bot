using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Mail;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

class Program
{
    private static readonly HttpClient client = new HttpClient();
    private static IConfiguration configuration;
    private static ILogger<Program> logger;

    static async Task Main(string[] args)
    {
        // Load environment variables
        var builder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();
        configuration = builder.Build();

        // Configure logging
        using var loggerFactory = LoggerFactory.Create(loggingBuilder => loggingBuilder.AddConsole());
        logger = loggerFactory.CreateLogger<Program>();

        var hackathons = await ScrapeHackathons();
        if (hackathons.Count > 0)
        {
            SendEmail(hackathons);
        }
        else
        {
            logger.LogInformation("No hackathons found.");
        }
    }

    private static async Task<List<string>> ScrapeHackathons()
    {
        var url = "https://mlh.io/seasons/2024/events";
        var hackathons = new List<string>();

        try
        {
            var response = await client.GetStringAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(response);

            foreach (var eventNode in doc.DocumentNode.SelectNodes("//div[contains(@class, 'event')]"))
            {
                var title = eventNode.SelectSingleNode(".//h3").InnerText.Trim();
                var date = eventNode.SelectSingleNode(".//div[contains(@class, 'event-date')]").InnerText.Trim();
                var location = eventNode.SelectSingleNode(".//div[contains(@class, 'event-location')]").InnerText.Trim();
                var link = eventNode.SelectSingleNode(".//a").GetAttributeValue("href", string.Empty);

                if (location.Contains("South Africa"))
                {
                    hackathons.Add($"{title} - {date} - {location} - {link}");
                }
            }
        }
        catch (HttpRequestException e)
        {
            logger.LogError($"Error fetching hackathons: {e.Message}");
        }

        return hackathons;
    }

    private static void SendEmail(List<string> hackathons)
    {
        var senderEmail = configuration["EMAIL_SENDER"];
        var receiverEmail = configuration["EMAIL_RECEIVER"];
        var password = configuration["EMAIL_PASSWORD"];

        if (string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(receiverEmail) || string.IsNullOrEmpty(password))
        {
            logger.LogError("Email credentials are not set in environment variables.");
            return;
        }

        var subject = "Upcoming Hackathons in South Africa!";
        var body = string.Join("\n", hackathons);

        var msg = new MailMessage(senderEmail, receiverEmail, subject, body);

        using var client = new SmtpClient("smtp.gmail.com", 587)
        {
            Credentials = new System.Net.NetworkCredential(senderEmail, password),
            EnableSsl = true
        };

        try
        {
            client.Send(msg);
            logger.LogInformation("Email sent successfully.");
        }
        catch (SmtpException e)
        {
            logger.LogError($"Error sending email: {e.Message}");
        }
    }
}