using Unity.Entities;
using Unity.Mathematics;

namespace Logic.Common
{
    public struct PathFindingRequest : IEnableableComponent, IComponentData { }
    
    public struct PathFindingProcessing  : IEnableableComponent, IComponentData { }

    public struct CleanPath : IEnableableComponent, IComponentData { }

    public struct IncorrectPathProperties : IEnableableComponent, IComponentData { }
    
    [InternalBufferCapacity(4)]
    public struct PathPositionElement : IBufferElementData
    {
        public int2 Value;
    }
    
    public struct FollowPathProperties : IComponentData
    {
        public int Index;
    }
    
    public struct RegisteredObstacleInGrid : IComponentData { }
    
    public struct IgnoreRegistrationInGrid : IComponentData { }
    
    public struct RVOAgent : IComponentData { }
}