using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChangeSignSizeButton : MonoBehaviour
{
    //To get the saved settings and save the user choice for the next time
    SettingsManager settingsManager;
    //To propagate the functions of the button to the signs
    SignManager signManager;
    //Internal state of the button
    private bool small = false;

    [SerializeField]
    Button button;
    //Sprite representing the Big state
    [SerializeField]
    Sprite smallIt;
    //Sprite representing the Small state
    [SerializeField]
    Sprite bigIt;
    //The image that shows the sprites
    [SerializeField]
    Image image;

    // Start is called before the first frame update
    void Start()
    {
        //load the user settings from last session
        settingsManager = SettingsManager.Instance;
        int type = settingsManager.GetSignType();
        if (type == 0)
        {
            small = false;
        }
        else {
            small = true;
        }
        //check if the all of the required components were set
        if (button == null) {
            button = GetComponent<Button>();
            if (button == null) {
                Debug.LogError("ChangeSignButton not assigned");
            }
        }
        if (image == null)
        {
            image = GetComponent<Image>();
            if (image == null)
            {
                Debug.LogError("ChangeSignImage not assigned");
            }
        }
        if (smallIt == null || bigIt == null) {
            Debug.LogError("I dont have sprites");
        }

        button.onClick.AddListener(ChangeType);
        
        //Adds listener to be able to hide/show the button when necessary
        signManager = SignManager.Instance;
        signManager.FirstSignAdded.AddListener(Show);
        signManager.LastSignRemoved.AddListener(Hide);
        //Hide the buton as at the very beginning there are no signs
        image.enabled = false;
        button.interactable = false;
    }
    /// <summary>
    /// Changes the type of the signs
    /// </summary>
    private void ChangeType() {
        small = !small;
        if (small)
        {
            settingsManager.SetSignType(1); // 1 = small
            image.sprite = bigIt;
        }
        else
        {
            settingsManager.SetSignType(0); // 0 = big
            image.sprite = smallIt;
        }
    }

    /// <summary>
    /// Make the button appear and become interactable
    /// </summary>
    private void Show()
    {
        image.enabled = true;
        button.interactable = true;
    }

    /// <summary>
    /// Hide the button and make it noninteractable
    /// </summary>
    private void Hide()
    {
        image.enabled = false;
        button.interactable = false;
    }
}
