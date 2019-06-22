using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Playables;
using UnityEngine.XR.ARFoundation;

[RequireComponent(typeof(ARSessionOrigin))]
public class PlaceObjectAtTransform : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Instantiates this prefab on a plane at the transform location.")]
    GameObject m_PlacedPrefab;

    [SerializeField] Transform m_PlacementTransform = null;
    [SerializeField] PlacementCircle m_PlacementCircle = null;

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
    [SerializeField] GameObject m_SpanwedObject = null;
    
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

            var m_Touch = Input.GetTouch(0);

            if (IsTouchOverUIObject(m_Touch))
            {
                return;
            }

            if (!m_PlacedObject)
            {
                m_PlacedObject = true;
                m_SpanwedObject.transform.position = m_PlacementTransform.position;

                RotateTowardCamera();
                
                // play jump timeline animation
                m_SpanwedObject.GetComponent<PlayableDirector>().Play();

                if (onPlacedObject != null)
                {
                    onPlacedObject();
                }
            }
            else
            {
                m_SpanwedObject.transform.position = m_PlacementTransform.position;
                RotateTowardCamera();
            }
        }
    }

    public void TogglePlacement()
    {
        m_CanPlace = !m_CanPlace;
        m_PlacementCircle.ToggleCircle();
    }
    
    bool IsTouchOverUIObject(Touch touch)
    {   
        PointerEventData m_EventDataCurrentPosition = new PointerEventData(EventSystem.current);
        m_EventDataCurrentPosition.position = touch.position;
        List<RaycastResult> m_Results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(m_EventDataCurrentPosition, m_Results);
        return m_Results.Count > 0;
    }

    void RotateTowardCamera()
    {
        Vector3 m_LookVector = Camera.main.transform.position - m_SpanwedObject.transform.position;
        m_SpanwedObject.transform.rotation = Quaternion.LookRotation(m_LookVector, Vector3.up);
        m_SpanwedObject.transform.rotation = new Quaternion(0, m_SpanwedObject.transform.rotation.y, 0, m_SpanwedObject.transform.rotation.w) * Quaternion.Euler(0,180,0);
    }


}
