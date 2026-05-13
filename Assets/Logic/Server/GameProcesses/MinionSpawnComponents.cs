using System;
using Unity.Entities;

namespace Logic.Server
{
    public struct MinionSpawnProperties : IComponentData
    {
        public float TimeBetweenWaves;
        public float TimeBetweenMinions;
        public int CountToSpawnInWave;
    }

    public struct MinionSpawnTimers : IComponentData
    {
        public float TimeToNextWave;
        public float TimeToNextMinion;
        public int CountSpawnedInWave;

        public bool ShouldSpawn => TimeToNextWave <= 0f && TimeToNextMinion <= 0f;
        
        public bool IsWaveSpawned(int countToSpawnInWave) => CountSpawnedInWave >= countToSpawnInWave;

        public void DecrementTimers(float deltaTime)
        {
            if (TimeToNextWave >= 0f)
            {
                TimeToNextWave -= deltaTime;
                return;
            }

            if (TimeToNextMinion >= 0f)
            {
                TimeToNextMinion -= deltaTime;
            }
        }

        public void PlusCountSpawnedInWave() =>
            CountSpawnedInWave++;
        
        
        public void ResetWaveTimer(float timeBetweenWaves) => 
            TimeToNextWave = timeBetweenWaves;
        
        public void ResetMinionTimer(float timeBetweenMinions) => 
            TimeToNextMinion = timeBetweenMinions;
        
        public void ResetSpawnCounter() => 
            CountSpawnedInWave = 0;
    }

    public struct MinionPathContainer : IComponentData
    {
        public Entity TopLane;
        public Entity MidLane;
        public Entity BotLane;

        public int Length => 2;
        
        public Entity this[int index] => index switch
            {
                0 => TopLane,
                1 => MidLane,
                2 => BotLane,
                _ => Entity.Null
            };
    }
}