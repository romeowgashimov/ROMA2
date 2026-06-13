using Unity.Entities;

namespace ROMA2.Logic.Common.Abilities
{
    public struct AbilityCommand : IComponentData
    {
        public Entity Owner;
        public bool NeedToConfirmAbilities;
        public int AbilityIndex;
        public Entity Prefab;
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
    
    public struct AimingTag : IComponentData, IEnableableComponent { }
    
    public struct DefaultInstAbilityCommand : IComponentData, IEnableableComponent
    {
        public bool NeedToConfirmAbilities;
        public int AbilityIndex;
        public Entity Prefab;
    }
}