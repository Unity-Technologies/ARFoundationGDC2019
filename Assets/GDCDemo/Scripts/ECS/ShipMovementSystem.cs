using Unity.Collections;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public class ShipMovementSystem : JobComponentSystem
{
    [BurstCompile]
    struct MovementJob : IJobProcessComponentData<Position, Rotation, MovementData>
    {
        public float topBound;
        public float bottomBound;
        public float deltaTime;

        public void Execute(ref Position position, [ReadOnly] ref Rotation rotation, [ReadOnly] ref MovementData speed)
        {
            float3 value = position.Value;

            value += deltaTime * speed.Value * math.forward(rotation.Value);

            if (value.z < bottomBound)
                value.z = topBound;

            position.Value = value;
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        MovementJob moveJob = new MovementJob
        {
            //topBound = ShipMovementManager.GM.topBound,
            //bottomBound = ShipMovementManager.GM.bottomBound,
            topBound = ShipMovementManager.GM.WorldResetTop,
            bottomBound = ShipMovementManager.GM.WorldResetBottom,
            deltaTime = Time.deltaTime
        };
      
        JobHandle moveHandle = moveJob.Schedule(this, inputDeps);

        return moveHandle;
    }
}

