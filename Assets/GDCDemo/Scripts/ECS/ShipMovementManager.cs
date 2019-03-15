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
    public float topBound = 20.0f;
    public float bottomBound = -20.0f;
    public float leftBound = -10f;
    public float rightBound = 10f;

    public float WorldResetBottom;
    public float WorldResetTop;

    [Header("Enemy Settings")]
    public GameObject enemyShipPrefab;
    public float enemySpeed = 1f;


    public GameObject EnemyShip1;
    public GameObject EnemyShip2;
    public GameObject EnemyShip3;
    public GameObject EnemyShip4;
    public GameObject EnemyShip5;

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
        manager = World.Active.GetOrCreateManager<EntityManager>();
        
    }

    public void SpawnShips()
    {
        WorldResetTop = RootObject.position.z + topBound;
        WorldResetBottom = RootObject.position.z + bottomBound;
        AddShips(enemyShipCount, EnemyShip1);
        AddShips(enemyShipCount, EnemyShip2);
        AddShips(enemyShipCount, EnemyShip3);
        AddShips(enemyShipCount, EnemyShip4);
        AddShips(enemyShipCount, EnemyShip5);
        
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
            float xVal = UnityEngine.Random.Range(leftBound, rightBound) + RootObject.position.x;
            float yVal = UnityEngine.Random.Range(-5.0f, 5.0f) + RootObject.position.y;
            float zVal = (RootObject.position.z) + Random.Range(bottomBound, topBound);
            
            manager.SetComponentData(entities[i], new Position { Value = new float3(xVal, yVal, zVal) });
            manager.SetComponentData(entities[i], new Rotation { Value = new quaternion(RootObject.rotation.x, RootObject.rotation.y, RootObject.rotation.z, RootObject.rotation.w) });
            manager.SetComponentData(entities[i], new MovementData{Value = Random.Range(0.25f, 2.5f)});

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

