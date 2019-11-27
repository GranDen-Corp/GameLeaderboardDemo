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
    public class PlayerController : ControllerBase
    {
        private readonly IClusterClient _grainClient;
        private readonly GameContext _context;
        public PlayerController(IClusterClient client, GameContext context)
        {
            _grainClient = client;
            _context = context;
        }
        // POST: api/Player
        [HttpPost]
        [ActionName("Point")]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task Point([FromForm]AddPointDto model)
        {
            if (ModelState.IsValid)
            {
                var game = _context.Games.FirstOrDefault();
                var leaderboard = _grainClient.GetGrain<IGameGrain>(game.Id);
                await leaderboard.AddPoint(model.playerId, model.point);
            }
        }
    }

    public class AddPointDto
    {
        public Guid playerId { get; set; }
        public int point { get; set; }
    }
}
