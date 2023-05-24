using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The UI for the settings screen
/// </summary>
public class UISettings : MonoBehaviour
{
   
    //To load and save the settings
    SettingsManager settingsManager;
    //to load and save the pipe settings
    PipesSettingsManager pipesSettingsManager;

    //Set pipe size
    [SerializeField]
    Slider pipeSize;
    //set pipe from camera distance
    [SerializeField]
    Slider pipeDistance;
    //set altitude anchor reset threshold
    [SerializeField]
    Slider altitudeThreshold;
    //Reset all colors/turn ons/offs
    [SerializeField]
    Button resetColorsButton;
    [SerializeField]
    Button resetSwitchesButton;
    //Set the occlusion mode
    [SerializeField]
    TMP_Dropdown occlusion;
    //Shows unsupported if the occlusion is not supported
    [SerializeField]
    TMP_Text occlusionSupport;
    //set the lighting mode
    [SerializeField]
    TMP_Dropdown lighting;



    [SerializeField]
    Button close;

    [SerializeField]
    TMP_Text pipeSizeText;

    [SerializeField]
    TMP_Text pipeDistanceText;

    [SerializeField]
    TMP_Text altitudeThresholdText;

    //min and max values for the sliders
    float minPipeSize = 0.02f;
    float maxPipeSize = 0.2f;

    float minPipeDistance = 0.5f;
    float maxPipeDistance = 2f;

    float minAltitudeThreshold = 0.2f;
    float maxAltitudeThreshold = 2f;



    // Start is called before the first frame update
    private void Start()
    {
        settingsManager = SettingsManager.Instance;
        pipesSettingsManager = PipesSettingsManager.Instance;
        SetUpSliders();
        SetUpButtons();
        SetUpDropDown();

    }
    /// <summary>
    /// Sets the drop down to the value based on the saved user settings
    /// </summary>
    private void SetUpDropDown() {
        occlusion.value = settingsManager.GetOcclusionModeInt();
        if (settingsManager.IsOcclusitonSupported())
        {
            occlusion.gameObject.SetActive(true);
            occlusionSupport.gameObject.SetActive(false);
        }
        else
        {
            occlusionSupport.gameObject.SetActive(true);
            occlusion.gameObject.SetActive(false);
        }

        lighting.value = settingsManager.GetLightingModeInt();

    }
    /// <summary>
    /// Sets up the listneres to the buttons
    /// </summary>
    private void SetUpButtons() {
        resetColorsButton.onClick.AddListener(ResetColors);
        resetSwitchesButton.onClick.AddListener(ResetSwitches);
        close.onClick.AddListener(SaveAndQuit);
    }
    /// <summary>
    /// Saves the settings and quites the Settings menu
    /// </summary>
    private void SaveAndQuit() {
        settingsManager.SetSettingsValues(pipeSize.value, pipeDistance.value, altitudeThreshold.value, occlusion.value, lighting.value);
        this.gameObject.SetActive(false);
    }

    /// <summary>
    /// Initializes the Reset of the switches
    /// </summary>
    private void ResetSwitches() {
        pipesSettingsManager.ResetSwitches();
    }
    /// <summary>
    /// Initializes the Reset of the colors
    /// </summary>
    private void ResetColors()
    {
        pipesSettingsManager.ResetColors();
    }
    /// <summary>
    /// Set the slider to the values from the settings
    /// </summary>
    private void SetUpSliders() {
        pipeSize.minValue = minPipeSize;
        pipeSize.maxValue = maxPipeSize;
        pipeSize.value = settingsManager.GetPipeSize();
        ChangeValue(pipeSizeText, pipeSize);
        pipeSize.onValueChanged.AddListener((float a) => { ChangeValue(pipeSizeText, pipeSize); });

        pipeDistance.minValue = minPipeDistance;
        pipeDistance.maxValue = maxPipeDistance;
        pipeDistance.value = settingsManager.GetPipeDistance();
        ChangeValue(pipeDistanceText, pipeDistance);
        pipeDistance.onValueChanged.AddListener((float a) => { ChangeValue(pipeDistanceText, pipeDistance); });

        altitudeThreshold.minValue = minAltitudeThreshold;
        altitudeThreshold.maxValue = maxAltitudeThreshold;
        altitudeThreshold.value = settingsManager.GetThreshold();
        ChangeValue(altitudeThresholdText, altitudeThreshold);
        altitudeThreshold.onValueChanged.AddListener((float a) => { ChangeValue(altitudeThresholdText, altitudeThreshold); });
    }

    private void ChangeValue(TMP_Text text, Slider slider) {
        text.text = slider.value.ToString("F3");
    }

}
