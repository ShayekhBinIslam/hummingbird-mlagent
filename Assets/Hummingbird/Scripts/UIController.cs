using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls a very simple UI. Doesn't do anything on its own.
/// </summary>
public class UIController : MonoBehaviour
{
    [Tooltip("The nectar progress bar for the player")]
    public Slider playerNectarBar;

    [Tooltip("The nectar progress bar for the opponent")]
    public Slider opponentNectarBar;

    [Tooltip("The timer text")]
    public TextMeshProUGUI timerText;

    [Tooltip("The banner text")]
    public TextMeshProUGUI bannerText;

    [Tooltip("The button")]
    public Button button;

    [Tooltip("The button text")]
    public TextMeshProUGUI buttonText;

    /// <summary>
    /// Delegate for a button click
    /// </summary>
    public delegate void ButtonClick();

    /// <summary>
    /// Called when the button is clicked
    /// </summary>
    public ButtonClick OnButtonClicked;

    /// <summary>
    /// Responds to button clicks
    /// </summary>
    public void ButtonClicked()
    {
        if (OnButtonClicked != null) OnButtonClicked();
    }

    /// <summary>
    /// Shows the button
    /// </summary>
    /// <param name="text">The text string on the button</param>
    public void ShowButton(string text)
    {
        buttonText.text = text;
        button.gameObject.SetActive(true);
    }

    /// <summary>
    /// Hides the button
    /// </summary>
    public void HideButton()
    {
        button.gameObject.SetActive(false);
    }

    /// <summary>
    /// Shows banner text
    /// </summary>
    /// <param name="text">The text string to show</param>
    public void ShowBanner(string text)
    {
        bannerText.text = text;
        bannerText.gameObject.SetActive(true);
    }

    /// <summary>
    /// Hides the banner text
    /// </summary>
    public void HideBanner()
    {
        bannerText.gameObject.SetActive(false);
    }

    /// <summary>
    /// Sets the timer, if timeRemaining is negative, hides the text
    /// </summary>
    /// <param name="timeRemaining">The time remaining in seconds</param>
    public void SetTimer(float timeRemaining)
    {
        if (timeRemaining > 0f)
            timerText.text = timeRemaining.ToString("00");
        else
            timerText.text = "";
    }

    /// <summary>
    /// Sets the player's nectar amount
    /// </summary>
    /// <param name="nectarAmount">An amount between 0 and 1</param>
    public void SetPlayerNectar(float nectarAmount)
    {
        playerNectarBar.value = nectarAmount;
    }

    /// <summary>
    /// Sets the opponent's nectar amount
    /// </summary>
    /// <param name="nectarAmount">An amount between 0 and 1</param>
    public void SetOpponentNectar(float nectarAmount)
    {
        opponentNectarBar.value = nectarAmount;
    }
}
