using UnityEngine;

public enum TransformType
{
    UI_Center, Object_Center, Custom_Center
}

public class ReturnToPos : MonoBehaviour
{
    [SerializeField] TransformType type;
    [SerializeField] Vector2 customPos;

    private void Awake()
    {
        switch (type)
        {
            case TransformType.UI_Center:
                transform.position = new Vector2(Screen.width/2, Screen.height/2); break;

        }
    }
}
