using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    /// <summary>
    /// 当鼠标处于带有UIInterfere组件的对象上时
    /// </summary>
    public static bool ui_inRange;
    /// <summary>
    /// 鼠标下带有UIInterfere组件的对象
    /// </summary>
    public static GameObject ui_touchObj;




    private void Awake() => Instance = this;
}
