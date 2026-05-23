using ROMA2.Logic.Common.Bakers;
using ROMA2.Logic.Helpers.Bakers;
using Unity.Entities;
using UnityEngine;

namespace Logic.Common.ACs
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