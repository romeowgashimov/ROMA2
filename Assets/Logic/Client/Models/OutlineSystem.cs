using ROMA2.Logic.Client.Data;
using ROMA2.Logic.Data;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using static Unity.Entities.Entity;
using static Unity.Entities.SystemAPI;

namespace ROMA2.Logic.Client.Models
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct OutlineSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkId>();
        }

        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new OutlineJob
            {
                OutlineWidthLookup = GetComponentLookup<OutlineWidth>(),
                OutlineColorLookup = GetComponentLookup<OutlineColor>(),
                OutlinedEntityLookup = GetComponentLookup<OutlineEntityContainer>(),
                TeamLookup = GetComponentLookup<Team>(),
            }.Schedule(state.Dependency);
        }
    }

    [WithAll(typeof(GhostOwnerIsLocal))]
    public partial struct OutlineJob : IJobEntity
    {
        private const float OUTLINE_WIDTH = 1.12F;
        private static readonly Color OUTLINE_COLOR_RED = new(1f, 0.3f, 0.3f, 1f);
        private static readonly Color OUTLINE_COLOR_BLUE = new(0.3f, 0.7f, 1f, 1f);
        private static readonly Color OUTLINE_COLOR_DEFAULT = new(1f, 0.7f, 1f, 1f);
            
        public ComponentLookup<OutlineWidth> OutlineWidthLookup;
        public ComponentLookup<OutlineColor> OutlineColorLookup;
        [ReadOnly] public ComponentLookup<OutlineEntityContainer> OutlinedEntityLookup;
        [ReadOnly] public ComponentLookup<Team> TeamLookup;

        private void Execute(
            in SelectedEntity selectedEntity, 
            in Team team, 
            ref LastOutlinedEntity outlinedEntity)
        {
            Entity selected = selectedEntity.Value;
            Entity outlined = outlinedEntity.Value;
            if (selected != outlined && outlined != Null)
            {
                outlinedEntity.Value = Null;
                if (!OutlinedEntityLookup.HasComponent(outlined)) return;
                Entity oldOutline = OutlinedEntityLookup[outlined].Value;
                OutlineWidthLookup[oldOutline] = new() { Value = 1f };
            }
            
            // Из-за долбанной логики рейкаста у меня всегда selected != Null B)
            if (selected == Null) return;
            if (selected == outlinedEntity.Value) return;

            if (!OutlinedEntityLookup.HasComponent(selected)) return;
            Entity newOutline = OutlinedEntityLookup[selected].Value;
            if (!OutlineWidthLookup.HasComponent(newOutline) || !OutlineColorLookup.HasComponent(newOutline)) 
                return;

            OutlineWidthLookup[newOutline] = new() { Value = OUTLINE_WIDTH };
            
            Color targetColor;
            if (TeamLookup.TryGetComponent(selected, out Team targetTeam))
            {
                targetColor = team.Value == targetTeam.Value 
                    ? OUTLINE_COLOR_BLUE
                    : OUTLINE_COLOR_RED;
            }
            else targetColor = OUTLINE_COLOR_DEFAULT;
            
            OutlineColorLookup[newOutline] = new() { Value = targetColor };
            
            outlinedEntity.Value = selected;
        }
    }
}