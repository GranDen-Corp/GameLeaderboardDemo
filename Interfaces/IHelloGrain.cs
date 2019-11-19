using Orleans;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Interfaces
{
    public interface IHelloGrain: IGrainWithGuidKey
    {
        Task<string> HelloWorld(string name);
    }
}
