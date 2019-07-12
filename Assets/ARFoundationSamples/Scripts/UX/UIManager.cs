using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class UIManager : MonoBehaviour
{
    const string k_FadeOffAnim = "FadeOff";
    const string k_FadeOnAnim = "FadeOn";

    [SerializeField]
    ARPlaneManager m_PlaneManager;

    [SerializeField]
    ARCameraManager m_CameraManager;

    public ARPlaneManager planeManager
    {
        get { return m_PlaneManager; }
        set { m_PlaneManager = value; }
    }

    [SerializeField]
    Animator m_MoveDeviceAnimation;

    public Animator moveDeviceAnimation
    {
        get { return m_MoveDeviceAnimation; }
        set { m_MoveDeviceAnimation = value; }
    }

    [SerializeField]
    Animator m_TapToPlaceAnimation;

    public Animator tapToPlaceAnimation
    {
        get { return m_TapToPlaceAnimation; }
        set { m_TapToPlaceAnimation = value; }
    }

    static List<ARPlane> s_Planes = new List<ARPlane>();

    bool m_ShowingTapToPlace = false;

    bool m_ShowingMoveDevice = true;

    void OnEnable()
    {
        m_CameraManager.frameReceived += FrameChanged;
        PlaceMultipleObjectsOnPlane.onPlacedObject += PlacedObject;
        PlaceObjectAtTransform.onPlacedObject += PlacedObject;
    }

    void OnDisable()
    {
        m_CameraManager.frameReceived -= FrameChanged;
        PlaceMultipleObjectsOnPlane.onPlacedObject -= PlacedObject;
        PlaceObjectAtTransform.onPlacedObject -= PlacedObject;
    }

    void FrameChanged(ARCameraFrameEventArgs args)
    {
        if (PlanesFound() && m_ShowingMoveDevice)
        {
            if (moveDeviceAnimation)
                moveDeviceAnimation.SetTrigger(k_FadeOffAnim);

            if (tapToPlaceAnimation)
                tapToPlaceAnimation.SetTrigger(k_FadeOnAnim);

            m_ShowingTapToPlace = true;
            m_ShowingMoveDevice = false;
        }
    }

    bool PlanesFound()
    {
        if (planeManager == null)
            return false;

        return planeManager.trackables.count > 0;
    }

    void PlacedObject()
    {
        if (m_ShowingTapToPlace)
        {
            if (tapToPlaceAnimation)
                tapToPlaceAnimation.SetTrigger(k_FadeOffAnim);

            m_ShowingTapToPlace = false;
        }
    }
}
