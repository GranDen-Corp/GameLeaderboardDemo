using System;
using System.Linq;

namespace Common
{
    public class DBInitializer
    {
        public static void Initialize(GameContext context)
        {
            context.Database.EnsureCreated();

            if (!context.Players.Any())
            {
                Enumerable.Range(1, 1000).ToList().ForEach(x =>
                {
                    context.Players.Add(new Models.Player
                    {
                        Id = Guid.NewGuid(),
                        Name = $"Player No.{x}"
                    });
                });
                context.SaveChanges();
            }

            if (!context.Games.Any())
            {
                Enumerable.Range(1, 10).ToList().ForEach(x =>
                {
                    context.Games.Add(new Models.Game
                    {
                        Id = Guid.NewGuid(),
                        Name = $"Game No.{x}"
                    });
                });
                context.SaveChanges();
            }
        }
    }
}
