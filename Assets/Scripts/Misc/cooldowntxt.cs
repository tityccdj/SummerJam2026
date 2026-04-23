using TMPro;
using UnityEngine;

public class cooldowntxt : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private TextMeshProUGUI textComponent;

    [Header("Settings")]
    public float minSize = 30f;
    public float maxSize = 40f;
    public float speed = 15f; // ความเร็วในการขยับ

    void Start()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        if (textComponent != null)
        {
            // สูตร: ค่าต่ำสุด + PingPong(เวลา * ความเร็ว, ระยะห่างจากต่ำสุดไปสูงสุด)
            float size = minSize + Mathf.PingPong(Time.time * speed, maxSize - minSize);
            textComponent.fontSize = size;
        }
    }
}
