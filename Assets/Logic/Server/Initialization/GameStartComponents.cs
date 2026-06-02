using Unity.Entities;
using Unity.Mathematics;

namespace ROMA2.Logic.Server.Initialization
{
    public struct GameStartProperties : IComponentData
    {
        public int MaxPlayersPerTeam;
        public int MinPlayersToStartGame;
        public int CountdownTime;
    }

    public struct TeamPlayerCounter : IComponentData
    {
        public int BlueTeamPlayers;
        public int RedTeamPlayers;
        public int InitializedPlayers;
        public int UninitializedPlayers;
    }

    public struct InitializedPlayer : IEnableableComponent, IComponentData { }

    public struct SpawnOffset : IBufferElementData
    {
        public float3 Value;
    }
}