using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace Logic.Common
{
    public struct NeedPath : IEnableableComponent, IComponentData { }

    public struct RegisterNeedPathComponent : IComponentData
    {
        [GhostField] public bool Value;
    }
    
    [GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
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