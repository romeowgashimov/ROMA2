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
    }
        
    [GhostComponent(PrefabType = GhostPrefabType.AllPredicted, OwnerSendType = SendToOwnerType.SendToNonOwner)]
    public struct DamageThisTick : ICommandData
    {
        public NetworkTick Tick { get; set; }
        public int Value;
    }

    public struct AbilityPrefabs : IComponentData
    {
        public Entity AoeAbility;
        public Entity SkillShotAbility;
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
        public uint AoeAbility;
        public uint SkillShotAbility;
        public int Length => 2;

        public uint this[int index] => index switch
        {
            0 => AoeAbility,
            1 => SkillShotAbility,
            _ => uint.MaxValue
        };
    }

    [GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
    public struct AbilityCooldownTargetTicks : ICommandData
    {
        public NetworkTick Tick { get; set; }
        public NetworkTick AoeAbility;
        public NetworkTick SkillShotAbility;

        public int Length => 2;

        public NetworkTick this[int index] => index switch
        {
            0 => AoeAbility,
            1 => SkillShotAbility,
            _ => NetworkTick.Invalid
        };
    }

        public struct AimSkillShotTag : IComponentData { }

    public struct AbilityMoveSpeed : IComponentData
    {
        public float Value;
    }

    public struct NpcTargetRadius : IComponentData
    {
        public float Value;
    }

    public struct NpcTargetEntity : IComponentData
    {
        [GhostField] public Entity Value;
    }

    public struct NpcAttackProperties : IComponentData
    {
        public float3 FirePointOffset;
        public uint CooldownTickCount;
        public Entity AttackPrefab;
    }

    public struct NpcAttackCooldown : ICommandData
    {
        public NetworkTick Tick { get; set; }
        public NetworkTick Value;
    }
}