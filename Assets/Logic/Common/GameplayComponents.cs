using Unity.Entities;
using Unity.NetCode;

namespace Logic.Common
{
    public struct GameplayingTag : IComponentData { }

    public struct GameStartTick : IComponentData
    {
        public NetworkTick Value;
    }
}