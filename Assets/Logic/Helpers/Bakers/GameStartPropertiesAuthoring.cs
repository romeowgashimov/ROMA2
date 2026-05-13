using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Logic.Server
{
    public class GameStartPropertiesAuthoring : MonoBehaviour
    {
        public int MaxPlayersPerTeam;
        public int MinPlayersToStartGame;
        public int CountdownTime;
        public Vector3[] SpawnOffsets;

        public class GameStartPropertiesBaker : Baker<GameStartPropertiesAuthoring>
        {
            public override void Bake(GameStartPropertiesAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new GameStartProperties
                {
                    MaxPlayersPerTeam = authoring.MaxPlayersPerTeam,
                    MinPlayersToStartGame = authoring.MinPlayersToStartGame,
                    CountdownTime = authoring.CountdownTime
                });
                AddComponent<TeamPlayerCounter>(entity);
                DynamicBuffer<SpawnOffset> spawnOffsets = AddBuffer<SpawnOffset>(entity);
                foreach (float3 spawnOffset in authoring.SpawnOffsets)
                    spawnOffsets.Add(new() { Value = spawnOffset });
            }
        }
    }
}