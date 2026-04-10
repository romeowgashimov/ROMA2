using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using static Unity.Scenes.SceneSystem;

namespace Logic.Common
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct ClientLoadCharactersSystem : ISystem
    {
        private EntityQuery _query;
        
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ChampionPrefabElement>();
            state.RequireForUpdate<LoadCharacterRequest>();
            
            _query = SystemAPI.QueryBuilder()
                .WithAll<LoadCharacterRequest, ReceiveRpcCommandRequest>()
                .Build();
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = new(Allocator.Temp);
            Entity heroesContainer = SystemAPI.GetSingletonEntity<ChampionPrefabElement>();
            DynamicBuffer<ChampionPrefabElement> heroesPrefabs = SystemAPI.GetBuffer<ChampionPrefabElement>(heroesContainer);
            using NativeArray<Entity> requestEntities = _query.ToEntityArray(Allocator.Temp);
            using NativeArray<LoadCharacterRequest> loadRequests = 
                _query.ToComponentDataArray<LoadCharacterRequest>(Allocator.Temp);
            
            for (int i = 0; i < requestEntities.Length; i++)
            {
                ecb.DestroyEntity(requestEntities[i]);
                
                LoadPrefabAsync(state.WorldUnmanaged, heroesPrefabs[(int)loadRequests[i].CharacterId].Value);
            }
            ecb.Playback(state.EntityManager);
        }
    }
}