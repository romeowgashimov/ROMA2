using Unity.Entities;
using Unity.Mathematics;

namespace Logic.Common
{
    public struct GridTag : IComponentData { }

    public struct GridSize : IComponentData
    {
        public int2 Value;
    }
}