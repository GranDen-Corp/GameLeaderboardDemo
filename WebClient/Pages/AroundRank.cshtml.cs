using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Models;
using Orleans.Streams;
using Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Orleans;
using WebClient.Hubs;

namespace WebClient.Pages
{
    public class AroundRankModel : PageModel
    {
        private readonly ILogger<AroundRankModel> _logger;
        private readonly GameContext _context;
        private readonly IHubContext<GameHub> _hubContext;
        private readonly IClusterClient _grainClient;
        public Game Game { get; set; }
        public Dictionary<Guid, string> Players { get; set; }

        public AroundRankModel(IClusterClient grainClient,
            GameContext context,
            IHubContext<GameHub> hubContext,
            ILogger<AroundRankModel> logger)
        {
            _hubContext = hubContext;
            _grainClient = grainClient;
            _logger = logger;
            _context = context;
        }
        public async Task OnGetAsync(Guid id)
        {
            this.Game = this._context.Games.FirstOrDefault(x => x.Id.Equals(id));
            this.Players = this._context.Players.ToDictionary(k => k.Id, v => v.Name);

            var game = this._grainClient.GetGrain<IGameGrain>(this.Game.Id);
            var joinedPlayers = await game.GetJoinedPlayers();
            var streamId = await game.Subscribe();
            var stream = this._grainClient
                                .GetStreamProvider(Constants.OrleansStreamProvider)
                                .GetStream<bool>(streamId, Constants.OrleansStreamNameSpace);
            await stream.SubscribeAsync(async (isChanged, token) =>
            {
                if (isChanged)
                {
                    await _hubContext.Clients.All.SendAsync("BroadcastLeaderboardUpdate");
                }
            });

            this.Players = this.Players.Where(p => joinedPlayers.Contains(p.Key)).ToDictionary(k => k.Key, v => v.Value);
        }
        public async Task<JsonResult> OnGetRefresh([FromRoute(Name = "id")]Guid leaderboardId, [FromQuery(Name = "playerId")]Guid? playerId)
        {
            if (playerId.HasValue)
            {
                this.Players = this._context.Players.ToDictionary(k => k.Id, v => v.Name);

                var game = this._grainClient.GetGrain<IGameGrain>(leaderboardId);
                var joinedPlayers = await game.GetJoinedPlayers();
                this.Players = this.Players.Where(p => joinedPlayers.Contains(p.Key)).ToDictionary(k => k.Key, v => v.Value);

                var aboveRanks = await game.GetAbovePlayer(playerId.Value);
                var belowRanks = await game.GetBelowPlayer(playerId.Value);
                var total = aboveRanks.ToList();
                total.AddRange(belowRanks.Skip(1));

                var ranks = total.Select((rank, index) => new 
                {
                    Rank = rank.Rank,
                    Id = rank.Id,
                    Name = Players[rank.Id],
                    Score = rank.Score
                }).ToList();
                return new JsonResult(ranks);
            }
            else
            {
                return new JsonResult(string.Empty);
            }
        }
    }
}