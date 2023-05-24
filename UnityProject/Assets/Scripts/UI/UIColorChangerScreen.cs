using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Takes care of the color changing screen. It generates the textures for the HSV sliders
/// </summary>
public class UIColorChangerScreen : MonoBehaviour
{

    [SerializeField]
    Button saveButton;
    [SerializeField]
    Button returnButton;
    [SerializeField]
    Slider hueSlider;
    [SerializeField]
    Slider saturationSlider;
    [SerializeField]
    Slider valueSlider;

    [SerializeField]
    private Image ColorShower;

    private Image hueSliderHandle;
    private Image saturationSliderHandle;
    private Image valueSliderHandle;

    private Image hueBackground; 
    private Image saturationBackground;
    private Image valueBackground;

    float currentHue, currentSaturation, currentValue;


    //To get the appropriate color
    PipesSettingsManager pipesSettingManager;

    //diferenciate between types and subtypes
    PipesSettingsManager.SubType currentSubType;
    PipesSettingsManager.PipeType currentPipeType;
    bool currentlyHaveSubType = false;

    // Start is called before the first frame update
    /// <summary>
    /// Initialize the menu
    /// </summary>
    /// <param name="subtype">subtype that the menu is generated for</param>
    public void Initialize(PipesSettingsManager.SubType subtype) {
        pipesSettingManager = PipesSettingsManager.Instance;
        //gets the current pipe colour
        Color color = pipesSettingManager.GetColor(subtype);
        float h, s, v;
        Color.RGBToHSV(color, out h, out s, out v);
        currentHue = h;
        currentSaturation = s;
        currentValue = v;
        currentSubType = subtype;
        currentlyHaveSubType = true;

        //sets up the sliders and image
        SetUpHueSlider();
        SetUpSaturationSlider();
        SetUpValueSlider();
        SetUpImage();

        saveButton.onClick.AddListener(SaveAndQuit);
        returnButton.onClick.AddListener(Quit);
    }
    /// <summary>
    /// Initialize the menu
    /// </summary>
    /// <param name="pipetype">type that the menu is generated for</param>
    public void Initialize(PipesSettingsManager.PipeType pipetype) {
        pipesSettingManager = PipesSettingsManager.Instance;
        Color color = pipesSettingManager.GetColor(pipetype);
        float h, s, v;
        Color.RGBToHSV(color, out h, out s, out v);
        currentHue = h;
        currentSaturation = s;
        currentValue = v;
        currentPipeType = pipetype;
        currentlyHaveSubType = false;

        //sets up the sliders and image
        SetUpHueSlider();
        SetUpSaturationSlider();
        SetUpValueSlider();
        SetUpImage();
        saveButton.onClick.AddListener(SaveAndQuit);
        returnButton.onClick.AddListener(Quit);
    }
    /// <summary>
    /// Sets up the image to showcase the current color
    /// </summary>
    private void SetUpImage() {
        Color color = Color.HSVToRGB(currentHue, currentSaturation, currentValue);
        ColorShower.color = color;
    }
    /// <summary>
    /// closes the menu without saving
    /// </summary>
    public void Quit()
    {
        gameObject.SetActive(false);
    }
    /// <summary>
    /// Saves and then closes the menu
    /// </summary>
    public void SaveAndQuit() {
        Color color = Color.HSVToRGB(currentHue, currentSaturation, currentValue);
        if (currentlyHaveSubType)
        {
            pipesSettingManager.SetColor(color, currentSubType);
        }
        else {
            pipesSettingManager.SetColor(color, currentPipeType);
        }
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Initilaize the hue slider
    /// </summary>
    private void SetUpHueSlider() {
        hueSlider.maxValue = 1;
        hueSlider.minValue = 0;
        hueSlider.value = currentHue;

        Transform handle = hueSlider.transform.Find("Handle Slide Area").Find("Handle");
        hueSliderHandle = handle.GetComponent<Image>();
        hueBackground = hueSlider.transform.Find("Background").GetComponent<Image>();
        SetHueBackground();
        hueSlider.onValueChanged.AddListener(HueChanged);
    }
    /// <summary>
    /// Make changes to the color based on the new value
    /// </summary>
    /// <param name="value">new hue value for the color</param>
    private void HueChanged(float value) {
        Color color = Color.HSVToRGB(value, 1, 1);
        hueSliderHandle.color = color;
        currentHue = value;
        ColorShower.color = Color.HSVToRGB(currentHue, currentSaturation, currentValue);
        UpdateSaturationSlider();
        UpdateValueSlider();
    }


    /// <summary>
    /// Creates the background for the hue bar
    /// </summary>
    private void SetHueBackground() {
        Texture2D tex = new Texture2D(256, 1);
        for (int i = 0; i < 256; i++)
        {
            float hue = i / 255f;
            Color color = Color.HSVToRGB(hue,1, 1);
            tex.SetPixel(i, 0, color);
        }
        tex.Apply();
        hueBackground.sprite = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
        Color colour = Color.HSVToRGB(currentHue, 1, 1);
        hueSliderHandle.color = colour;
    }

    /// <summary>
    /// Sets up the saturation slider
    /// </summary>
    private void SetUpSaturationSlider() {
        saturationSlider.maxValue = 1;
        saturationSlider.minValue = 0;
        saturationSlider.value = currentSaturation;

        Transform handle = saturationSlider.transform.Find("Handle Slide Area").Find("Handle");
        saturationSliderHandle = handle.GetComponent<Image>();
        saturationSlider.onValueChanged.AddListener(SaturationChanged);
        saturationBackground = saturationSlider.transform.Find("Background").GetComponent<Image>();
        UpdateSaturationSlider();
    }


    /// <summary>
    /// Updates the background and the color of the handle for the saturation slider
    /// </summary>
    private void UpdateSaturationSlider() {
        Texture2D tex = new Texture2D(256, 1);
        for (int i = 0; i < 256; i++)
        {
            float saturation = i / 255f;
            Color color = Color.HSVToRGB(currentHue, saturation, currentValue);
            tex.SetPixel(i, 0, color);
        }
        tex.Apply();
        saturationBackground.sprite = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
        Color colour = Color.HSVToRGB(currentHue, currentSaturation, currentValue);
        saturationSliderHandle.color = colour;

    }
    /// <summary>
    /// Changes the colors saturation value
    /// </summary>
    /// <param name="value">New saturation value of the color</param>
    private void SaturationChanged(float value) {
        currentSaturation = value;
        Color color = Color.HSVToRGB(currentHue, currentSaturation, currentValue);
        saturationSliderHandle.color = color;
        ColorShower.color = color;
        UpdateValueSlider();
    }
    /// <summary>
    /// Sets up the value slider
    /// </summary>
    private void SetUpValueSlider()
    {
        valueSlider.maxValue = 1;
        valueSlider.minValue = 0;
        valueSlider.value = currentValue;

        Transform handle = valueSlider.transform.Find("Handle Slide Area").Find("Handle");
        valueSliderHandle = handle.GetComponent<Image>();
        valueSlider.onValueChanged.AddListener(ValueChanged);
        valueBackground = valueSlider.transform.Find("Background").GetComponent<Image>();
        UpdateValueSlider();
    }
    /// <summary>
    /// Updates the background and the color of the handle for the value slider
    /// </summary>
    private void UpdateValueSlider()
    {
        Texture2D tex = new Texture2D(256, 1);
        for (int i = 0; i < 256; i++)
        {
            float value = i / 255f;
            Color color = Color.HSVToRGB(currentHue, currentSaturation, value);
            tex.SetPixel(i, 0, color);
        }
        tex.Apply();
        valueBackground.sprite = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
        Color colour = Color.HSVToRGB(currentHue, currentSaturation, currentValue);
        valueSliderHandle.color = colour;

    }

    /// <summary>
    /// Changes a value of the color to the new value
    /// </summary>
    /// <param name="value">The new value for the value</param>
    private void ValueChanged(float value)
    {
        currentValue= value;
        Color color = Color.HSVToRGB(currentHue, currentSaturation, currentValue);
        valueSliderHandle.color = color;
        ColorShower.color = color;
        UpdateSaturationSlider();
    }





}
