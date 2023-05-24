using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DestroySignsButton : MonoBehaviour
{
    //To be able to tell the signs that they should be deleted
    SignManager signManager;

    Image image;
    Button button;
    /// <summary>
    /// Initialize the button
    /// </summary>
    void Awake()
    {
        //Check for the components
        button = GetComponent<Button>();
        if (button == null)
        {
            Debug.Log("DestroySignsButton cant acces Button component");
            return;
        }
        image = GetComponent<Image>();
        if (image == null)
        {
            Debug.Log("DestorySignsButton couldn't find an image component");
            return;
        }
        //add listeners so that the button can show/hide when necessary
        signManager = SignManager.Instance;
        signManager.FirstSignAdded.AddListener(Show);
        signManager.LastSignRemoved.AddListener(Hide);
        GetComponent<Button>().onClick.AddListener(DestroyAll);
        //hide and disable the button as there are no sings at the beginning
        image.enabled = false;
        button.interactable = false;
    }
    /// <summary>
    /// Make the button appear and become interactable
    /// </summary>
    private void Show() {
        image.enabled = true;
        button.interactable = true;
    }
    /// <summary>
    /// Hide the button and make it noninteractable
    /// </summary>
    private void Hide() {
        image.enabled = false;
        button.interactable = false;
    }
    /// <summary>
    /// Signals the signManager to delete all of the signs
    /// </summary>
    private void DestroyAll() {
        signManager.DestroyAllSigns();
    }

}
