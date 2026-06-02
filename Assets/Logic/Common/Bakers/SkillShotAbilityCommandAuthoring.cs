using ROMA2.Logic.Common.Abilities;
using ROMA2.Logic.Common.Extensions;
using Unity.Entities;
using UnityEngine;

namespace ROMA2.Logic.Common.Bakers
{
    public class SkillShotAbilityCommandAuthoring : MonoBehaviour
    {
        public GameObject SkillShotAbilityPrefab;
        
        private class SkillShotAbilityBaker : Baker<SkillShotAbilityCommandAuthoring>
        {
            public override void Bake(SkillShotAbilityCommandAuthoring commandAuthoring)
            {
                this.BakeAbilityCommand<SkillShotAbilityCommandAuthoring,
                    SkillShotAbilityCommand>(commandAuthoring.SkillShotAbilityPrefab);
            }
        }
    }
}