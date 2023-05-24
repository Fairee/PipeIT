using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UISubMenu : MonoBehaviour
{

    //To get the amount of buttons to be generated and to save the settings
    PipesSettingsManager pipesStettingManager;

    PipesSettingsManager.PipeType pipeType;

    [SerializeField]
    Button prefabSwitcher;
    [SerializeField]
    Button closeButton;

    [SerializeField]
    UIColorChangerScreen colorChangerScreen;

    List<Button> buttons;
    /// <summary>
    /// Intializes the submenu
    /// </summary>
    /// <param name="pipe">The type for which the submenu is created</param>
    public void Initialize(PipesSettingsManager.PipeType pipe) {
        buttons = new List<Button>();
        pipeType = pipe;
        pipesStettingManager = PipesSettingsManager.Instance;
        closeButton.onClick.AddListener(Close);
        CreateButtons();
    }
    /// <summary>
    /// Closes the menu, destroyes all the buttons and saves the settings
    /// </summary>
    private void Close() {
        foreach (Button but in buttons) {
            Destroy(but.gameObject);
        }
        buttons.Clear();
        pipesStettingManager.SaveSwitches();
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Create all the subtypes buttons
    /// </summary>
    private void CreateButtons()
    {
        float positionY = 0;
        Button button;
        foreach (PipesSettingsManager.SubType type in pipesStettingManager.GetSubTypes(pipeType))
        {

            button = Instantiate(prefabSwitcher, this.transform);
            ButtonSwitch butswitch = button.GetComponent<ButtonSwitch>();
            butswitch.Initialize(colorChangerScreen);
            butswitch.SetType(type);
            buttons.Add(button);

            RectTransform buttonRect = button.GetComponent<RectTransform>();
            buttonRect.anchoredPosition = buttonRect.anchoredPosition + new Vector2(0, positionY);
            positionY -= 160;

        }

    }

}
