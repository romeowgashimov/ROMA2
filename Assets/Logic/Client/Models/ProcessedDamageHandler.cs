using ROMA2.Logic.Client.Data;
using ROMA2.Logic.Data;
using Unity.Entities;
using Unity.NetCode;

namespace ROMA2.Logic.Client.Models
{
    // Интересно, если я забилжу сервер, эта система не попадёт в билд. Не возникнет ли из-за этого проблем?
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct ProcessedDamageHandler : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkTime>();
        }

        public void OnUpdate(ref SystemState state)
        {
            // Получаем сетевое время для текущего обрабатываемого мира
            NetworkTime networkTime = SystemAPI.GetSingleton<NetworkTime>();

            // КРИТИЧЕСКИ ВАЖНО: Выполняем логику ТОЛЬКО для нового тика.
            // При откатах (Rollback) это свойство будет равно false.
            if (!networkTime.IsFirstPredictionTick) return;

            EntityCommandBuffer ecb = SystemAPI
                .GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach ((DynamicBuffer<ProcessedDamageElement> processedDamages, Entity entity) in SystemAPI
                    .Query<DynamicBuffer<ProcessedDamageElement>>()
                    .WithAll<GhostOwnerIsLocal, CachedDamageElement>()
                    .WithEntityAccess())
            {
                if (processedDamages.IsEmpty) continue;

                foreach(ProcessedDamageElement element in processedDamages)
                {
                    ecb.AppendToBuffer<CachedDamageElement>(entity, new()
                    {
                        Receiver = element.Receiver,
                        PhysicalDamage = element.PhysicalDamage,
                        MagicalDamage = element.MagicalDamage,
                        TrueDamage = element.TrueDamage,
                        AbilityIndex = element.AbilityIndex
                    });
                }
            }
        }
    }
}