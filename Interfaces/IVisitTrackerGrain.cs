using Orleans;
using System.Threading.Tasks;

namespace Interfaces
{
    public interface IVisitTrackerGrain : IGrainWithStringKey
    {
        Task<int> GetNumberOfVisits();
        Task Visit();
    }
}
