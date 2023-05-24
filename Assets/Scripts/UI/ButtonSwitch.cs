using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Representation of the button that switches the visibility of a type/subtype and changes its color
/// </summary>
public class ButtonSwitch : MonoBehaviour
{
    //Settings to differenciate between PipeType and subTypes buttons
    bool isSub = false;
    PipesSettingsManager.SubType subType;
    PipesSettingsManager.PipeType pipeType;
   
    //To access the settings from the last time
    PipesSettingsManager pipesSettingManager;
    //keeps track whether the pipe is hidden or not
    bool switcher = false;

    //The image that shows if the type is hidden or not
    [SerializeField]
    Image image;
    //Sprites corresponding to the on and off states
    [SerializeField]
    Sprite on, off;
    //the button for accesing the color changer menu
    [SerializeField]
    Button colorButton;
    //the image of the color accessing button
    [SerializeField]
    Image ColorImage;

    //the UI for the color changing.
    UIColorChangerScreen colorChangerScreen;


    public void Initialize(UIColorChangerScreen colorChangerScreen) {
        this.colorChangerScreen = colorChangerScreen;
        pipesSettingManager = PipesSettingsManager.Instance;
        
        //Find my button components and add a listener to it
        var button = this.GetComponent<Button>();
        button.onClick.AddListener(Switch);


        colorButton.onClick.AddListener(OpenChangeColorScreen);

        //Add listeners to change the state of the button when the color changes or the pipe hides
        pipesSettingManager.SubTypeChanged.AddListener(ChangeSprite);
        pipesSettingManager.PipeTypeChanged.AddListener(ChangeSprite);
        
        pipesSettingManager.ColorChangedPipe.AddListener(ChangeIconColor);
        pipesSettingManager.ColorChangedSub.AddListener(ChangeIconColor);

        pipesSettingManager.ColorResetMade.AddListener(ResetMyColor);
        pipesSettingManager.TypesResetMade.AddListener(ResetSwitch);
    }

    /// <summary>
    /// Reset the switch state to the default
    /// </summary>
    private void ResetSwitch() {
        bool state;
        if (isSub)
        {
            state = pipesSettingManager.GetSubtypeSwitch(subType);
            ChangeSprite(subType, state);
        }
        else {
            state = pipesSettingManager.GetTypeSwitch(pipeType);
            ChangeSprite(pipeType, state);
        }
        switcher = state;
    }
    /// <summary>
    /// Reset the color to the defualt
    /// </summary>
    private void ResetMyColor() {
        Color color;
        if (isSub)
        {
            color = pipesSettingManager.GetColor(subType);
        }
        else {
            color = pipesSettingManager.GetColor(pipeType);
        }
        ColorImage.color = color;
    }

    /// <summary>
    /// Changes the icon color
    /// </summary>
    /// <param name="type">The type that has its color changed</param>
    /// <param name="color">The color to which it changed</param>
    private void ChangeIconColor(PipesSettingsManager.PipeType type, Color color) {
        //Check if im a type button
        if (!isSub) {
            //Check if the type is the one I'm representing
            if (type == pipeType) {
                ColorImage.color = color;
            }
        }
    }
    /// <summary>
    /// Changes the icon color
    /// </summary>
    /// <param name="sub">The type that has its color changed</param>
    /// <param name="color">The color to which it changed</param>
    private void ChangeIconColor(PipesSettingsManager.SubType sub, Color color)
    {
        //check if im subtype button
        if (isSub)
        {
            //Check if the sub is the one I'm representing
            if (sub == subType)
            {
                ColorImage.color = color;
            }
        }
    }

    /// <summary>
    /// Opens the color changing UI
    /// </summary>
    private void OpenChangeColorScreen() {
        //tell the color changing UI what type is it changing
        if (isSub)
        {
            colorChangerScreen.Initialize(subType);
        }
        else {
            colorChangerScreen.Initialize(pipeType);
        }
        colorChangerScreen.gameObject.SetActive(true);
    }
    /// <summary>
    /// Changes the sprite based on the value
    /// </summary>
    /// <param name="sub">The type that has its color changed</param>
    /// <param name="value">The value to which it will change</param>
    private void ChangeSprite(PipesSettingsManager.SubType sub, bool value) {
        //check if im subtype button
        if (isSub) {
            //Check if the sub is the one I'm representing
            if (subType == sub) {
                if (value)
                {
                    image.sprite = on;
                }
                else {
                    image.sprite = off;
                }
            }
        }
    }
    /// <summary>
    /// Changes the sprite based on the value
    /// </summary>
    /// <param name="type">The type that has its color changed</param>
    /// <param name="value">The value to which it will change</param>
    private void ChangeSprite(PipesSettingsManager.PipeType type, bool value) {
        //check if im type button
        if (!isSub) {
            //Check if the type is the one I'm representing
            if (pipeType == type) {
                if (value)
                {
                    image.sprite = on;
                }
                else {
                    image.sprite = off;
                }
            
            }
        }
    }
    /// <summary>
    /// Sets the value for the button based on the type
    /// </summary>
    /// <param name="type">The type the button represents</param>
    public void SetType(PipesSettingsManager.PipeType type) {
        pipeType = type;
        isSub = false;
        switcher = pipesSettingManager.GetTypeSwitch(type);
        Color color = pipesSettingManager.GetColor(type);
        ColorImage.color = color;
        ChangeSprite(type, switcher);
        Button button = GetComponent<Button>();
        SetName(button, type);
    }

