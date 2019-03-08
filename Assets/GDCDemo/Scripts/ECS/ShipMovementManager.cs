using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine.UI;
using Random = UnityEngine.Random;


public class ShipMovementManager : MonoBehaviour
{
    
    // debug text fields
    public Text EntityTextDisplay;
    private int numberOfShips = 0;
    
    public int ShipsToSpawn = 1000;

    public Mesh shipMesh;
    public Material shipMaterial;

    public List<GameObject> ShipPrefabs;

    public static EntityArchetype ShipArch;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Initialize()
    {
        var entityManager = World.Active.GetOrCreateManager<EntityManager>();

        ShipArch = entityManager.CreateArchetype(
            typeof(Position), typeof(Rotation),
            typeof(MeshRenderer)
            
            //typeof(MeshInstanceRenderer), typeof(TransformMatrix)
        );
    }

    void Start ()
    {
        SpawnShips();
    }
	
    void Update ()
    {
        if(Input.GetKeyDown(KeyCode.Space))
        {
            SpawnShips();
        }
    }

    void SpawnShips()
    {
/*
        var entityManager = World.Active.GetOrCreateManager<EntityManager>();
        
        NativeArray<Entity> ships = new NativeArray<Entity>(ShipsToSpawn, Allocator.Temp);
        entityManager.CreateEntity(ShipArch, ships);
*/

        for (int i = 0; i < ShipsToSpawn; i++)
        {
            for (int j = 0; j < ShipPrefabs.Count; j++)
            {
                var entityManager = World.Active.GetOrCreateManager<EntityManager>();
                Entity ship = entityManager.Instantiate(ShipPrefabs[j]);
                entityManager.AddComponentData(ship, new Position {Value = RandomPosition()});
                entityManager.AddComponentData(ship, new ShipMovementData {movementSpeed = RandomFallSpeed()});
            }
        }

        /*
        manager.SetComponentData(bullet, new Position { Value = gunBarrel.position });
        manager.SetComponentData(bullet, new Rotation { Value = Quaternion.Euler(rotation) });
        for (int i = 0; i < ShipsToSpawn; i++)
        {
            entityManager.SetComponentData(ships[i], new Position{ Value = RandomPosition()});
            //entityManager.SetSharedComponentData(snowFlakes[i], new MeshInstanceRenderer{ mesh = SnowflakeMesh, material = SnowflakeMat});
            entityManager.SetComponentData(ships[i], new Renderer{mesh = shipMesh, material = shipMaterial});
            entityManager.AddComponentData(ships[i], new ShipMovementData{movementSpeed = RandomFallSpeed()});

        }
        ships.Dispose();
*/
        // update UI
        numberOfShips += ShipsToSpawn;
        EntityTextDisplay.text = numberOfShips.ToString();
    }


    // Random Value functions
    float3 RandomPosition()
    {
        return new float3(Random.Range(0.1f, 1.0f), Random.Range(0.1f, 1.0f), Random.Range(0.1f, 1.0f));
    }

    float RandomFallSpeed()
    {
        return Random.Range(0.1f, 5.0f);
    }

    float RandomRotateSpeed()
    {
        return 1.0f;
    }
}
