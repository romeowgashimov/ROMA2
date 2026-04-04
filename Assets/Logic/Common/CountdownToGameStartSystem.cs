using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace Logic.Common
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial class CountdownToGameStartSystem : SystemBase
    {
        public Action<int> OnUpdateCountdownText;
        public Action OnCountdownEnd;

        protected override void OnCreate()
        {
            RequireForUpdate<NetworkTime>();
        }

        protected override void OnUpdate()
        {
            NetworkTime networkTime = SystemAPI.GetSingleton<NetworkTime>();
            if (!networkTime.IsFirstTimeFullyPredictingTick) return;
            NetworkTick currentTick = networkTime.ServerTick;

            EntityCommandBuffer ecb = new(Allocator.Temp);

            foreach ((GameStartTick gameStartTick, Entity entity) in SystemAPI
                         .Query<GameStartTick>()
                         .WithAll<Simulate>()
                         .WithEntityAccess())
            {
                if (currentTick.Equals(gameStartTick.Value) || currentTick.IsNewerThan(gameStartTick.Value))
                {
                    Entity gamePlayingEntity = ecb.CreateEntity();
                    ecb.SetName(gamePlayingEntity, "GamePlayingEntity");
                    ecb.AddComponent<GameplayingTag>(gamePlayingEntity);
                    
                    ecb.DestroyEntity(entity);
                    OnCountdownEnd?.Invoke();
                }
                else
                {
                    uint ticksToStart = gameStartTick.Value.TickIndexForValidTick - currentTick.TickIndexForValidTick;
                    int simulationTickRate = NetCodeConfig.Global.ClientServerTickRate.SimulationTickRate;
                    int secondsToStart = (int)math.ceil((float)ticksToStart / simulationTickRate);
                    OnUpdateCountdownText?.Invoke(secondsToStart);
                }
            }
            
            ecb.Playback(EntityManager);
        }
    }
}