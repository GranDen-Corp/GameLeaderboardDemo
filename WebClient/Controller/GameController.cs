using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Orleans;

namespace WebClient.Controller
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class GameController : ControllerBase
    {
        private readonly IClusterClient _grainClient;
        private readonly GameContext _context;
        public GameController(IClusterClient client, GameContext context)
        {
            _grainClient = client;
            _context = context;
        }
        // POST: api/Player
        [HttpPost]
        [ActionName("Summary")]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task Point([FromForm]SummaryDto model)
        {
            if (ModelState.IsValid)
            {
                var game = _context.Games.FirstOrDefault(x => x.Id.Equals(model.gameId));
                var leaderboard = _grainClient.GetGrain<IGameGrain>(game.Id);
                await leaderboard.SummaryReport();
            }
        }
    }

    public class SummaryDto
    {
        public Guid gameId { get; set; }
    }
}