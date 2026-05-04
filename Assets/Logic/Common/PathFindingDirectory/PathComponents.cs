using Unity.Entities;
using Unity.Mathematics;

namespace Logic.Common
{
    public struct PathFindingRequest : IEnableableComponent, IComponentData { }
 
    // Может быть проблема с плавностью ходьбы именно из-за синхронизации
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