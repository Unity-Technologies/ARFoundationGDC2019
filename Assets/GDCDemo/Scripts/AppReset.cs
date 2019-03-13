using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AppReset : MonoBehaviour
{

    bool m_resetting = false;
    void Update()
    {
        if (Input.touchCount > 3)
        {
            Touch firstTouch = Input.GetTouch(0);
            Touch secondTouch = Input.GetTouch(1);
            Touch thirdTouch = Input.GetTouch(2);
            Touch fourthTouch = Input.GetTouch(3);

            if (firstTouch.phase == TouchPhase.Began ||
                secondTouch.phase == TouchPhase.Began ||
                thirdTouch.phase == TouchPhase.Began ||
                fourthTouch.phase == TouchPhase.Began)
            {
                if (!m_resetting)
                {
                    ResetApp();
                }
            }
        }
        
    }

    void ResetApp()
    {
        m_resetting = true;
        Scene scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.name);
    }
    
}
