using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Representation of the button that leads to a submenu
/// </summary>
public class ButtonMenu : MonoBehaviour
{
    PipesSettingsManager.PipeType pipeType;

    private UISubMenu subMenu;

    /// <summary>
    /// sets the submenu that this buttons lead to
    /// </summary>
    /// <param name="subMenu">the submenu to be set</param>
    public void Initialize(UISubMenu subMenu) {
        this.subMenu = subMenu;
    }

    /// <summary>
    /// Set the button to correspond the appropriate type it represents
    /// </summary>
    /// <param name="type"></param>
    public void SetType(PipesSettingsManager.PipeType type) {
        pipeType = type;
        Button button = this.GetComponent<Button>();
        SetName(button, type);
        button.onClick.AddListener(OpenMenu);
    }

    /// <summary>
    /// Opens the menu
    /// </summary>
    public void OpenMenu()
    {
        subMenu.Initialize(pipeType);
        subMenu.gameObject.SetActive(true);
    }

 
    /// <summary>
    /// Sets name for a button based on the PipeType it represents
    /// </summary>
    /// <param name="button">the button to be named</param>
    /// <param name="type">the type to be used to name it</param>
    private void SetName(Button button, PipesSettingsManager.PipeType type)
    {
        TMP_Text text = button.transform.Find("Name").GetComponent<TMP_Text>();
        switch (type)
        {
            case PipesSettingsManager.PipeType.Kanalizace:
                text.text = "Kanalizace";
                break;
            case PipesSettingsManager.PipeType.Plyn:
                text.text = "Plynovod";
                break;

            case PipesSettingsManager.PipeType.SilnoProud:
                text.text = "Silnoproud";
                break;
            case PipesSettingsManager.PipeType.SlaboProud:
                text.text = "Slaboproud";
                break;
            case PipesSettingsManager.PipeType.Teplo:
                text.text = "Teplovod";
                break;
            case PipesSettingsManager.PipeType.Voda:
                text.text = "Vodovod";
                break;
        }
    }

}
