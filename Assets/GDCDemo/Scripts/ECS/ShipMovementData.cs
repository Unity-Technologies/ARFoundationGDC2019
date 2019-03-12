using System;
using UnityEngine;
using Unity.Entities;

[Serializable]
public struct MovementData : IComponentData
{
    public float Value;
}

public class ShipMovementData : ComponentDataWrapper<MovementData> {}
