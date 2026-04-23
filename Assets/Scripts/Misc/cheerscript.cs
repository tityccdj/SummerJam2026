using UnityEngine;

public class cheerscript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [Header("Movement Settings")]
    [SerializeField] private float bounceHeight = 0.2f;   // ความสูงที่กระโดด
    [SerializeField] private float bounceSpeed = 5f;    // ความเร็วในการขยับ

    [Header("Squash & Stretch")]
    [SerializeField] private bool useSquash = true;     // เปิด/ปิด การยืดหดตัว
    [SerializeField] private float squashAmount = 0.1f; // ความมากน้อยของการบี้ตัว

    private Vector3 initialPosition;
    private Vector3 initialScale;
    private float randomOffset;

    void Start()
    {
        initialPosition = transform.localPosition;
        initialScale = transform.localScale;

        //  หัวใจสำคัญ: สุ่มค่าเริ่มต้นเพื่อให้กองเชียร์แต่ละตัวขยับไม่พร้อมกัน
        randomOffset = Random.Range(0f, Mathf.PI * 2f);

        // สุ่มความเร็วเล็กน้อยเพื่อให้ดูเป็นธรรมชาติมากขึ้น
        bounceSpeed *= Random.Range(0.85f, 1.15f);
    }

    void Update()
    {
        // คำนวณค่า Sine Wave ตามเวลา + ค่าสุ่ม
        float time = (Time.time * bounceSpeed) + randomOffset;
        float wave = Mathf.Sin(time); // ค่าจะวิ่งระหว่าง -1 ถึง 1
        float positiveWave = (wave + 1f) * 0.5f; // แปลงเป็น 0 ถึง 1

        // 1. การกระโดด (Up/Down)
        // ใช้ wave ปกติเพื่อให้มีการขยับขึ้นลงจากจุดกลาง
        transform.localPosition = initialPosition + Vector3.up * positiveWave * bounceHeight;

        // 2. การยืดหด (Squash & Stretch)
        if (useSquash)
        {
            // เวลาตัวต่ำลงให้ตัวบี้ออกข้าง (X กว้างขึ้น Y เตี้ยลง)
            // เวลาตัวลอยขึ้นให้ตัวยืด (X แคบลง Y สูงขึ้น)
            float squashX = initialScale.x + (wave * squashAmount);
            float squashY = initialScale.y - (wave * squashAmount);

            transform.localScale = new Vector3(squashX, squashY, initialScale.z);
        }
    }
}
