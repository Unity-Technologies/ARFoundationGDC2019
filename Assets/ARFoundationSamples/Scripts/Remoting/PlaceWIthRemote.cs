using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaceWithRemote : MonoBehaviour
{

    [SerializeField] GameObject m_hitObjectPrefab = null;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hitInfo;
            Ray cameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(cameraRay, out hitInfo))
            {
                Debug.Log(hitInfo.transform.name);
                Instantiate(m_hitObjectPrefab, hitInfo.point, Quaternion.identity);
            }
        }
    }
}
