using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 检测鼠标的进入和退出，将信息提交给UIManager
/// </summary>

public class UIInterfere : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        UIManager.ui_inRange = true;
        UIManager.ui_touchObj = gameObject;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UIManager.ui_inRange = false;
        UIManager.ui_touchObj = null;
    }

    private void OnDisable()
    {
        if (UIManager.ui_touchObj == gameObject)
        {
            UIManager.ui_touchObj = null;
        }
    }
}