    /// <summary>
    /// Sets the value for the button based on the type
    /// </summary>
    /// <param name="sub">The subtype the button represents</param>
    public void SetType(PipesSettingsManager.SubType sub) {
        subType = sub;
        isSub = true;
        switcher = pipesSettingManager.GetSubtypeSwitch(sub);
        Color color = pipesSettingManager.GetColor(sub);
        ColorImage.color = color;
        ChangeSprite(sub, switcher);
        Button button = GetComponent<Button>();
        SetName(button, sub);
    }

    /// <summary>
    /// Signals that the button should change its state
    /// </summary>
    public void Switch() {
        switcher = !switcher;
        if (isSub)
        {
            pipesSettingManager.SetSubTypeSwitch(subType, switcher);
        }
        else {
            pipesSettingManager.SetPipeTypeSwitch(pipeType, switcher);
        }
    }

    /// <summary>
    /// Sets the name of the button
    /// </summary>
    /// <param name="button">The button to have its name set</param>
    /// <param name="type">The type that this button will represent</param>
    private void SetName(Button button, PipesSettingsManager.PipeType type)
    {
        TMP_Text text = button.transform.Find("Name").GetComponent<TMP_Text>();
        switch (type)
        {
            case PipesSettingsManager.PipeType.Kolektor:
                text.text = "Kolektor";
                break;
            case PipesSettingsManager.PipeType.Posta:
                text.text = "Potrubní Pošta";
                break;
            case PipesSettingsManager.PipeType.Produkt:
                text.text = "Produktovod";
                break;
        }
    }
    /// <summary>
    /// Sets the name of the button
    /// </summary>
    /// <param name="button">The button to have its name set</param>
    /// <param name="sub">The type that this button will represent</param>
    private void SetName(Button button, PipesSettingsManager.SubType sub)
    {
        TMP_Text text = button.transform.Find("Name").GetComponent<TMP_Text>();
        switch (sub)
        {
            case PipesSettingsManager.SubType.Antena:
                text.text = "Antena";
                break;
            case PipesSettingsManager.SubType.Be:
            case PipesSettingsManager.SubType.Bez:
            case PipesSettingsManager.SubType.BezR:
            case PipesSettingsManager.SubType.BezRo:
            case PipesSettingsManager.SubType.BezRoz:
            case PipesSettingsManager.SubType.BezRozl:
                text.text = "Bez Rozliseni";
                break;
            case PipesSettingsManager.SubType.Dalkove:
                text.text = "Dalkova";
                break;
            case PipesSettingsManager.SubType.Destovat:
                text.text = "Destova";
                break;
            case PipesSettingsManager.SubType.Hodiny:
                text.text = "Hodiny";
                break;
            case PipesSettingsManager.SubType.Horkovod:
                text.text = "Horkovod";
                break;
            case PipesSettingsManager.SubType.Jednotna:
                text.text = "Jednotna";
                break;
            case PipesSettingsManager.SubType.Kalova:
                text.text = "Kalova";
                break;
            case PipesSettingsManager.SubType.MistniTelefon:
                text.text = "Mistni Telefon";
                break;
            case PipesSettingsManager.SubType.NN:
                text.text = "NN";
                break;
            case PipesSettingsManager.SubType.NTL:
                text.text = "NTL";
                break;
            case PipesSettingsManager.SubType.Obj:
            case PipesSettingsManager.SubType.Objekty:
                text.text = "Objekty";
                break;
            case PipesSettingsManager.SubType.Odlehcovaci:
                text.text = "Odlehcovaci";
                break;
            case PipesSettingsManager.SubType.Parovod:
                text.text = "Parovod";
                break;
            case PipesSettingsManager.SubType.PKO:
                text.text = "PKO";
                break;
            case PipesSettingsManager.SubType.Sekundarni:
                text.text = "Sekudnarni";
                break;
            case PipesSettingsManager.SubType.Splaskova:
                text.text = "Splaskova";
                break;
            case PipesSettingsManager.SubType.STL:
                text.text = "STL";
                break;
            case PipesSettingsManager.SubType.Telefon:
                text.text = "Telefon";
                break;
            case PipesSettingsManager.SubType.TelefoniBudka:
                text.text = "Telefonni Budka";
                break;
            case PipesSettingsManager.SubType.Teplovod:
                text.text = "Teplovod";
                break;
            case PipesSettingsManager.SubType.Verej:
                text.text = "Verejnr Osvetleni";
                break;
            case PipesSettingsManager.SubType.VN:
                text.text = "VN";
                break;
            case PipesSettingsManager.SubType.VodovodLetni:
                text.text = "Vodovod Letni";
                break;
            case PipesSettingsManager.SubType.VodovodPitna:
                text.text = "Vodovod Pitna";
                break;
            case PipesSettingsManager.SubType.VodovodUzit:
                text.text = "Vodovd Uzitkova";
                break;
            case PipesSettingsManager.SubType.VTL:
                text.text = "VTL";
                break;
            case PipesSettingsManager.SubType.WN:
                text.text = "WN";
                break;
            case PipesSettingsManager.SubType.Zemni:
                text.text = "Zemni";
                break;
        }
    }
}
