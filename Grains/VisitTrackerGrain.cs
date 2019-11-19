using Common;
using Interfaces;
using Orleans;
using Orleans.Providers;
using System;
using System.Threading.Tasks;

namespace Grains
{
    public class VisitTrackerState
    {
        public DateTime? FirstVisit { get; set; }
        public DateTime? LastVisit { get; set; }
        public int NumberOfVisits { get; set; }
    }
    [StorageProvider(ProviderName = Constants.OrleansMemoryProvider)]
    public class VisitTrackerGrain : Grain<VisitTrackerState>, IVisitTrackerGrain
    {
        /*
         * NOTE: Using Grain<T> to add storage to a grain is considered legacy
         * functionality: grain storage should be added using IPersistentState<T>
         * as previously described.
         */
        public Task<int> GetNumberOfVisits()
        {
            return Task.FromResult(State.NumberOfVisits);
        }

        public async Task Visit()
        {
            var now = DateTime.Now;
            if (!State.FirstVisit.HasValue)
            {
                State.FirstVisit = now;
            }
            State.NumberOfVisits++;
            State.LastVisit = now;
            await WriteStateAsync();
        }
    }
}
