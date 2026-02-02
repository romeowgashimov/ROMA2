using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace Logic.Common
{
    [GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
    public struct NeedPath : IEnableableComponent, IComponentData { }

    public struct LastProcessedClick : IComponentData
    {
        public int Value;
    }
    
    [InternalBufferCapacity(4)]
    public struct PathPosition : IBufferElementData
    {
        public int2 Value;
    }
    
    public struct FollowPathIndex : IComponentData
    {
        public int Value;
    }
}