using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaceWIthRemote : MonoBehaviour
{

    [SerializeField] GameObject hitObjectPrefab;
    // Start is called before the first frame update
    void Start()
    {
        
    }

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
                Instantiate(hitObjectPrefab, hitInfo.point, Quaternion.identity);
            }
        }
    }
}
