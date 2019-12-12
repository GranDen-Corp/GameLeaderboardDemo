using Common;
using Common.Models;
using CsvHelper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace ResultValidation
{
    public class RequestData
    {
        public Guid gameId { get; set; }
        public Guid playerId { get; set; }
        public int score { get; set; }
    }
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory())
                            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: true, reloadOnChange: true)
                            .AddEnvironmentVariables();
            var configuration = builder.Build();
            string request_data_filepath = @"C:\Users\Gundambox-GranDen\Desktop\GameLeaderboard_Development\Request_data.csv";
            List<RequestData> request_data = read_request_data(request_data_filepath);
            var summaries = summary_request_data(request_data);
            var optionsBuilder = new DbContextOptionsBuilder<GameContext>();
            optionsBuilder.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
            using (var context = new GameContext(options: optionsBuilder.Options))
            {
                foreach (var summary in summaries)
                {
                    var db_summary = context.Summaries.Where(s => s.GameId.Equals(summary.Key)).ToList();
                    var req_summary = summary.Value;

                    var firstNotSecond = db_summary.Except(req_summary).ToList();
                    var secondNotFirst = req_summary.Except(db_summary).ToList();

                    if (!firstNotSecond.Any() && !secondNotFirst.Any())
                    {
                        Console.WriteLine($"Game ID: {summary.Key}, Failed !");
                        Console.WriteLine("db_summary - req_summary = ");
                        foreach (var ex in firstNotSecond) {
                            Console.WriteLine($"Player ID: {ex.PlayerId}, Score: {ex.Score}, Rank: {ex.Rank}");
                        }
                        Console.WriteLine("req_summary - db_summary = ");
                        foreach (var ex in secondNotFirst)
                        {
                            Console.WriteLine($"Player ID: {ex.PlayerId}, Score: {ex.Score}, Rank: {ex.Rank}");
                        }
                    }
                    else {
                        Console.WriteLine($"Game ID: {summary.Key}, Pass !");
                    }
                }
            }
        }

        public static List<RequestData> read_request_data(string file_path)
        {
            using (var reader = new StreamReader(file_path))
            using (var csv = new CsvReader(reader))
            {
                IEnumerable<RequestData> records = csv.GetRecords<RequestData>();
                return records.ToList();
            }
        }

        public static Dictionary<Guid, IEnumerable<Summary>> summary_request_data(List<RequestData> request_datas)
        {
            var games = request_datas.GroupBy(g => g.gameId).ToDictionary(o => o.Key, o => o.ToList());
            var summaries = new Dictionary<Guid, IEnumerable<Summary>>();
            foreach (var game in games)
            {
                summaries.Add(game.Key,
                                game.Value
                                    .GroupBy(g => new { g.gameId, g.playerId })
                                    .Select(g => new Summary
                                    {
                                        GameId = g.Key.gameId,
                                        PlayerId = g.Key.playerId,
                                        Score = g.Sum(p => p.score)
                                    })
                                    .OrderBy(x => x.Score).ThenBy(x => x.PlayerId)
                                    .Select((x, index) =>
                                    {
                                        x.Rank = index + 1;
                                        return x;
                                    }).ToList());
            }
            return summaries;
        }
    }
}
