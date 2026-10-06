using System;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;

public class PlayerIdentityManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private Button startGameButton;
    [SerializeField] private TMP_Text statusText;

    [Header("Panels Navigation (Optional)")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject roomBrowserPanel;

    [Header("Scripts References")]
    [SerializeField] private LobbyManager lobbyManager; 

    private const string PLAYER_NAME_KEY = "SavedPlayerName";
    public static string LocalPlayerName { get; private set; }

    private void Start()
    {
        if (PlayerPrefs.HasKey(PLAYER_NAME_KEY))
        {
            string savedName = PlayerPrefs.GetString(PLAYER_NAME_KEY);
            nameInputField.text = savedName;
        }

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

        startGameButton.interactable = false;
        UpdateStatus("Connecting to Unity services...");

        PlayerPrefs.SetString(PLAYER_NAME_KEY, inputName);
        PlayerPrefs.Save();
        LocalPlayerName = inputName;

        bool success = await InitializeAndSignInAsync(inputName);

        if (success)
        {
            UpdateStatus("Successfully signed in!");
            OpenRoomBrowser();
        }
        else
        {
            UpdateStatus("Connection failed! Check your internet connection and try again.");
            startGameButton.interactable = true;
        }
    }

    private async Task<bool> InitializeAndSignInAsync(string rawName)
    {
        try
        {
            InitializationOptions options = new InitializationOptions();

            string cleanProfileName = Regex.Replace(rawName, "[^a-zA-Z0-9_-]", "");
            if (string.IsNullOrEmpty(cleanProfileName)) cleanProfileName = "Player";
            if (cleanProfileName.Length > 20) cleanProfileName = cleanProfileName.Substring(0, 20);

#if UNITY_EDITOR
            cleanProfileName += "_" + UnityEngine.Random.Range(1000, 9999);
#endif

            options.SetProfile(cleanProfileName);

            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                await UnityServices.InitializeAsync(options);
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                await AuthenticationService.Instance.UpdatePlayerNameAsync(LocalPlayerName);
            }

            Debug.Log($"[Auth Success] Profile: {cleanProfileName} | Player ID: {AuthenticationService.Instance.PlayerId}");
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
        if (loginPanel != null) loginPanel.SetActive(false);
        if (roomBrowserPanel != null) roomBrowserPanel.SetActive(true);

        if (lobbyManager != null)
        {
            lobbyManager.RefreshLobbyList();
        }
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }
}