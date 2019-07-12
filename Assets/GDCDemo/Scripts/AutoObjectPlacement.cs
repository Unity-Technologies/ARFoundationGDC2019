using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class AutoObjectPlacement : MonoBehaviour
{
    [SerializeField] float m_ProtectedRadius = 1;

    [SerializeField] List<GameObject> m_PlacementPrefabs = null;
    ARPointCloudManager m_PointCloudManager;
    ARRaycastManager m_RaycastManager;
    
    ARSessionOrigin m_SessionOrigin;
    static List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();

    TrackableType m_TrackableTypeMask = TrackableType.PlaneWithinPolygon | TrackableType.FeaturePoint;
    
    Vector2 m_ScreenCenter;
    int m_PlacementIndex = 0;
    GameObject m_SpawnObject;
    float m_RandomScale;
    
    bool m_CanPlace = false;

    void Start()
    {
        m_SessionOrigin = GetComponent<ARSessionOrigin>();
        m_RaycastManager = GetComponent<ARRaycastManager>();
        m_ScreenCenter = new Vector2(Screen.width / 2, Screen.height / 2);
        m_PointCloudManager = GetComponent<ARPointCloudManager>();
    }

    void Update()
    {
        if (m_CanPlace)
        {
            if (m_RaycastManager.Raycast(m_ScreenCenter, s_Hits, m_TrackableTypeMask))
            {
                PlaceObjectTest();
            }
        }
    }

    void PlaceObjectTest()
    {
        var m_HitPose = s_Hits[0].pose;
        Collider[] m_HitColliders = Physics.OverlapSphere(m_HitPose.position, m_ProtectedRadius);
        
        if (m_HitColliders.Length > 0)
        {
            return;
        }
        else
        {
            m_SpawnObject = RandomItem();
            Instantiate(m_SpawnObject, m_HitPose.position, m_HitPose.rotation);
        }
    }

    GameObject RandomItem()
    {
        int m_NewIndex = Random.Range(0, m_PlacementPrefabs.Count);
        if (m_NewIndex != m_PlacementIndex)
        {
            m_PlacementIndex = m_NewIndex;
            return m_PlacementPrefabs[m_PlacementIndex];
        }
        else
        {
            return RandomItem();
        }
    }

    public void TogglePlacement()
    {
        m_CanPlace = !m_CanPlace;

        foreach (var pointcloud in m_PointCloudManager.trackables)
        {
            pointcloud.gameObject.SetActive(m_CanPlace);
        }
        
    }
}
