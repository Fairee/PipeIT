using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OcclusionButton : MonoBehaviour
{
    Button button;
    Image image;

    //To save the settings and load the settings and propagete the action to the application
    SettingsManager settings;

    [SerializeField]
    Sprite on;
    [SerializeField]
    Sprite off;

    bool state = false;

    // Start is called before the first frame update
    void Start()
    {
        //check for the components
        button = GetComponent<Button>();
        if (button == null) {
            Debug.Log("OcclusionButtonScript cant acces Button component");
            return;            
        }
        image = GetComponent<Image>();
        if (image == null) {
            Debug.Log("OcclusionButtonScript couldn't find an image component");
            return;
        }
        settings = SettingsManager.Instance;
        //set up the initial state
        if (settings.GetOcclusionSwitch())
        {
            state = true;
            image.sprite = on;
        }
        else {
            state = false;
            image.sprite = off;
        }

        button.onClick.AddListener(Switch);
    }

    /// <summary>
    /// Switches the state of the button
    /// </summary>
    private void Switch() {
        state = !state;
        if (state)
        {
            image.sprite = on;
        }
        else {
            image.sprite = off;
        }
        //inform the settings manager that the state has changed
        settings.SetOcclusionValue(state);   
    }

}
