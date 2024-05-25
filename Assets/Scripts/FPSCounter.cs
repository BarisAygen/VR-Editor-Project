using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FPSCounter : MonoBehaviour
{
    private TextMeshProUGUI fpsText;
    [SerializeField] private float hudRefreshRate = 1f;
    private int fps;
    private float timer;

    private void Start()
    {
        fpsText = GetComponent<TextMeshProUGUI>();
    }

    private void Update()
    {
        if (Time.unscaledTime > timer)
        {
            fps = (int)(1f / Time.unscaledDeltaTime);
            fpsText.text = fps.ToString();
            timer = Time.unscaledTime + hudRefreshRate;
        }
    }
}
