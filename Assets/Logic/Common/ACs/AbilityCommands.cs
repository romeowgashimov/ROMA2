using Unity.Entities;

namespace Logic.Common
{
    // Что делать с умениями, которые не имеют префабов, таргетные, допустим, или баффы на себя или союзника
    public interface IAbilityCommand : IComponentData, IEnableableComponent
    {
        bool NeedToConfirmAbilities { get; set; }
        int AbilityIndex { get; set; }
        Entity Prefab { get; set; }
    }
    
    public struct AbilityCommand : IAbilityCommand
    {
        public Entity Owner;
        public bool NeedToConfirmAbilities { get; set; }
        public int AbilityIndex { get; set; }
        public Entity Prefab { get; set; }
    }

    public struct ActivatedAbilitiesCommands : IComponentData
    {
        public bool Ability1;
        public bool Ability2;
        public bool Ability3;
        public bool Ability4;
        public int Length => 4;
        
        public bool this[int index]
        {
            get
            {
                return index switch
                {
                    0 => Ability1,
                    1 => Ability2,
                    2 => Ability3,
                    3 => Ability4,
                    _ => true
                };
            }
            set
            {
                switch (index)
                {
                    case 0:
                        Ability1 = value;
                        break;
                    case 1:
                        Ability2 = value;
                        break;
                    case 2:
                        Ability3 = value;
                        break;
                    case 3:
                        Ability4 = value;
                        break;
                }
            }
        }
    }

    public struct DrawAbilityUITag : IComponentData, IEnableableComponent { }
    
    public struct UpdateAbilityUITag : IComponentData, IEnableableComponent { }
    
    public struct AimingTag : IComponentData, IEnableableComponent { }

    public struct AoeAbilityCommand : IAbilityCommand
    {
        public bool NeedToConfirmAbilities { get; set; }
        public int AbilityIndex { get; set; }
        public Entity Prefab { get; set; }
    }
    
    public struct SkillShotAbilityCommand : IAbilityCommand
    {
        public bool NeedToConfirmAbilities { get; set; }
        public int AbilityIndex { get; set; }
        public Entity Prefab { get; set; }
    }
}