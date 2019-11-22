using Common;
using Common.Dto;
using Interfaces;
using Orleans;
using Orleans.Providers;
using Orleans.Streams;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Grains
{
    public class GameState
    {
        public List<KeyValuePair<Guid, int>> Leaderboard { get; set; } = new List<KeyValuePair<Guid, int>>();
    }
    [StorageProvider(ProviderName = Constants.OrleansDataStorageProvider)]
    public class GameGrain : Grain<GameState>, IGameGrain
    {
        private IAsyncStream<bool> stream;
        public override Task OnActivateAsync()
        {
            var streamProvider = GetStreamProvider(Constants.OrleansStreamProvider);
            stream = streamProvider.GetStream<bool>(this.GetPrimaryKey(), Constants.OrleansStreamNameSpace);
            return base.OnActivateAsync();
        }
        public async Task AddPoint(Guid playerId, int point)
        {
            bool isChanged = false;
            try
            {
                var rank = await this.GetPlayerRank(playerId);
                if (rank < 0)
                {
                    this.State.Leaderboard.Add(new KeyValuePair<Guid, int>(playerId, point));
                }
                else
                {
                    var newPoint = this.State.Leaderboard[rank].Value + point;
                    this.State.Leaderboard[rank] = new KeyValuePair<Guid, int>(playerId, newPoint);
                }
                this.State.Leaderboard.Sort((x, y) => -1 * x.Value.CompareTo(y.Value));
                await this.WriteStateAsync();
                isChanged = true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                await stream.OnNextAsync(isChanged);
            }
            return;
        }

        public async Task<ImmutableList<PlayerDto>> GetAbovePlayer(Guid playerId, int count = Constants.AboveCount)
        {
            var playerRank = await GetPlayerRank(playerId);
            var start = playerRank - count < 0 ? 0 : playerRank - count;
            var aboveCount = start == 0 ? playerRank + 1 : count + 1;
            var result = this.State.Leaderboard
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
            int belowCount = (playerRank + count >= this.State.Leaderboard.Count() ?
                                this.State.Leaderboard.Count() - playerRank :
                                count + 1);
            var result = this.State.Leaderboard
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
            var rank = this.State.Leaderboard.FindIndex(player => player.Key.Equals(playerId));
            return Task.FromResult(rank);
        }

        public Task<ImmutableList<PlayerDto>> GetTopPlayer(int count = Constants.TopCount)
        {
            var topCount = Math.Min(count, this.State.Leaderboard.Count);
            var result = this.State.Leaderboard
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
            return Task.FromResult(this.stream.Guid);
        }

        public Task<ImmutableList<Guid>> GetJoinedPlayers()
        {
            var players = this.State.Leaderboard.Select(p => p.Key).ToImmutableList();
            return Task.FromResult(players);
        }
    }
}