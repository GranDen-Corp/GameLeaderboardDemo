using Common;
using Common.Dto;
using Common.Models;
using Orleans;
using System;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Interfaces
{
    public interface IGameGrain : IGrainWithGuidKey
    {
        Task<Guid> Subscribe();
        Task AddPoint(Guid playerId, int point);
        Task<ImmutableList<Guid>> GetJoinedPlayers();
        Task<int> GetPlayerRank(Guid playerId);
        Task<ImmutableList<PlayerDto>> GetTopPlayer(int topCount = Constants.TopCount);
        Task<ImmutableList<PlayerDto>> GetAbovePlayer(Guid playerId, int aboveCount = Constants.AboveCount);
        Task<ImmutableList<PlayerDto>> GetBelowPlayer(Guid playerId, int belowCount = Constants.BeloweCount);
        Task SummaryReport();
    }
}
