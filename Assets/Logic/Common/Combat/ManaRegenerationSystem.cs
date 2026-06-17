using ROMA2.Logic.Data;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Mathematics;

namespace ROMA2.Logic.Common.Combat
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct ManaRegenerationSystem : ISystem
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
                foreach ((MaxMana maxMana, RefRW<CurrentMana> currMana, ManaRegeneration regeneration) 
                         in SystemAPI.Query<MaxMana, RefRW<CurrentMana>, ManaRegeneration>())
                {
                    if (currMana.ValueRO.Value >= maxMana.Value) continue;

                    currMana.ValueRW.Value = math.min(maxMana.Value, currMana.ValueRO.Value + regeneration.Value);
                }

                _nextRegenTick.Add(_simulationTickRate);
            }
        }
    }
}