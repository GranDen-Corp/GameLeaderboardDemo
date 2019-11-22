using Common;
using Common.Models;
using Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Streams;
using System;
using System.Linq;
using System.Threading.Tasks;
using WebClient.Hubs;

namespace WebClient.Pages
{
    public class TopRankModel : PageModel
    {
        private readonly ILogger<TopRankModel> _logger;
        private readonly GameContext _context;
        private readonly IHubContext<GameHub> _hubContext;
        private readonly IClusterClient _grainClient;
        public Game Game { get; set; }

        public TopRankModel(IClusterClient grainClient,
            GameContext context,
            IHubContext<GameHub> hubContext,
            ILogger<TopRankModel> logger)
        {
            _hubContext = hubContext;
            _grainClient = grainClient;
            _logger = logger;
            _context = context;
        }
        public async Task OnGetAsync(Guid id)
        {
            this.Game = this._context.Games.FirstOrDefault(x => x.Id.Equals(id));
            var game = this._grainClient.GetGrain<IGameGrain>(this.Game.Id);
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
        }
        public async Task<JsonResult> OnGetRefresh([FromRoute]Guid id)
        {
            var grain = this._grainClient.GetGrain<IGameGrain>(id);
            var topRanks = await grain.GetTopPlayer();
            var players = this._context.Players.ToDictionary(k => k.Id, v => v.Name);
            var ranks = topRanks.Select(rank => new
            {
                Rank = rank.Rank,
                Id = rank.Id,
                Name = players[rank.Id],
                Score = rank.Score
            }).ToList();
            return new JsonResult(ranks);
        }
    }
}