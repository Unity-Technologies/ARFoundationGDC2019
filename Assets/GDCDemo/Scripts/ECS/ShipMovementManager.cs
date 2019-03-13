using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = UnityEngine.Random;

public class ShipMovementManager : MonoBehaviour
{
    #region GAME_MANAGER_STUFF

    //Boilerplat game manager stuff that is the same in each example
    public static ShipMovementManager GM;

    [Header("Simulation Settings")]
    public float topBound = 16.5f;
    public float bottomBound = -13.5f;
    public float leftBound = -23.5f;
    public float rightBound = 23.5f;

    public float WorldResetBottom;
    public float WorldResetTop;

    [Header("Enemy Settings")]
    public GameObject enemyShipPrefab;
    public float enemySpeed = 1f;

    public GameObject EnemyShip2;
    public GameObject EnemyShip3;

    [Header("Spawn Settings")]
    public int enemyShipCount = 1;
    public int enemyShipIncremement = 1;

    int count;

    int spawnedShip = 0;

    void Awake()
    {
        if (GM == null)
            GM = this;
        else if (GM != this)
            Destroy(gameObject);
    }
    #endregion

    EntityManager manager;

    [SerializeField] Transform RootObject;
    

    void Start()
    {
        //bottomBound = RootObject.position.z - bottomBound;
        //topBound = RootObject.position.z + topBound;
        WorldResetTop = RootObject.localPosition.z + topBound;
        WorldResetBottom = RootObject.localPosition.z + bottomBound;
        manager = World.Active.GetOrCreateManager<EntityManager>();
        AddShips(enemyShipCount, enemyShipPrefab);
        AddShips(enemyShipCount, EnemyShip2);
        AddShips(enemyShipCount, EnemyShip3);
        
    }

    void Update()
    {
        if (Input.GetKeyDown("space"))
            AddShips(enemyShipIncremement, EnemyShip3);
    }

    void AddShips(int amount, GameObject shipPrefab)
    {
        NativeArray<Entity> entities = new NativeArray<Entity>(amount, Allocator.Temp);

        manager.Instantiate(shipPrefab, entities);

        for (int i = 0; i < amount; i++)
        {
            float xVal = UnityEngine.Random.Range(leftBound, rightBound) + RootObject.localPosition.x;
            float yVal = UnityEngine.Random.Range(-5.0f, 5.0f) + RootObject.localPosition.y;
            float zVal = (RootObject.position.z + bottomBound) + Random.Range(-10.0f, 10.0f);
            
            manager.SetComponentData(entities[i], new Position { Value = new float3(xVal, yVal, zVal) });
            manager.SetComponentData(entities[i], new Rotation { Value = new quaternion(RootObject.rotation.x, RootObject.rotation.y, RootObject.rotation.z, RootObject.rotation.w) });
            manager.SetComponentData(entities[i], new MovementData{Value = enemySpeed});

            spawnedShip++;
            if (spawnedShip > 2)
            {
                spawnedShip = 0;
            }
            
        }
        entities.Dispose();

        count += amount;
    }
}

