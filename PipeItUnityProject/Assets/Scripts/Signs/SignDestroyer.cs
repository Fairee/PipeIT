using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//Initiate the destoyment of a sign
public class SignDestroyer : MonoBehaviour
{
    /// <summary>
    /// Tries tp destroy the sign
    /// </summary>
    public void DestroyIT()
    {
        try
        {
            SignManager.Instance.RemoveSign(gameObject.transform.parent.parent.GetComponent<Sign>());
            gameObject.SetActive(false);
        }
        catch {
            Debug.LogError(gameObject.transform.parent.parent.gameObject.name + "Did not have the Sign component");
        }
    }
}
