using UnityEngine;
using UnityEngine.UI;

public class CustomCursor : MonoBehaviour
{
    [SerializeField] Image sr;
    [SerializeField] Sprite normal;
    [SerializeField] Sprite rightClick;

    private void Start()
    {
        Cursor.visible = false;
    }
    private void Update()
    {
        transform.position = Input.mousePosition;
        
        if (Input.GetMouseButtonDown(1))
        {
            sr.sprite = rightClick;
        }
        else if( Input.GetMouseButtonUp(1))
        {
            sr.sprite = normal;
        }
    }
}
