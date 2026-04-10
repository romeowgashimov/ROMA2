using Logic.Common;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Serialization;
using static Unity.Scenes.SceneSystem;

namespace Logic.Server
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct ServerLoadCharactersSystem : ISystem
    {
        private EntityQuery _query;
        
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ChampionPrefabElement>();
            state.RequireForUpdate<PendingSpawn>();

            _query = SystemAPI.QueryBuilder()
                .WithAll<PendingSpawn>()
                .Build();
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = new(Allocator.Temp);
            Entity heroesContainer = SystemAPI.GetSingletonEntity<ChampionPrefabElement>();
            DynamicBuffer<ChampionPrefabElement> heroesPrefabs = SystemAPI.GetBuffer<ChampionPrefabElement>(heroesContainer);
            using NativeArray<Entity> entities = _query.ToEntityArray(Allocator.Temp);
            using NativeArray<PendingSpawn> pending = _query.ToComponentDataArray<PendingSpawn>(Allocator.Temp);
            
            for (int i = 0; i < entities.Length; i++)
            {
                Entity loadRequest = LoadPrefabAsync(state.WorldUnmanaged, heroesPrefabs[pending[i].CharacterId].Value);
                ecb.AddComponent<LoadedEntity>(entities[i], new() { Value = loadRequest });
            }
            ecb.Playback(state.EntityManager);
        }
    }
}