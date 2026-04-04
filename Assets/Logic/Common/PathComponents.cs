using Unity.Entities;
using Unity.Mathematics;

namespace Logic.Common
{
    public struct NeedPath : IEnableableComponent, IComponentData { }
    
    [InternalBufferCapacity(4)]
    public struct PathPositionElement : IBufferElementData
    {
        public int2 Value;
    }
    
    public struct FollowPathIndex : IComponentData
    {
        public int Value;
    }
}