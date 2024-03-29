using Common;
using Common.Dto;
using Common.Models;
using Interfaces;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Providers;
using Orleans.Runtime;
using Orleans.Streams;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Grains
{
    [Serializable]
    public class GameState
    {
        public List<KeyValuePair<Guid, int>> Leaderboard { get; set; } = new List<KeyValuePair<Guid, int>>();
    }
    [StorageProvider(ProviderName = Constants.OrleansDataStorageProvider)]
    public class GameGrain : Grain, IGameGrain, IRemindable
    {
        private bool isStateChanged = false;
        private IAsyncStream<bool> stream;
        private IGrainReminder _reminder;
        private readonly ILogger<GameGrain> _logger;
        private readonly IPersistentState<GameState> _game;
        private readonly GameContext _gameContext;

        public GameGrain(ILogger<GameGrain> logger,
            [PersistentState(nameof(GameState), Constants.OrleansDataStorageProvider)] IPersistentState<GameState> game,
            GameContext gameContext)
        {
            _logger = logger;
            _game = game;
            _gameContext = gameContext;
        }
        public override async Task OnActivateAsync()
        {
            var streamProvider = GetStreamProvider(Constants.OrleansStreamProvider);
            stream = streamProvider.GetStream<bool>(this.GetPrimaryKey(), Constants.OrleansStreamNameSpace);

            RegisterTimer(async o =>
            {
                if (isStateChanged)
                {
                    await _game.WriteStateAsync();
                    isStateChanged = false;
                }
                else
                {
                    await _game.ReadStateAsync();
                }
            }, null,
            TimeSpan.FromSeconds(Constants.WriteBackPeriodSecond),
            TimeSpan.FromSeconds(Constants.WriteBackPeriodSecond));

            _reminder = await RegisterOrUpdateReminder(
                this.GetPrimaryKey().ToString(),
                TimeSpan.FromMinutes(Constants.WriteStreamDelayMinute),
                TimeSpan.FromMinutes(Constants.WriteStreamDelayMinute) // apparently the minimum
            );

            await base.OnActivateAsync();
            return;
        }

        public override async Task OnDeactivateAsync()
        {
            await UnregisterReminder(_reminder);
            await base.OnDeactivateAsync();
            return;
        }
        public async Task AddPoint(Guid playerId, int point)
        {
            try
            {
                _logger.LogWarning(this.RuntimeIdentity);
                var rank = await GetPlayerRank(playerId);
                if (rank < 0)
                {
                    _game.State.Leaderboard.Add(new KeyValuePair<Guid, int>(playerId, point));
                }
                else
                {
                    var newPoint = _game.State.Leaderboard[rank].Value + point;
                    _game.State.Leaderboard[rank] = new KeyValuePair<Guid, int>(playerId, newPoint);
                }
                _game.State.Leaderboard.Sort((x, y) => -1 * x.Value.CompareTo(y.Value));
                isStateChanged = true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return;
        }

        public async Task<ImmutableList<PlayerDto>> GetAbovePlayer(Guid playerId, int count = Constants.AboveCount)
        {
            var playerRank = await GetPlayerRank(playerId);
            var start = playerRank - count < 0 ? 0 : playerRank - count;
            var aboveCount = start == 0 ? playerRank + 1 : count + 1;
            var result = _game.State.Leaderboard
                                    .GetRange(start, aboveCount)
                                    .Select((element, index) => new PlayerDto
                                    {
                                        Id = element.Key,
                                        Rank = index + 1 + start,
                                        Score = element.Value
                                    })
                                    .ToImmutableList();
            return result;
        }

        public async Task<ImmutableList<PlayerDto>> GetBelowPlayer(Guid playerId, int count = Constants.BeloweCount)
        {
            var playerRank = await GetPlayerRank(playerId);
            var start = playerRank;
            int belowCount = playerRank + count >= _game.State.Leaderboard.Count() ?
                                _game.State.Leaderboard.Count() - playerRank :
                                count + 1;
            var result = _game.State.Leaderboard
                                    .GetRange(start, belowCount)
                                    .Select((player, index) => new PlayerDto
                                    {
                                        Rank = index + 1 + start,
                                        Id = player.Key,
                                        Score = player.Value
                                    })
                                    .ToImmutableList();
            return result;
        }

        public Task<int> GetPlayerRank(Guid playerId)
        {
            var rank = _game.State.Leaderboard.FindIndex(player => player.Key.Equals(playerId));
            return Task.FromResult(rank);
        }

        public Task<ImmutableList<PlayerDto>> GetTopPlayer(int count = Constants.TopCount)
        {
            var topCount = Math.Min(count, _game.State.Leaderboard.Count);
            var result = this._game.State.Leaderboard
                            .Take(topCount)
                            .Select((element, index) => new PlayerDto
                            {
                                Id = element.Key,
                                Rank = index + 1,
                                Score = element.Value
                            })
                            .ToImmutableList();
            return Task.FromResult(result);
        }

        public Task<Guid> Subscribe()
        {
            return Task.FromResult(stream.Guid);
        }

        public Task<ImmutableList<Guid>> GetJoinedPlayers()
        {
            var players = _game.State.Leaderboard.Select(p => p.Key).ToImmutableList();
            return Task.FromResult(players);
        }

        public async Task ReceiveReminder(string reminderName, TickStatus status)
        {
            await stream.OnNextAsync(isStateChanged);
        }

        public Task SummaryReport()
        {
            var gameId = this.GetPrimaryKey();
            _game.State.Leaderboard
                .Select((element, index) => new Summary
                {
                    GameId = this.GetPrimaryKey(),
                    PlayerId = element.Key,
                    Rank = index + 1,
                    Score = element.Value
                })
                .ToList()
                .ForEach(element =>
                {
                    var player_rank = _gameContext.Summaries
                                        .FirstOrDefault(s => s.GameId.Equals(element.GameId) &&
                                                                s.PlayerId.Equals(element.PlayerId));
                    if (player_rank != null)
                    {
                        player_rank.Rank = element.Rank;
                        player_rank.Score = element.Score;
                        _gameContext.Summaries.Update(player_rank);
                    }
                    else
                    {
                        _gameContext.Summaries.Add(new Summary
                        {
                            GameId = gameId,
                            PlayerId = element.PlayerId,
                            Rank = element.Rank,
                            Score = element.Score
                        });
                    }
                });
            _gameContext.SaveChanges();
            return Task.CompletedTask;
        }
    }
}