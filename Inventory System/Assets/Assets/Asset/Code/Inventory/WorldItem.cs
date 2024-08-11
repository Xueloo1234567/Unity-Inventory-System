using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 这只是一个WorldItem的示例，只是简单实现了左右键拾取和消除，以及初始化
/// </summary>

public class WorldItem : MonoBehaviour, IPointerDownHandler, IPointerExitHandler, IPointerEnterHandler
{
    [SerializeField] Image icon;
    [SerializeField] GameObject hint;

    public Item item;
    public int count;




    private void Start() => icon.sprite = item.icon;



    public void InitialItem(Item item, int count)
    {
        this.item = item;
        this.count = count;
    }



    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left) //拾取
        {
            Destroy(gameObject);
            InventorySystem.AddItem(item, count);
            InventorySystem.Instance.RandomSpawnItemInWorld();
        }
        else if(eventData.button == PointerEventData.InputButton.Right) //消除
        {
            Destroy(gameObject);
        }
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        hint.SetActive(true);
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        hint.SetActive(false);
    }
}
