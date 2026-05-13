using Logic.Common;
using Unity.Entities;
using Unity.NetCode;

namespace Logic.Server
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct ServerChoiceChampionRequestSystem : ISystem
    {
        private EntityQuery _query;
        
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<GameStartProperties>();

            _query = SystemAPI.QueryBuilder()
                .WithAll<NetworkId>()
                .WithNone<NetworkStreamInGame, InitializedPlayer>()
                .Build();
            
            state.RequireForUpdate(_query);
        }

        public void OnUpdate(ref SystemState state)
        {
            if (_query.CalculateEntityCount() == 0) return;
            
            EntityCommandBuffer ecb = SystemAPI
                .GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            
            Entity gamePropertiesEntity = SystemAPI.GetSingletonEntity<GameStartProperties>();
            GameStartProperties gameStartProperties = SystemAPI.GetComponent<GameStartProperties>(gamePropertiesEntity);
            TeamPlayerCounter teamPlayerCounter = SystemAPI.GetComponent<TeamPlayerCounter>(gamePropertiesEntity);
            
            state.Dependency = new ChoiceChampionRequestJob
            {
                ECB = ecb.AsParallelWriter(),
                TeamPlayerCounter = teamPlayerCounter,
                GameStartProperties = gameStartProperties,
                GamePropertiesEntity = gamePropertiesEntity,
                HasGameplayingTag = SystemAPI.HasSingleton<GameplayingTag>()
            }.ScheduleParallel(_query, state.Dependency);
        }
    }
    
    public partial struct ChoiceChampionRequestJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        public TeamPlayerCounter TeamPlayerCounter;
        public GameStartProperties GameStartProperties;
        public Entity GamePropertiesEntity;
        public bool HasGameplayingTag;
        
        public void Execute([ChunkIndexInQuery] int key, Entity connectionEntity)
        {
            InitializedPlayer initializedPlayer = new();
            ECB.AddComponent(key, connectionEntity, initializedPlayer);
            ECB.SetComponentEnabled<InitializedPlayer>(key, connectionEntity, true);
                
            TeamPlayerCounter.UninitializedPlayers++;
                
            int playersRemainingToStart =
                GameStartProperties.MinPlayersToStartGame - TeamPlayerCounter.UninitializedPlayers;

            Entity choiceChampionRpc = ECB.CreateEntity(key);
            if (playersRemainingToStart <= 0 && !HasGameplayingTag)
                ECB.AddComponent(key, choiceChampionRpc, new ChoiceChampionRpc());
            else
                ECB.AddComponent(key, choiceChampionRpc,
                    new PlayersRemainingToStart { Value = playersRemainingToStart });

            ECB.AddComponent<SendRpcCommandRequest>(key, choiceChampionRpc);
            ECB.SetComponent(key, GamePropertiesEntity, TeamPlayerCounter);
        }
    }
}