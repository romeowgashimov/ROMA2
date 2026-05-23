using Logic.Server;
using Unity.Entities;
using UnityEngine;

namespace ROMA2.Logic.Server.Initialization
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct GameStartPropertiesInitializationSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameStartProperties>();
        }

        public void OnUpdate(ref SystemState state)
        {
            RefRW<GameStartProperties> propertiesRef = SystemAPI.GetSingletonRW<GameStartProperties>();
            propertiesRef.ValueRW.MinPlayersToStartGame = GameStartSettings.MinPlayersToStart;
            state.Enabled = false;
        }
    }
}