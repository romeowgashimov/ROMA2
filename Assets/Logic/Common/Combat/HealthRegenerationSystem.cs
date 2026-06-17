using ROMA2.Logic.Data;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Mathematics;

namespace ROMA2.Logic.Common.Combat
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct HealthRegenerationSystem : ISystem
    {
        private uint _simulationTickRate;
        private NetworkTick _nextRegenTick;

        public void OnCreate(ref SystemState state)
        {
            _simulationTickRate = (uint)NetCodeConfig.Global.ClientServerTickRate.SimulationTickRate;
            state.RequireForUpdate<NetworkTime>();
            
            // Инициализируем начальный тик как невалидный
            _nextRegenTick = NetworkTick.Invalid;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            NetworkTime networkTime = SystemAPI.GetSingleton<NetworkTime>();
            NetworkTick currentTick = networkTime.ServerTick;

            // Если это первый запуск, планируем следующий тик регенерации через 1 секунду
            if (!_nextRegenTick.IsValid)
            {
                _nextRegenTick = currentTick;
                _nextRegenTick.Add(_simulationTickRate);
                return;
            }

            if (currentTick.IsNewerThan(_nextRegenTick) || currentTick.Equals(_nextRegenTick))
            {
                foreach ((MaxHealthPoints maxHP, RefRW<CurrentHealthPoints> currHP, HealthRegeneration regeneration) 
                         in SystemAPI.Query<MaxHealthPoints, RefRW<CurrentHealthPoints>, HealthRegeneration>())
                {
                    if (currHP.ValueRO.Value >= maxHP.Value) continue;

                    currHP.ValueRW.Value = math.min(maxHP.Value, currHP.ValueRO.Value + regeneration.Value);
                }

                _nextRegenTick.Add(_simulationTickRate);
            }
        }
    }
}
