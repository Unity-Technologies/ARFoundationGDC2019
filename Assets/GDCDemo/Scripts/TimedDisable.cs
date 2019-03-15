using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimedDisable : MonoBehaviour
{
    [SerializeField] float m_DisableTime = 3;
    void Start()
    {
        Invoke("DisableObj", m_DisableTime);
    }

    void DisableObj()
    {
        this.gameObject.SetActive(false);
    }
}
