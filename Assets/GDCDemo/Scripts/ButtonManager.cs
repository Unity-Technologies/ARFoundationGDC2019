using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonManager : MonoBehaviour
{
    [SerializeField] GameObject m_ButtonPanelStart = null;
    [SerializeField] GameObject m_ButtonPanelFinal = null;

    bool m_ShowingStartPanel = true;
    // Update is called once per frame
    void Update()
    {
        if (Input.touchCount > 1 && Input.touchCount < 3)
        {
            Touch m_FirstTouch = Input.GetTouch(0);
            Touch m_SecondTouch = Input.GetTouch(1);
            
            if (m_FirstTouch.phase == TouchPhase.Began || m_SecondTouch.phase == TouchPhase.Began)
            {
                TogglePanels();
            }
        }
    }
    void TogglePanels()
    {
        if (m_ShowingStartPanel)
        {
            m_ButtonPanelStart.SetActive(false);
            m_ButtonPanelFinal.SetActive(true);
            m_ShowingStartPanel = false;
        }
        else
        {
            m_ButtonPanelStart.SetActive(true);
            m_ButtonPanelFinal.SetActive(false);
            m_ShowingStartPanel = true;
        }
        
    }
    
}
