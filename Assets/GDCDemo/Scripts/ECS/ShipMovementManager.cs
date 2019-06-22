using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = UnityEngine.Random;

public class ShipMovementManager : MonoBehaviour
{
    public static ShipMovementManager k_shipManager;

    [Header("Simulation Settings")]
    public float topBound = 20.0f;
    public float bottomBound = -20.0f;
    public float leftBound = -10f;
    public float rightBound = 10f;

    public float WorldResetBottom;
    public float WorldResetTop;

    [Header("Enemy Settings")]
    public GameObject EnemyShip1;
    public GameObject EnemyShip2;
    public GameObject EnemyShip3;
    public GameObject EnemyShip4;
    public GameObject EnemyShip5;

    [Header("Spawn Settings")]
    public int enemyShipCount = 1;
    public int enemyShipIncrement = 1;

    void Awake()
    {
        if (k_shipManager == null)
            k_shipManager = this;
        else if (k_shipManager != this)
            Destroy(gameObject);
    }

    EntityManager m_Manager;

    [SerializeField] Transform m_RootObject = null;
    

    void Start()
    {
        m_Manager = World.Active.GetOrCreateManager<EntityManager>();
    }

    void OnDisable()
    {
        // scene has been reset, clean up entities
        NativeArray<Entity> m_Allships = m_Manager.GetAllEntities();

        for (int i = 0; i < m_Allships.Length; i++)
        {
            m_Manager.DestroyEntity(m_Allships[i]);
        }
        
    }

    public void SpawnShips()
    {
        WorldResetTop = m_RootObject.position.z + topBound;
        WorldResetBottom = m_RootObject.position.z + bottomBound;
        AddShips(enemyShipCount, EnemyShip1);
        AddShips(enemyShipCount, EnemyShip2);
        AddShips(enemyShipCount, EnemyShip3);
        AddShips(enemyShipCount, EnemyShip4);
        AddShips(enemyShipCount, EnemyShip5);
        
    }

    void Update()
    {
        if (Input.GetKeyDown("space"))
            AddShips(enemyShipIncrement, EnemyShip3);
    }

    void AddShips(int amount, GameObject shipPrefab)
    {
        NativeArray<Entity> m_Entities = new NativeArray<Entity>(amount, Allocator.Temp);

        m_Manager.Instantiate(shipPrefab, m_Entities);

        for (int i = 0; i < amount; i++)
        {
            float m_XVal = UnityEngine.Random.Range(leftBound, rightBound) + m_RootObject.position.x;
            float m_YVal = UnityEngine.Random.Range(-5.0f, 5.0f) + m_RootObject.position.y;
            float m_ZVal = (m_RootObject.position.z) + Random.Range(bottomBound, topBound);
            
            m_Manager.SetComponentData(m_Entities[i], new Position { Value = new float3(m_XVal, m_YVal, m_ZVal) });
            m_Manager.SetComponentData(m_Entities[i], new Rotation { Value = new quaternion(m_RootObject.rotation.x, m_RootObject.rotation.y, m_RootObject.rotation.z, m_RootObject.rotation.w) });
            m_Manager.SetComponentData(m_Entities[i], new MovementData{Value = Random.Range(0.25f, 2.5f)});
            
        }
        m_Entities.Dispose();
    }
}

