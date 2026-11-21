using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ProgressBarController : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Komponen Slider bar (Interactable dimatikan)")]
    public Slider slider;
    [Tooltip("Gambar Fill di dalam Slider (untuk diwarnai)")]
    public Image fillImage;
    public TMP_Text label;
    [Tooltip("Garis penanda ambang 50% (anak dari objek Slider)")]
    public RectTransform thresholdMarker;
    [Tooltip("Teks kecil +10 / -5 yang muncul sebentar. Boleh dikosongkan.")]
    public TMP_Text deltaText;

    [Header("Animasi")]
    [Tooltip("Kecepatan isi bar (persen per detik)")]
    public float fillSpeed = 40f;
    public float colorSmooth = 8f;

    [Header("Warna")]
    public Color redColor = new Color(0.86f, 0.2f, 0.2f);
    public Color yellowColor = new Color(0.97f, 0.78f, 0.15f);
    public Color greenColor = new Color(0.25f, 0.75f, 0.35f);
    [Tooltip("Di bawah nilai ini bar berwarna merah (bahaya)")]
    public float dangerBelow = 20f;

    float display;
    float target;
    float maxValue = 100f;
    float threshold = 50f;
    Color currentColor;
    Coroutine deltaRoutine;

    void OnEnable()
    {
        GameManager.OnBarChanged += HandleBarChanged;
    }

    void OnDisable()
    {
        GameManager.OnBarChanged -= HandleBarChanged;
    }

    void Start()
    {
        GameManager gm = GameManager.Instance;
        if (gm != null)
        {
            maxValue = gm.maxBar;
            threshold = gm.orationThreshold;
            target = display = gm.Bar;
        }

        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = maxValue;
            slider.value = display;
        }

        PlaceMarker();
        currentColor = ColorFor(display);
        if (fillImage != null) fillImage.color = currentColor;

        if (deltaText != null) deltaText.alpha = 0f;
        UpdateLabel();
    }

    void Update()
    {
        // Isi bar bergerak halus menuju nilai target
        display = Mathf.MoveTowards(display, target, fillSpeed * Time.unscaledDeltaTime);
        if (slider != null) slider.value = display;

        // Warna berubah halus
        currentColor = Color.Lerp(currentColor, ColorFor(display), colorSmooth * Time.unscaledDeltaTime);
        if (fillImage != null) fillImage.color = currentColor;

        UpdateLabel();
    }

    void HandleBarChanged(int value, int delta)
    {
        target = value;
        ShowDelta(delta);
    }

    Color ColorFor(float v)
    {
        if (v < dangerBelow) return redColor;
        if (v < threshold) return yellowColor;
        return greenColor;
    }

    void UpdateLabel()
    {
        if (label != null)
            label.text = "Bar Progres Aksi: " + Mathf.RoundToInt(display) + "%";
    }

    // Letakkan garis penanda di posisi ambang orasi pada bar
    void PlaceMarker()
    {
        if (thresholdMarker == null) return;
        float t = Mathf.Clamp01(threshold / maxValue);
        thresholdMarker.anchorMin = new Vector2(t, 0.5f);
        thresholdMarker.anchorMax = new Vector2(t, 0.5f);
        thresholdMarker.anchoredPosition = Vector2.zero;
    }

    void ShowDelta(int delta)
    {
        if (deltaText == null) return;
        if (deltaRoutine != null) StopCoroutine(deltaRoutine);
        deltaRoutine = StartCoroutine(DeltaRoutine(delta));
    }

    IEnumerator DeltaRoutine(int delta)
    {
        Color c = delta > 0 ? greenColor : redColor;
        deltaText.text = (delta > 0 ? "+" : "") + delta;

        // Tahan sebentar, lalu pudar
        c.a = 1f;
        deltaText.color = c;
        yield return new WaitForSecondsRealtime(0.8f);

        float t = 0f;
        while (t < 0.7f)
        {
            t += Time.unscaledDeltaTime;
            c.a = 1f - Mathf.Clamp01(t / 0.7f);
            deltaText.color = c;
            yield return null;
        }
        deltaText.alpha = 0f;
    }
}
