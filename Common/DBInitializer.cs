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
                Enumerable.Range(1, 200).ToList().ForEach(x =>
                {
                    context.Players.Add(new Models.Player
                    {
                        Id = Guid.NewGuid(),
                        Name = $"Player No.{x}"
                    });
                });
                context.SaveChanges();
            }
        }
    }
}
