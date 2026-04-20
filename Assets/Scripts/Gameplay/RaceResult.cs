using System.Collections.Generic;
using UnityEngine;

public class RaceResult : MonoBehaviour
{
    public class RunnerResult
    {
        public string Name;
        public int Rank;
        public float FinishTime;
        public bool IsHuman;
        public bool HasCharacter; // เอาไว้เช็กว่าตัวนี้มีสคริปต์ Character ไหม
        public int HairIndex;
        public int FaceIndex;
        public int ClothIndex;
        public Color HairColor;
        public Color FallbackColor; // เอาไว้เก็บสี CPU กรณีที่ดึง Prefab ไม่ติด (กลายเป็นสี่เหลี่ยม)
    }

    // 2. สร้างกล่องลอยฟ้า (static) เพื่อเก็บรายชื่อนักแข่งทั้งหมด
    public static class RaceResultData
    {
        public static List<RunnerResult> FinalResults = new List<RunnerResult>();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
