using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Slot中存放的物品，可以被拖拽、堆叠、分裂等多种功能
/// 目前功能包括：拖拽，交换，堆叠，分裂，聚集，单例分配，平均分配
/// </summary>

public class InventoryItem : MonoBehaviour, IPointerDownHandler
{
    #region 公共
    /// <summary>
    /// 这个InventoryItem的持有物品
    /// </summary>
    public Item Item { get; set; }
    /// <summary>
    /// 每次更新时会自动更新UI
    /// </summary>
    public int Count { get => count; set { count = value; RefreshCount(); } }
    /// <summary>
    /// 物品被拖拽前所处的父对象
    /// </summary>
    public Transform parentBeforeDrag { get; private set; }
    /// <summary>
    /// [分配]状态，处于该状态下Slot对右键和左键有不同反应
    /// 拖拽物品时点击空Slot变为true
    /// </summary>
    public bool assignState { get; set; }
    /// <summary>
    /// 聚集物品的双击计时器，默认为0.2秒
    /// </summary>
    public float doubleClickTimer { get; set; }
    /// <summary>
    /// 物品成堆堆放的双击计时器，默认为0.2秒
    /// </summary>
    public float leftShiftDoubleClickTimer { get; set; }
    #endregion

    #region 局部
    private int count;
    /// <summary>
    /// 分配状态开始时，这个InventoryItem的物品持有数，默认为-1，意思是还未开始记录
    /// </summary>
    private int assignStartCount = -1;
    /// <summary>
    /// 记录此次进入分配状态后，已经分配过的Slot
    /// </summary>
    public List<InventoryItem> assignSlots { get; private set; }
    /// <summary>
    /// 物品拖拽的携程
    /// </summary>
    private Coroutine dragCor; 
    #endregion

    #region UI
    [SerializeField, Tooltip("负责InventoryItem的点击检测")] Image image;
    [SerializeField, Tooltip("物品的Icon")] Image icon;
    [SerializeField, Tooltip("物品的数量")] Text countText;
    [SerializeField, Tooltip("物品数量的物件，同于开关数字显示")] GameObject textObj;
    #endregion



    private void Start()
    {
        icon.sprite = Item.icon;
        RefreshCount();
    }
    private void OnEnable()=> InventorySystem.ItemRaycast += RaycastTarget;
    private void OnDisable() => InventorySystem.ItemRaycast -= RaycastTarget;
    private void OnDestroy() => InventorySystem.OnItemRaycast(true);



    public void InitialItem(Item item, int count, bool raycast = true)
    {
        Item = item;
        Count = count;
        image.raycastTarget = raycast;
    }

    public void RefreshCount()
    {
        countText.text = count.ToString();
        ActiveCountText();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (InventorySystem.Instance.dragItem != null)
            return;

        if (eventData.button == PointerEventData.InputButton.Right) //分裂
        {
            Split();
            return;
        }
        else if (eventData.button == PointerEventData.InputButton.Left) 
        {
            if (Input.GetKey(KeyCode.LeftShift)) //快捷移动
            {
                InventorySystem.SwitchInventory(this, GetSlot().Type);
                return;
            }
        }

        StartDrag();
    }

    public void CreateNewAssign() => assignSlots = new List<InventoryItem>();



