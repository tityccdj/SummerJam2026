using UnityEngine;

public class UIHidePC : MonoBehaviour
{
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        bool showMobileUI = Utility.IsMobile();
        gameObject.SetActive(showMobileUI);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
