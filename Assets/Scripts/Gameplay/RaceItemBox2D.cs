using UnityEngine;

// ประกาศประเภทไอเทม
public enum RaceItemType
{
    None,
    Soda,   // วิ่งเร็วขึ้น
    Banana, // วางกับดัก (Overheat)
    Ice,     // น้ำแข็ง ล้างความร้อน
    Sun,Gun,Weight
}

public class RaceItemBox2D : MonoBehaviour
{
    [SerializeField] private float respawnTime = 10f; // เกิดใหม่ทุก 10 วินาที
    [SerializeField] private float triggerRadius = 0.5f;

    private bool isAvailable = true;
    private SpriteRenderer spriteRenderer;

    public bool IsAvailable => isAvailable;
    public float TriggerRadius => triggerRadius;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Collect(PlayerSplineRunner runner)
    {
        if (!isAvailable) return;

        // สุ่มไอเทม 1 ถึง 3 (1=Soda, 2=Banana, 3=Ice)
        int itemTotal = System.Enum.GetValues(typeof(RaceItemType)).Length;
        RaceItemType randomItem = (RaceItemType)Random.Range(1, itemTotal);

        // ลองส่งไอเทมให้คนเล่น ถ้าเขามือว่าง (ReceiveItem คืนค่า true) กล่องถึงจะหายไป
        if (runner.ReceiveItem(randomItem))
        {
            isAvailable = false;
            if (spriteRenderer != null) spriteRenderer.enabled = false;
            
            // สั่งให้กล่องกลับมาใหม่ในอีก 10 วินาที
            Invoke(nameof(Respawn), respawnTime);
        }
    }

    private void Respawn()
    {
        isAvailable = true;
        if (spriteRenderer != null) spriteRenderer.enabled = true;
    }
}