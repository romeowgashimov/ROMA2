using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace Logic.Common
{
    public struct MaxHealthPoints : IComponentData
    {
        [GhostField] public int Value;
    }
        
    public struct CurrentHealthPoints : IComponentData
    {
        [GhostField] public int Value;
    }

    [GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
    public struct DamageBufferElement : IBufferElementData
    {
        public int Value;
        public Entity DealingDamageEntity;
    }
        
    [GhostComponent(PrefabType = GhostPrefabType.AllPredicted, OwnerSendType = SendToOwnerType.SendToNonOwner)]
    public struct DamageThisTick : ICommandData
    {
        public NetworkTick Tick { get; set; }
        public int Value;
    }

    public struct AbilityCommands : IComponentData
    {
        public Entity Ability1;
        public Entity Ability2;
        public Entity Ability3;
        public Entity Ability4;
        
        public int Length => 4;
        
        public Entity this[int index] => index switch
        {
            0 => Ability1,
            1 => Ability2,
            2 => Ability3,
            3 => Ability4,
            _ => default
        };
    }

    public struct DestroyOnTimer : IComponentData
    {
        public float Value;
    }
    
    public struct DestroyAtTick : IComponentData
    {
        [GhostField] public NetworkTick Value;
    }
    
    public struct DestroyEntityTag : IComponentData { }

    public struct DamageOnTrigger : IComponentData
    {
        public int Value;
    }

    public struct AlreadyDamagedEntity : IBufferElementData
    {
        public Entity Value;
    }

    public struct AbilityCooldownTicks : IComponentData
    {
        public uint Ability1;
        public uint Ability2;
        public int Length => 2;

        public uint this[int index] => index switch
        {
            0 => Ability1,
            1 => Ability2,
            _ => uint.MaxValue
        };
    }

    [GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
    public struct AbilityCooldownTargetTicks : ICommandData
    {
        public NetworkTick Tick { get; set; }
        public NetworkTick Ability1;
        public NetworkTick Ability2;

        public readonly int GetAbilityCount() => 2;

        public readonly NetworkTick GetAbilityByTick(int index)
        {
            return index switch
            {
                0 => Ability1,
                1 => Ability2,
                _ => NetworkTick.Invalid
            };
        }

        public void SetAbilityByTick(int index, NetworkTick value)
        {
            switch (index)
            {
                case 0:
                    Ability1 = value;
                    break;
                case 1:
                    Ability2 = value;
                    break;
            }
        }
    }

    public struct AbilityMoveSpeed : IComponentData
    {
        public float Value;
    }

    public struct AttackRadius : IComponentData
    {
        public float Value;
    }
    
    public struct DetectionRadius : IComponentData
    {
        public float Value;
    }

    public struct TargetEntity : IComponentData
    {
        [GhostField] public Entity Value;
        [GhostField] public bool InAttackArea;
    }

    public struct LastTargetEntityPosition : IComponentData
    {
        public float3 Value;
    }
    
    public struct InAttackArea : IEnableableComponent, IComponentData { }
    
    public struct RangedAttackProperties : IComponentData
    {
        public float3 FirePointOffset;
        public uint CooldownTickCount;
        public Entity AttackPrefab;
    }

    public struct AttackCooldown : ICommandData
    {
        public NetworkTick Tick { get; set; }
        public NetworkTick Value;
    }

    public struct GameOverOnDestroyTag : IComponentData { }

    public struct BasicAttackTarget : IComponentData
    {
        public Entity Value;
    }

    public struct ReAggrRequest : IComponentData, IEnableableComponent
    {
        public Entity Target;
    }
}