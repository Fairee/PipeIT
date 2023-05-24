using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class RayCastManager : MonoBehaviour
{


    SignManager signManager;
    private void Start()
    {
        signManager = SignManager.Instance;
    }

    // Update is called once per frame
    void Update()
    {
        //check for clicks
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            //Clicked on UI element
            if (EventSystem.current.currentSelectedGameObject != null) {
                return;
            }

            Ray ray = Camera.main.ScreenPointToRay(Input.GetTouch(0).position);
            RaycastHit hit;
            //check if we hit anything that interests us
            if (Physics.Raycast(ray, out hit)) {
                if (hit.transform.GetComponent<Pipe>() != null)
                {
                    signManager.CreateSign( hit);
                    return;
                }
                SignDestroyer destroyer = hit.transform.GetComponent<SignDestroyer>();
                if (destroyer != null) {
                    destroyer.DestroyIT();
                }

                OpenUI openUI = hit.transform.GetComponent<OpenUI>();
                if (openUI != null) {
                    openUI.OpenIt();
                }
            }


        }

#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            //Clicked on UI element
            if (EventSystem.current.currentSelectedGameObject != null)
            {
                return;
            }
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                if (hit.transform.GetComponent<Pipe>() != null)
                {
                    signManager.CreateSign( hit);
                    return;
                }
                SignDestroyer destroyer = hit.transform.GetComponent<SignDestroyer>();
                if (destroyer != null)
                {
                    destroyer.DestroyIT();
                }
                OpenUI openUI = hit.transform.GetComponent<OpenUI>();
                if (openUI != null)
                {
                    openUI.OpenIt();
                }
            }
        }
#endif
    }
}
