using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AppReset : MonoBehaviour
{

    bool m_Resetting = false;
    void Update()
    {
        if (Input.touchCount > 3)
        {
            Touch m_FirstTouch = Input.GetTouch(0);
            Touch m_SecondTouch = Input.GetTouch(1);
            Touch m_ThirdTouch = Input.GetTouch(2);
            Touch m_FourthTouch = Input.GetTouch(3);

            if (m_FirstTouch.phase == TouchPhase.Began ||
                m_SecondTouch.phase == TouchPhase.Began ||
                m_ThirdTouch.phase == TouchPhase.Began ||
                m_FourthTouch.phase == TouchPhase.Began)
            {
                if (!m_Resetting)
                {
                    ResetApp();
                }
            }
        }
        
    }

    void ResetApp()
    {
        m_Resetting = true;
        Scene m_Scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(m_Scene.name);
    }
    
}