    #region 功能
    /// <summary>
    /// 将一次拖拽的数量一个一个分配给所有被分配到的Slot
    /// </summary>
    /// <param name="newAssignSlot">被分配到的Slot</param>
    public void AssignOne(Slot newAssignSlot)
    {
        if (count < 1) return;

        InventoryItem newAssign = InventorySystem.InstantiateItem(Item, 1, newAssignSlot.transform, image.raycastTarget);
        assignSlots.Add(newAssign);

        Count -= 1;
        if (count <= 0) Delete();

        //如果是第一个分配，就隐藏自己 (模仿Minecraft)
        if (assignSlots.Count <= 1) Active(false);
        else Active(true);
    }
    /// <summary>
    /// 将一次拖拽的数量平均分配给所有被分配到的Slot
    /// </summary>
    /// <param name="newAssignSlot">被分配到的Slot</param>
    public void Assign(Slot newAssignSlot) 
    {
        if (assignStartCount == -1) // 初始化起始数量
            assignStartCount = count;

        // 检查是否还有足够的物品分配给新的槽位
        if (assignSlots.Count > 0 && assignStartCount / (assignSlots.Count + 1) < 1f)
            return; // 无法继续分配

        // 实例化新的物品并添加到新的槽位
        InventoryItem newAssign = InventorySystem.InstantiateItem(Item, 0, newAssignSlot.transform, image.raycastTarget);
        assignSlots.Add(newAssign);

        // 如果是第一个分配，就隐藏自己 (模仿Minecraft)
        if (assignSlots.Count <= 1) Active(false);
        else Active(true);

        // 计算每个槽位的分配量
        int assignCount = assignStartCount / assignSlots.Count; // 分配给每一个Slot的量
        int remain = assignStartCount - (assignCount * assignSlots.Count); // 余量

        if (assignCount == 0) // 确保每个槽位至少有一个物品
        {
            assignCount = 1;
            remain = assignStartCount - assignSlots.Count;
        }

        foreach (InventoryItem assign in assignSlots) // 更新每个槽位的数量
        {
            assign.Count = assignCount;
        }

        // 更新剩余数量
        Count = remain;

        // (下方代码解封后，会将余量也分配上去)
        /*if (remain > 0)
        {
            assignSlots[assignSlots.Count - 1].Count += remain;
            Count = 0;
        }*/
    }
    /// <summary>
    /// 结束[分配]状态
    /// </summary>
    public void EndAssign()
    {
        assignSlots = null;
        assignState = false;
        assignStartCount = -1;

        Active(true);

        if (count <= 0) Delete();
    }
    /// <summary>
    /// 分裂物品的一半变为拖拽物品
    /// </summary>
    public void Split()
    {
        if (count > 1)
        {
            int splitCount = count / 2;
            Count -= splitCount;

            InventoryItem splitInventoryItem = InventorySystem.InstantiateItem(Item, splitCount, InventorySystem.Instance.canvas);
            splitInventoryItem.StartDrag();
        }
    }
    /// <summary>
    /// 设置并开启拖动状态
    /// </summary>
    public void StartDrag()
    {
        parentBeforeDrag = transform.parent;
        transform.SetParent(InventorySystem.Instance.canvas);
        transform.localScale *= 1.2f;
        ActiveCountText(false);

        InventorySystem.Instance.dragItem = this;
        InventorySystem.OnItemRaycast(false);
        dragCor = StartCoroutine(DragHandle());
    }
    IEnumerator DragHandle() //拖动携程，可以有效避免使用Update频繁检测
    {
        yield return null;
        doubleClickTimer = 0.2f; //初始化双击计时器

        while (true)
        {
            transform.position = Input.mousePosition;
            yield return null;
        }
    }
    /// <summary>
    /// 设置并结束拖动状态
    /// </summary>
    /// <param name="slot"></param>
    public void EndDrag(Transform slot)
    {
        parentBeforeDrag = null;
        transform.SetParent(slot);
        transform.localScale /= 1.2f;
        ActiveCountText(true);

        if (dragCor != null)
        {
            InventorySystem.Instance.dragItem = null;
            StopCoroutine(dragCor);
        }
    }
    #endregion



    /// <summary>
    /// 激活或禁用InventoryItem的UI显示
    /// </summary>
    /// <param name="active">是否激活</param>
    public void Active(bool active)
    {
        icon.gameObject.SetActive(active);
        ActiveCountText(active);
    }
    /// <summary>
    /// true为默认根据物品数量决定是否显示数量，否则不显示
    /// </summary>
    /// <param name="enable"></param>
    public void ActiveCountText(bool enable = true)
    {
        if (!enable){
            textObj.gameObject.SetActive(false); return;
        }

        textObj.gameObject.SetActive(count > 1); //数量大于1时显示
    }



    /// <summary>
    /// 移除当前的InventoryItem
    /// </summary>
    public void Delete()
    {
        //Debug被删除的InventoryItem是哪一个
        /*if (GetSlot() != null)
        {
            Debug.Log("删除了" + transform.parent.parent.name + "下的第" + transform.parent.GetSiblingIndex() + "个");
        }*/
        Destroy(gameObject);
    }
    private void RaycastTarget(bool raycast) => image.raycastTarget = raycast;
    private Slot GetSlot()
    {
        return GetComponentInParent<Slot>();
    }
}
