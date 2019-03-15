using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.XR;
using UnityEngine.XR.ARFoundation;

public class AutoObjectPlacement : MonoBehaviour
{
    [SerializeField] float m_ProtectedRadius = 1;

    [SerializeField] List<GameObject> m_PlacementPrefabs;
    ARPointCloudManager m_PointCloudManager;

    ARSessionOrigin m_SessionOrigin;
    static List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();

    TrackableType m_TrackableTypeMask = TrackableType.FeaturePoint | TrackableType.PlaneWithinPolygon;
    
    Vector2 m_ScreenCenter;
    int m_PlacementIndex = 0;
    float m_MaxSize = 0.5f;
    float m_MinSize = 0.2f;
    GameObject m_SpawnObject;
    float m_RandomScale;
    
    bool m_CanPlace = false;

    void Start()
    {
        m_SessionOrigin = GetComponent<ARSessionOrigin>();
        m_ScreenCenter = new Vector2(Screen.width / 2, Screen.height / 2);
        m_PointCloudManager = GetComponent<ARPointCloudManager>();
    }

    void Update()
    {
        if (m_CanPlace)
        {
            if (m_SessionOrigin.Raycast(m_ScreenCenter, s_Hits, m_TrackableTypeMask))
            {
                PlaceObjectTest();
            }
        }
    }

    void PlaceObjectTest()
    {
        var hitPose = s_Hits[0].pose;
        Collider[] hitColliders = Physics.OverlapSphere(hitPose.position, m_ProtectedRadius);
        
        if (hitColliders.Length > 0)
        {
            return;
        }
        else
        {
            m_SpawnObject = RandomItem();
            /*
            m_RandomScale = Random.Range(m_MinSize, m_MaxSize);
            m_SpawnObject.transform.localScale = new Vector3(m_RandomScale, m_RandomScale, m_RandomScale);
            */
            Instantiate(m_SpawnObject, hitPose.position, hitPose.rotation);
        }
    }

    GameObject RandomItem()
    {
        int newIndex = Random.Range(0, m_PlacementPrefabs.Count);
        if (newIndex != m_PlacementIndex)
        {
            m_PlacementIndex = newIndex;
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
        m_PointCloudManager.pointCloud.gameObject.SetActive(m_CanPlace);
    }
}
