using System;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.UI;

public class PlayerIdentityManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private Button startGameButton;
    [SerializeField] private TMP_Text statusText;

    [Header("Panels Navigation (Optional)")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject roomBrowserPanel;

    private const string PLAYER_NAME_KEY = "SavedPlayerName";
    public static string LocalPlayerName { get; private set; }

    private void Start()
    {
        // 1. Load the saved name if it exists
        if (PlayerPrefs.HasKey(PLAYER_NAME_KEY))
        {
            string savedName = PlayerPrefs.GetString(PLAYER_NAME_KEY);
            nameInputField.text = savedName;
        }

        // 2. Register the Start Game button listener
        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(OnStartGameClicked);
        }

        UpdateStatus("");
    }

    private async void OnStartGameClicked()
    {
        string inputName = nameInputField.text.Trim();

        if (string.IsNullOrEmpty(inputName))
        {
            UpdateStatus("Please enter your name first!");
            return;
        }

        // Disable the button to prevent multiple clicks
        startGameButton.interactable = false;
        UpdateStatus("Connecting to Unity services...");

        // Save the name locally
        PlayerPrefs.SetString(PLAYER_NAME_KEY, inputName);
        PlayerPrefs.Save();
        LocalPlayerName = inputName;

        // Sign in to Unity Services
        bool success = await InitializeAndSignInAsync();

        if (success)
        {
            UpdateStatus("Successfully signed in!");

            // Open the room browser panel (or load the next scene)
            OpenRoomBrowser();
        }
        else
        {
            UpdateStatus("Connection failed! Check your internet connection and try again.");
            startGameButton.interactable = true;
        }
    }

    private async Task<bool> InitializeAndSignInAsync()
    {
        try
        {
            // Initialize Unity Services
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                await UnityServices.InitializeAsync();
            }

            // Sign in anonymously if we are not already signed in
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

                // Set the player's name in Unity Authentication so other players can see it
                await AuthenticationService.Instance.UpdatePlayerNameAsync(LocalPlayerName);
            }

            Debug.Log($"[Auth Success] Player ID: {AuthenticationService.Instance.PlayerId}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Auth Error] Failed to sign in: {e.Message}");
            return false;
        }
    }

    private void OpenRoomBrowser()
    {
        // If you are using different panels in the same scene:
        if (loginPanel != null) loginPanel.SetActive(false);
        if (roomBrowserPanel != null) roomBrowserPanel.SetActive(true);

        // Note: If you are using scenes:
        // UnityEngine.SceneManagement.SceneManager.LoadScene("RoomBrowserScene");
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }
}

