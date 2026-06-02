using Unity.Entities;
using Unity.NetCode;

namespace ROMA2.Logic.Data
{
    public struct GameplayingTag : IComponentData { }

    public struct GameStartTick : IComponentData
    {
        public NetworkTick Value;
    }
    
    public struct GameOverTag : IComponentData { }

    public struct WinningTeam : IComponentData
    {
        [GhostField] public TeamType Value;
    }
}