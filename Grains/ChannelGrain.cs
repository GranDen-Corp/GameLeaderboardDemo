using Common;
using Interfaces;
using Orleans;
using Orleans.Streams;
using System;
using System.Threading.Tasks;

namespace Grains
{
    public class ChannelGrain : Grain, IChannelGrain
    {
        private IAsyncStream<string> stream;
        public override Task OnActivateAsync()
        {
            var streamProvider = GetStreamProvider(Constants.OrleansStreamProvider);
            stream = streamProvider.GetStream<string>(Guid.NewGuid(), Constants.OrleansStreamNameSpace);
            return base.OnActivateAsync();
        }
        public async Task Broadcast(string username, string message)
        {
            await stream.OnNextAsync($"{username} said: {message}");
        }

        public async Task<Guid> Join(string username)
        {
            await stream.OnNextAsync($"{username} join the broadcast channel.");
            return stream.Guid;
        }
    }
}
