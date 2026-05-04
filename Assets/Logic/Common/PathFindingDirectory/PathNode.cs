using Unity.Entities;

namespace Logic.Client
{
    [InternalBufferCapacity(0)]
    public struct PathNode : IBufferElementData
    {
        public int Index;
        public bool IsWalkable;
            
        public void SetIsWalkable(bool isWalkable) =>
            IsWalkable = isWalkable;
    }
}