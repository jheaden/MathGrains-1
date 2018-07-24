using System.Threading.Tasks;
using GrainInterfaces;
using Orleans;

namespace Grains
{
    public class SquareGrain : Grain, ISquareGrain
    {
        
        public Task<decimal> SquareMe(decimal input)
        {
            return Task.FromResult(input * input);
        }

    }

}
