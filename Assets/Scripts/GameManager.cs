using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // (nilai bar baru, perubahan yang diminta) -> dipakai UI bar
    public static event Action<int, int> OnBarChanged;
    // true = bar >= ambang orasi, false = turun lagi di bawah ambang
    public static event Action<bool> OnOrationUnlockChanged;
    // Dipanggil saat layar Game Over tampil
    public static event Action OnGameOver;

    [Header("Bar Progres Aksi (0-100)")]
    public int startBar = 30;
    public int maxBar = 100;
    [Tooltip("Bar minimal agar orasi di Bab 4 terbuka")]
    public int orationThreshold = 50;

    [Header("UI Game Over")]
    public GameObject gameOverPanel;
    public Button restartButton;

    [Header("Referensi Pemain (untuk mengunci kontrol saat Game Over)")]
    public PlayerController player;
    public InteractionSystem interaction;
    public ThirdPersonCameraRig cameraRig;

    [Header("Debug (hapus/matikan sebelum build final)")]
    [Tooltip("F2 = +10, F3 = -10 untuk tes cepat")]
    public bool debugKeys = true;

    public int Bar { get; private set; }
    public bool IsGameOver { get; private set; }
    public bool OrationUnlocked => Bar >= orationThreshold;

    bool pendingGameOver;

    void Awake()
    {
        Instance = this;
        Bar = Mathf.Clamp(startBar, 0, maxBar);
        IsGameOver = false;
        pendingGameOver = false;
        Time.timeScale = 1f;

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (restartButton != null) restartButton.onClick.AddListener(Restart);
    }

    void OnEnable()
    {
        DialogManager.OnBarChange += AddBar;
        DialogManager.OnDialogEnded += HandleDialogEnded;
    }

    void OnDisable()
    {
        DialogManager.OnBarChange -= AddBar;
        DialogManager.OnDialogEnded -= HandleDialogEnded;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (!debugKeys || IsGameOver) return;
        Keyboard kb = Keyboard.current;
        if (kb == null) return;
        if (kb.f2Key.wasPressedThisFrame) AddBar(10);
        if (kb.f3Key.wasPressedThisFrame) AddBar(-10);
    }

    // Tambah/kurangi bar. Bisa dipanggil dari sistem lain (misi, orasi, dll).
    public void AddBar(int amount)
    {
        if (IsGameOver || amount == 0) return;

        bool wasUnlocked = OrationUnlocked;
        Bar = Mathf.Clamp(Bar + amount, 0, maxBar);
        Debug.Log("[GameManager] Bar = " + Bar + "% (" + (amount > 0 ? "+" : "") + amount + ")");

        OnBarChanged?.Invoke(Bar, amount);

        if (OrationUnlocked != wasUnlocked)
            OnOrationUnlockChanged?.Invoke(OrationUnlocked);

        if (Bar <= 0)
        {
            IsGameOver = true;
            // Jika sedang dialog, tunggu dialog selesai dulu supaya tidak bentrok
            if (DialogManager.IsOpen) pendingGameOver = true;
            else ShowGameOver();
        }
    }

    void HandleDialogEnded(DialogData finished)
    {
        if (pendingGameOver)
        {
            pendingGameOver = false;
            ShowGameOver();
        }
    }

    void ShowGameOver()
    {
        Debug.Log("[GameManager] GAME OVER");

        // Kunci kontrol & bebaskan cursor agar tombol bisa diklik
        if (player != null) player.inputEnabled = false;
        if (interaction != null) interaction.interactionEnabled = false;
        if (cameraRig != null) cameraRig.SetLookEnabled(false);

        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        OnGameOver?.Invoke();
    }

    // Sementara: muat ulang scene dari awal.
    // Tahap 11 nanti diganti: muat dari checkpoint terakhir.
    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
