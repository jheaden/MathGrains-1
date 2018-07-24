using System.Threading.Tasks;
using GrainInterfaces;
using Orleans;

namespace Grains
{
    public class CubeGrain : Grain, ICubeGrain
    {

        public Task<decimal> CubeMe(decimal input)
        {
            return Task.FromResult(input * input * input);
        }

    }

}
