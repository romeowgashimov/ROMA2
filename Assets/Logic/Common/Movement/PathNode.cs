using Unity.Entities;

namespace ROMA2.Logic.Common.Movement
{
    [InternalBufferCapacity(0)]
    public struct PathNode : IBufferElementData
    {
        public int Index;
        public int Cost;
        public bool IsWalkable;
            
        public void SetIsWalkable(bool isWalkable) =>
            IsWalkable = isWalkable;
    }
}