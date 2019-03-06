using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.XR;
using UnityEngine.Experimental.XR.Interaction;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

public class PlacementCircle : MonoBehaviour
{
    bool m_OnPlane = false;

    [SerializeField] GameObject m_PlacementCircle;
    [SerializeField] Toggle m_RockButton;
    
    static List<ARRaycastHit> k_Hits = new List<ARRaycastHit>();

    ARSessionOrigin m_SessionOrigin;
    Vector2 m_ScreenCenter;
    bool m_PortraitMode = false;
    bool m_ShowCircle = false;
    Transform m_CircleTransform;
    

    void Awake()
    {
        m_SessionOrigin = GetComponent<ARSessionOrigin>();
        m_CircleTransform = m_PlacementCircle.transform;
        m_PortraitMode = (Input.deviceOrientation == DeviceOrientation.Portrait ||
                          Input.deviceOrientation == DeviceOrientation.PortraitUpsideDown);
    }

    void Start()
    {
        m_PlacementCircle.SetActive(false);
        m_ScreenCenter = GetCenterScreen();
    }

    void Update()
    {
        if (m_RockButton.isOn)
        {
            if (m_ShowCircle)
            {
                // calc center screen
                if (m_PortraitMode != (Input.deviceOrientation == DeviceOrientation.Portrait ||
                                       Input.deviceOrientation == DeviceOrientation.PortraitUpsideDown))
                {
                    m_PortraitMode = !m_PortraitMode;
                    m_ScreenCenter = GetCenterScreen();
                }

                if (m_SessionOrigin.Raycast(m_ScreenCenter, k_Hits, TrackableType.PlaneWithinPolygon))
                {
                    m_PlacementCircle.SetActive(true);
                    m_CircleTransform.localPosition = k_Hits[0].pose.position;
                }
                else
                {
                    m_PlacementCircle.SetActive(false);
                }
            }
        }
        else
        {
            m_PlacementCircle.SetActive(false);
        }
    }

    Vector2 GetCenterScreen()
    {
        return new Vector2(Screen.width / 2, Screen.height / 2);
    }

    public void ToggleCircle()
    {
        m_ShowCircle = !m_ShowCircle;
    }
    
}
