using Unity.Entities;
using Unity.Mathematics;

namespace ROMA2.Logic.Navigation
{
    public struct GridTag : IComponentData { }

    public struct GridSize : IComponentData
    {
        public int2 Value;
    }
}