using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public class ShipMovementSystem : JobComponentSystem
{
    public struct FlakeMoveJob : IJobProcessComponentData<Position, Rotation, ShipMovementData>
    {
        public float DeltaTime;
		
        public void Execute(ref Position pos, ref Rotation rot, ref ShipMovementData data)
        {
            pos.Value.z += data.movementSpeed * DeltaTime;
            //rot.Value = math.mul(math.normalize(rot.Value), math.axisAngle(math.up(), data.RotateSpeedValue * DeltaTime));
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var job = new FlakeMoveJob()
        {
            DeltaTime = Time.deltaTime
        };
        return job.Schedule(this, inputDeps);
    }
}
