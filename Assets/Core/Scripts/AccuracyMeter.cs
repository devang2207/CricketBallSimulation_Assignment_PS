using UnityEngine;
using UnityEngine.UI;

public class AccuracyMeter : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public RectTransform bar;        // the colored bar background
    public RectTransform indicator;  // the moving line

    [Header("Settings")]
    public float speed = 1.5f;

    bool  oscillating   = true;
    float normalizedPos = 0f;
    float barHeight;

    void Start()
    {
        barHeight = bar.rect.height;
    }

    void Update()
    {
        if (oscillating)
        {
            normalizedPos = Mathf.PingPong(Time.time * speed, 1f);
            UpdateIndicator();
        }
    }

    public float LockAndGetAccuracy()
    {
        oscillating = false;
        return GetAccuracy();
    }

    public void ResetMeter()
    {
        oscillating = true;
    }

    float GetAccuracy()
    {
        float distFromCenter = Mathf.Abs(normalizedPos - 0.5f) * 2f;
        return Mathf.Clamp01(1f - distFromCenter);
    }

    void UpdateIndicator()
    {
        if (indicator == null || bar == null) return;
        float y = Mathf.Lerp(-barHeight * 0.5f, barHeight * 0.5f, normalizedPos);
        indicator.anchoredPosition = new Vector2(indicator.anchoredPosition.x, y);
    }
}
