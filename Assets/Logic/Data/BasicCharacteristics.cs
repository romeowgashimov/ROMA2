using Unity.Entities;

namespace ROMA2.Logic.Data
{
    public struct AttackSpeed : IComponentData
    {
        public float Value;
    }

    public struct PhysicalPower : IComponentData
    {
        public int Value;
    }
    
    public struct MagicalPower : IComponentData
    {
        public int Value;
    }

    public struct PhysicalArmor : IComponentData
    {
        public int Value;
    }

    public struct MagicalArmor : IComponentData
    {
        public int Value;
    }

    public struct HealthRegeneration : IComponentData
    {
        public float Value;
    }
    
    public struct ManaRegeneration : IComponentData
    {
        public float Value;
    }
    
    public struct CurrentMana : IComponentData
    {
        public int Value;
    }
    
    public struct MaxMana : IComponentData
    {
        public int Value;
    }

    public enum DamageType : byte
    {
        Physical,
        Magical,
        True,
        All,
        None
    }
    
    // Изменение исходящего урона от персонажа
    public struct OutgoingDamageChangerElement : IBufferElementData // Буфер, так как может быть множество эффектов 
    {
        public float Value; // Количество урона
        public bool IsPercentage; // Процент или номинальный урон
        public DamageType Type; // Тип урона
    }

    // Изменение входящего урона персонажа
    public struct IncomingDamageChangerElement : IBufferElementData  
    {
        public float Value; // Количество урона
        public bool IsPercentage; // Процент или номинальный урон
        public DamageType Type; // Тип урона
    }

    public struct AttackCommand : IComponentData
    {
        // Не атака или умение, а владелец сущности, нанёсшей урон
        public Entity Owner;
        public Entity Receiver;
        public float PhysicalDamage;
        public float MagicalDamage;
        public float TrueDamage;
    }

    // Маркер, что команда обработалась
    public struct ProcessedAttackCommand : IComponentData, IEnableableComponent { }
    
    // Маркер, что команда базовой атаки 
    // ПО УМОЛЧАНИЮ КОМАНДА ВСЕГДА КОМАНДА БАЗОВОЙ АТАКИ!!!
    public struct BasicAttackCommand : IComponentData, IEnableableComponent { }

    public struct NewAttackCommand : IComponentData, IEnableableComponent { }

    public struct AttackCommandElement : IBufferElementData
    {
        public Entity Value;
    }

    public struct AttackCommandContainerTag : IComponentData
    {
        public Entity Value;
    }
    
    public struct DefaultDamage : IComponentData
    {
        // Допускаем смешанный урон, поэтому такой костыль
        public float PhysicalDamage;
        public float MagicalDamage;
        public float TrueDamage;
    }

    public struct SkillShotAbility : IComponentData
    {
        public int PhysicalPercentage;
    }

    public struct DeathSphereAbility : IComponentData
    {
        public int MagicalPercentage;
    }

    // Чтобы не лазить в лукапы сразу передаём всю инфу о владельце
    public struct CombineCharsComponent : IComponentData
    {
        public int PhysicalPower;
        public int MagicalPower;
        public int PhysicalArmor;
        public int MagicalArmor;
        public int MaxHP;
        public int CurrentHP;
        // public ...
    }
    
    public struct RangedAttack : IComponentData { }
}