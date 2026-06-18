using System;
using ROMA2.Logic.Client.Data;
using ROMA2.Logic.Data;
using Unity.Entities;
using Unity.NetCode;
using static Unity.Mathematics.math;

namespace ROMA2.Logic.Client.Models
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial class ChangeCharsEventSystem : SystemBase
    {
        public event Action<float, int> OnHealthChanged;
        public event Action<float, int> OnManaChanged;

        protected override void OnUpdate()
        {
            // .WithAll<GhostOwnerIsLocal>() для того, чтобы не выхватывать данные других персонажей 
            foreach ((CurrentHealthPoints currHP, MaxHealthPoints maxHP, RefRW<UpdatedHP4UI> updated) in SystemAPI
                         .Query<CurrentHealthPoints, MaxHealthPoints, RefRW<UpdatedHP4UI>>()
                         .WithAll<GhostOwnerIsLocal>())
            {
                if (abs(currHP.Value - updated.ValueRO.Current) < 0.01f 
                    && abs(maxHP.Value - updated.ValueRO.Max) < 1) continue;

                updated.ValueRW.Current = currHP.Value;
                updated.ValueRW.Max = maxHP.Value;
                
                OnHealthChanged?.Invoke(currHP.Value, maxHP.Value);
            }
            
            foreach ((CurrentMana currentMana, MaxMana maxMana, RefRW<UpdatedMana4UI> updated) in SystemAPI
                         .Query<CurrentMana, MaxMana, RefRW<UpdatedMana4UI>>()
                         .WithAll<GhostOwnerIsLocal>())
            {
                if (abs(currentMana.Value - updated.ValueRO.Current) < 0.01f 
                    && abs(maxMana.Value - updated.ValueRO.Max) < 1) continue;

                updated.ValueRW.Current = currentMana.Value;
                updated.ValueRW.Max = maxMana.Value;
                
                OnManaChanged?.Invoke(currentMana.Value, maxMana.Value);
            }
        }

        protected override void OnDestroy()
        {
            OnHealthChanged = null;
            OnManaChanged = null;
        }
    }
}