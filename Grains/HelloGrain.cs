using Interfaces;
using Orleans;
using System.Threading.Tasks;

namespace Grains
{
    public class HelloGrain : Grain, IHelloGrain
    {
        public Task<string> HelloWorld(string name)
        {
            return Task.FromResult($"Hello {name}!");
        }
    }
}
