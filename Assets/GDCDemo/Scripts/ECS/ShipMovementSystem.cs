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
            float3 m_Value = position.Value;

            m_Value += deltaTime * speed.Value * math.forward(rotation.Value);

            if (m_Value.z < bottomBound)
                m_Value.z = topBound;

            position.Value = m_Value;
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        MovementJob moveJob = new MovementJob
        {
            topBound = ShipMovementManager.k_shipManager.WorldResetTop,
            bottomBound = ShipMovementManager.k_shipManager.WorldResetBottom,
            deltaTime = Time.deltaTime
        };
      
        JobHandle moveHandle = moveJob.Schedule(this, inputDeps);

        return moveHandle;
    }
}

