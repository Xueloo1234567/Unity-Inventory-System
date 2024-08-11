using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 存放在背包里的格子，用于管理InventoryItem
/// 功能包括：被选择，被分配，同类物品成堆堆放，数字键放入快捷栏
/// </summary>

public class Slot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    [SerializeField] Image image;
    [SerializeField] Color selectColor, nonSelectColor, highlightColor;

    public InventoryType Type;



    private void Awake() => DeSelect();
    private void OnDisable() => DeSelect();
    public void Select() => image.color = selectColor;
    public void DeSelect() => image.color = nonSelectColor;



    public void OnPointerEnter(PointerEventData eventData)
    {
        InventorySystem.Instance.enterSlot = this;
        image.color = highlightColor;

        InventoryItem inventoryItem = InventorySystem.Instance.dragItem;
        InventoryItem itemInSlot = GetItemInSlot();

        //拖拽物品不为空且处于[分配]状态，以及当前Slot为空
        if (inventoryItem != null && inventoryItem.assignState 
            && itemInSlot == null)
        {
            if (InventorySystem.pressRight) //右键单个分配
            {
                inventoryItem.AssignOne(this);
            }
            else if (InventorySystem.pressLeft) //左键平均分配
            {
                inventoryItem.Assign(this);
            }
        }
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        InventorySystem.Instance.enterSlot = null;
        image.color = nonSelectColor;
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        InventoryItem inventoryItem = InventorySystem.Instance.dragItem;
        InventoryItem itemInSlot = GetItemInSlot();

        if (inventoryItem != null)
        {
            if (itemInSlot != null)
            {
                if (Input.GetKey(KeyCode.LeftShift)) //同类物品成堆堆放
                {
                    //实现0.2秒内的 [正在拖拽物品 + LeftShift + 双击其他物品]
                    float currentTimer = inventoryItem.leftShiftDoubleClickTimer;
                    if (currentTimer > 0) 
                    {
                        InventorySystem.Instance.OnLeftShiftDoubleClick(itemInSlot.Item, Type);
                    }
                    else inventoryItem.leftShiftDoubleClickTimer = 0.2f;

                    return; //有按LeftShift的话就不执行下面的判断了
                }

                InventorySystem.OnItemRaycast(true); //优先恢复所有inventoryItem的raycast

                if (itemInSlot.Item != inventoryItem.Item) //不是同类物品，交换
                {
                    inventoryItem.EndDrag(transform);
                    itemInSlot.StartDrag();
                }
                else //是同类物品
                {
                    //相加后小于堆叠
                    if (itemInSlot.Count + inventoryItem.Count <= itemInSlot.Item.stackCount)
                    {
                        itemInSlot.Count += inventoryItem.Count;
                        inventoryItem.Delete();
                    }
                    //相加后大于堆叠
                    else
                    {
                        int dif = inventoryItem.Item.stackCount - itemInSlot.Count;

                        itemInSlot.Count += dif;
                        inventoryItem.Count -= dif;

                        inventoryItem.EndDrag(InventorySystem.Instance.canvas);
                        inventoryItem.StartDrag();
                    }
                }
            }
            else
            {
                if (Input.GetMouseButtonDown(0)) //双击判断
                {
                    if (inventoryItem.doubleClickTimer > 0)
                    {
                        InventorySystem.Instance.OnDoubleClick(inventoryItem, 0);
                    }
                }

                StartCoroutine(Assign());
                //携程是为了在InventorySystem更新左右键检测后再判断
                IEnumerator Assign()
                {
                    yield return null;
                    inventoryItem.assignState = true;
                    inventoryItem.CreateNewAssign();

                    bool one = InventorySystem.pressRight;
                    if (one)
                    {
                        inventoryItem.AssignOne(this);
                    }
                    else
                    {
                        inventoryItem.Assign(this);
                    }
                }
            }
        }
    }



    public InventoryItem GetItemInSlot()
    {
        return GetComponentInChildren<InventoryItem>();
    }
}
