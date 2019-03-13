using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;

[RequireComponent(typeof(ARSessionOrigin))]
public class PlaceObjectAtTransform : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Instantiates this prefab on a plane at the touch location.")]
    GameObject m_PlacedPrefab;

    [SerializeField] Transform m_PlacementTransform;
    [SerializeField] PlacementCircle m_PlacementCircle;

    /// <summary>
    /// The prefab to instantiate on touch.
    /// </summary>
    public GameObject placedPrefab
    {
        get { return m_PlacedPrefab; }
        set { m_PlacedPrefab = value; }
    }

    bool m_CanPlace = false;
    bool m_PlacedObject = false;
    [SerializeField] GameObject m_SpanwedObject;
    
    public static event Action onPlacedObject;
    
    static List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();
    
    void Update()
    {
        if (m_CanPlace)
        {
            if (Input.touchCount == 0)
            {
                return;
            }

            var touch = Input.GetTouch(0);

            if (IsTouchOverUIObject(touch))
            {
                return;
            }

            if (!m_PlacedObject)
            {
                m_PlacedObject = true;
                //m_SpanwedObject = Instantiate(m_PlacedPrefab, m_PlacementTransform.position, Quaternion.identity);
                m_SpanwedObject.transform.position = m_PlacementTransform.position;

                if (onPlacedObject != null)
                {
                    onPlacedObject();
                }
            }
            else
            {
                m_SpanwedObject.transform.position = m_PlacementTransform.position;
            }
        }
    }

    public void TogglePlacement()
    {
        m_CanPlace = !m_CanPlace;

        // turning button off
        if (m_CanPlace == false)
        {
            m_PlacementCircle.ToggleCircle();
        }
    }
    
    bool IsTouchOverUIObject(Touch touch)
    {   
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = touch.position;
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        return results.Count > 0;
    }


}
