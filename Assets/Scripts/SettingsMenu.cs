using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    private const string VOLUME_KEY = "Settings.Volume";
    private const string MUTE_KEY = "Settings.Mute";
    private const string FULLSCREEN_KEY = "Settings.Fullscreen";
    private const string RESOLUTION_INDEX_KEY = "Settings.ResolutionIndex";
    private const float MIN_AUDIBLE_VOLUME = 0.0001f;

    [Header("Panels")]
    [SerializeField] private GameObject settingsRoot;
    [SerializeField] private GameObject generalTab;
    [SerializeField] private GameObject controlsTab;
    [SerializeField] private bool openSettingsOnStart = false;

    [Header("Audio")]
    [SerializeField] private string masterBusPath = "bus:/";
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Toggle muteToggle;

    [Header("Display")]
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private TMP_Dropdown resolutionDropdown;

    [Header("Navigation")]
    [SerializeField] private string mainMenuSceneName = "StartMenu";

    private readonly List<Resolution> availableResolutions = new List<Resolution>();
    private Bus masterBus;
    private bool isApplyingSettings = false;

    private void Awake()
    {
        masterBus = RuntimeManager.GetBus(masterBusPath);
    }

    private void Start()
    {
        PopulateResolutionDropdown();
        BindUiEvents();
        LoadSavedSettings();

        if (openSettingsOnStart)
        {
            OpenSettings();
        }
        else
        {
            ShowGeneralTab();
        }
    }

    public void OpenSettings()
    {
        if (settingsRoot != null)
        {
            settingsRoot.SetActive(true);
        }

        LoadSavedSettings();
        ShowGeneralTab();
    }

    public void CloseSettings()
    {
        if (settingsRoot != null)
        {
            settingsRoot.SetActive(false);
        }
    }

    public void ShowGeneralTab()
    {
        SetTabState(true);
    }

    public void ShowControlsTab()
    {
        SetTabState(false);
    }

    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName, LoadSceneMode.Single);
    }

    private void SetTabState(bool showGeneral)
    {
        if (generalTab != null)
        {
            generalTab.SetActive(showGeneral);
        }

        if (controlsTab != null)
        {
            controlsTab.SetActive(!showGeneral);
        }
    }

    private void BindUiEvents()
    {
        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.AddListener(HandleVolumeChanged);
        }

        if (muteToggle != null)
        {
            muteToggle.onValueChanged.AddListener(HandleMuteChanged);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.AddListener(HandleFullscreenChanged);
        }

        if (resolutionDropdown != null)
        {
            resolutionDropdown.onValueChanged.AddListener(HandleResolutionChanged);
        }
    }

    private void LoadSavedSettings()
    {
        isApplyingSettings = true;

        float volume = PlayerPrefs.GetFloat(VOLUME_KEY, 0.7f);
        bool isMuted = PlayerPrefs.GetInt(MUTE_KEY, 0) == 1;
        bool isFullscreen = PlayerPrefs.GetInt(FULLSCREEN_KEY, Screen.fullScreen ? 1 : 0) == 1;
        int resolutionIndex = PlayerPrefs.GetInt(RESOLUTION_INDEX_KEY, GetCurrentResolutionIndex());

        if (volumeSlider != null)
        {
            volumeSlider.SetValueWithoutNotify(volume);
        }

        if (muteToggle != null)
        {
            muteToggle.SetIsOnWithoutNotify(isMuted);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.SetIsOnWithoutNotify(isFullscreen);
        }

        if (resolutionDropdown != null && availableResolutions.Count > 0)
        {
            resolutionIndex = Mathf.Clamp(resolutionIndex, 0, availableResolutions.Count - 1);
            resolutionDropdown.SetValueWithoutNotify(resolutionIndex);
        }

        ApplyVolume(volume);
        ApplyMute(isMuted);
        ApplyFullscreen(isFullscreen);

        if (availableResolutions.Count > 0)
        {
            ApplyResolution(Mathf.Clamp(resolutionIndex, 0, availableResolutions.Count - 1));
        }

        isApplyingSettings = false;
    }

    private void PopulateResolutionDropdown()
    {
        if (resolutionDropdown == null)
        {
            return;
        }

        resolutionDropdown.ClearOptions();
        availableResolutions.Clear();

        Resolution[] screenResolutions = Screen.resolutions;
        HashSet<string> seenResolutions = new HashSet<string>();
        List<string> resolutionOptions = new List<string>();

        for (int i = 0; i < screenResolutions.Length; i++)
        {
            Resolution resolution = screenResolutions[i];
            string resolutionKey = $"{resolution.width}x{resolution.height}";
            if (seenResolutions.Contains(resolutionKey))
            {
                continue;
            }

            seenResolutions.Add(resolutionKey);
            availableResolutions.Add(resolution);
            resolutionOptions.Add($"{resolution.width} x {resolution.height}");
        }

        resolutionDropdown.AddOptions(resolutionOptions);
    }

    private int GetCurrentResolutionIndex()
    {
        for (int i = 0; i < availableResolutions.Count; i++)
        {
            Resolution resolution = availableResolutions[i];
            if (resolution.width == Screen.currentResolution.width &&
                resolution.height == Screen.currentResolution.height)
            {
                return i;
            }
        }

        return availableResolutions.Count - 1;
    }

    private void HandleVolumeChanged(float volume)
    {
        if (isApplyingSettings)
        {
            return;
        }

        ApplyVolume(volume);
        PlayerPrefs.SetFloat(VOLUME_KEY, volume);
        PlayerPrefs.Save();
    }

    private void HandleMuteChanged(bool isMuted)
    {
        if (isApplyingSettings)
        {
            return;
        }

        ApplyMute(isMuted);
        PlayerPrefs.SetInt(MUTE_KEY, isMuted ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void HandleFullscreenChanged(bool isFullscreen)
    {
        if (isApplyingSettings)
        {
            return;
        }

        ApplyFullscreen(isFullscreen);
        PlayerPrefs.SetInt(FULLSCREEN_KEY, isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void HandleResolutionChanged(int resolutionIndex)
    {
        if (isApplyingSettings)
        {
            return;
        }

        ApplyResolution(resolutionIndex);
        PlayerPrefs.SetInt(RESOLUTION_INDEX_KEY, resolutionIndex);
        PlayerPrefs.Save();
    }

    private void ApplyVolume(float volume)
    {
        float clampedVolume = Mathf.Clamp01(volume);
        float busVolume = clampedVolume <= 0.0f ? 0.0f : Mathf.Max(MIN_AUDIBLE_VOLUME, clampedVolume * clampedVolume);

        if (masterBus.isValid())
        {
            masterBus.setVolume(busVolume);
        }

        if (MusicManager.instance != null)
        {
            MusicManager.instance.SetVolume(clampedVolume);
        }

        if (BossMusicManager.instance != null)
        {
            BossMusicManager.instance.SetVolume(clampedVolume);
        }
    }

    private void ApplyMute(bool isMuted)
    {
        if (masterBus.isValid())
        {
            masterBus.setMute(isMuted);
        }
    }

    private void ApplyFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }

    private void ApplyResolution(int resolutionIndex)
    {
        if (resolutionIndex < 0 || resolutionIndex >= availableResolutions.Count)
        {
            return;
        }

        Resolution resolution = availableResolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
    }
}
