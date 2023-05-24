using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Takes care of the Main menu functionality
/// </summary>
public class UIMainMenu : MonoBehaviour
{
    // to save the settings when the menu is exited
    public PipesSettingsManager pipesSettingManager;
    [SerializeField]
    public Button prefabMenu;
    [SerializeField]
    public Button prefabChange;
    [SerializeField]
    private UISubMenu submenu;
    [SerializeField]
    private Button quitButton;

    [SerializeField]
    UIColorChangerScreen colorChanger;

    private List<Button> buttons;
    private void Start()
    {
        buttons = new List<Button>();
        pipesSettingManager = PipesSettingsManager.Instance;
        quitButton.onClick.AddListener(Quit);
        CreateButtons();
    }
    /// <summary>
    /// Saves the settings and quit the menu
    /// </summary>
    private void Quit() {
        gameObject.SetActive(false);
        pipesSettingManager.SaveSwitches();
    }

    /// <summary>
    /// Generate the buttons
    /// </summary>
    private void CreateButtons() {
        float positionY = 0;
        Button button;
        foreach (PipesSettingsManager.PipeType type in Enum.GetValues(typeof(PipesSettingsManager.PipeType))){
            //As we decided not to make a submenu for these, create the switch button for them instead
            if (type == PipesSettingsManager.PipeType.Kolektor || type == PipesSettingsManager.PipeType.Produkt || type == PipesSettingsManager.PipeType.Posta)
            {
                button = Instantiate(prefabChange, this.transform);
                ButtonSwitch butswitch = button.GetComponent<ButtonSwitch>();
                butswitch.Initialize(colorChanger);
                butswitch.SetType(type);
                
            }
            else {
                button = Instantiate(prefabMenu, this.transform);                
                ButtonMenu butmen = button.GetComponent<ButtonMenu>();
                butmen.Initialize(submenu);
                butmen.SetType(type);

            }

            RectTransform buttonRect = button.GetComponent<RectTransform>();
            buttonRect.anchoredPosition = buttonRect.anchoredPosition + new Vector2(0, positionY);
            buttons.Add(button);
            positionY -= 160;

        }
    
    }






}
