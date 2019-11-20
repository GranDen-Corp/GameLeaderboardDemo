using Orleans;
using System;
using System.Threading.Tasks;

namespace Interfaces
{
    public interface IChannelGrain : IGrainWithStringKey
    {
        Task<Guid> Join(string username);
        Task Broadcast(string username, string message);
    }
}
