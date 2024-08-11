using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 背包系统，提供了大部分背包系统的功能和方法，是单例对象。内部提供方法说明，可根据需求自行修改。
/// 作者：雪萝Xueloo
/// 系统参考：Minecraft背包系统
/// 代码基础参考，感谢油管Coco Code老师的教学：https://youtu.be/oJAE6CbsQQA?si=dExs8Oe7xQBqQAis
/// </summary>



public class InventorySystem : MonoBehaviour
{
    #region Debug
    [Header("Debug")]
    [SerializeField] bool addItemDebug = false; //AddItem函数的Debug
    [SerializeField] bool syncBagDebug = false; //同步背包的Debug
    [SerializeField] WorldItem worldItem_Prefab;
    [SerializeField] Item[] randomItems;
    #endregion

    #region Event
    public static Action<bool> ItemRaycast;
    #endregion

    #region 常量
    [Space(10),Header("必要引用")]
    [Tooltip("Order最高的Canvas")] public Transform canvas; //Order最高的UI
    [Tooltip("放置资源和箱子")] public Transform enviroment;
    [SerializeField,Tooltip("背包")] public GameObject bagPanel;
    [SerializeField,Tooltip("背包范围之外的Panel")] private GameObject[] bagOutsidePanels;
    [Tooltip("背包(箱子状态)")] public GameObject chestBag;
    [Tooltip("InventoryItem预制体")] public InventoryItem inventoryItem_Prefab;
    [Tooltip("Slot预制体")] public Slot slot_Prefab;
    [Tooltip("Chest预制体")] public Chest chest_Prefab;
    [SerializeField,Tooltip("库存")] private List<InventoryList> inventoryList = new List<InventoryList>();
    public static InventorySystem Instance { get; private set; }
    #endregion

    #region 变量
    private int curSelectSlot = -1; //快捷栏编号
    public InventoryItem dragItem { get; set; } //正在拖拽的InventoryItem
    public Slot enterSlot { get; set; } //鼠标下的Slot
    public static bool pressRight { get; private set; } //鼠标右键
    public static bool pressLeft { get; private set; } //鼠标左键
    public static bool onlyShortcut { get; set; } //当前画面中仅有快捷栏Active，当其他SlotList激活和禁用时改变
    public static Chest openChest { get; private set; } //正在打开的箱子
    #endregion



    /*仅有一行的函数我比较喜欢用匿名*/
    private void Awake() => Instance = this; //这是一个最简单的单例，可以根据需求修改

    private void Start() => ChangeSelectSlot(0); //初始默认选择快捷栏第一格

    private void Update() => InputHandle(); //输入检测和控制

    private void InputHandle()
    {
        //检测数字键，更改快捷栏选择
        if (Input.inputString != null)
        {
            bool isNumber = int.TryParse(Input.inputString, out int number);
            if (isNumber && number > 0 && number < 10) //数字键1到9
                ChangeSelectSlot(number - 1);
        }

        //左右键检测
        if (Input.GetKey(KeyCode.Mouse0)) pressLeft = true;
        else if (Input.GetKey(KeyCode.Mouse1)) pressRight = true;
        else pressRight = pressLeft = false;

        //ESC开启和关闭背包和背包(箱子)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (openChest != null) CloseChest();
            else
            {
                if (bagPanel.activeSelf) OpenBag(false);
                else OpenBag(true);
            }
        }

        if (dragItem != null)
        {
            //削减拖拽物品的双击计时器
            dragItem.doubleClickTimer -= Time.deltaTime;
            dragItem.leftShiftDoubleClickTimer -= Time.deltaTime;

            //检测是否有点击到背包外，丢弃物品
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
                DetectPanel();

            //结束[分配]状态
            if (dragItem.assignState && (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1)))
                dragItem.EndAssign();
        }

        //生成箱子在鼠标位置
        if (Input.GetKeyDown(KeyCode.C))
        {
            Instantiate(chest_Prefab, Input.mousePosition, Quaternion.identity).transform.SetParent(enviroment);
        }
        //销毁鼠标位置的箱子
        else if (Input.GetKeyDown(KeyCode.D))
        {
            Chest enterChest = UIManager.ui_touchObj.GetComponent<Chest>();
            if(enterChest != null)
                Destroy(enterChest.gameObject);
        }
    }



    #region Debug_Method
    /// <summary>
    /// 在场景中心位置生成随机物品和随机数量
    /// </summary>
    public void RandomSpawnItemInWorld()
    {
        WorldItem worldItem = Instantiate(worldItem_Prefab, GetScreenRandomPos(100), Quaternion.identity);
        worldItem.transform.SetParent(enviroment);
        Item spawnItem = randomItems[UnityEngine.Random.Range(0, randomItems.Length)];
        worldItem.InitialItem(spawnItem, UnityEngine.Random.Range(1, spawnItem.stackCount));

        Vector2 GetScreenRandomPos(float radius)
        {
            Vector2 range = new Vector2(Screen.width/2, Screen.height/2);
            Vector2 offset = UnityEngine.Random.onUnitSphere;
            range += offset * radius;
            return range;
        }
    }
    #endregion



    #region 公共方法
    /*大部分非静态方法我还是用了静态来包装，因为比较方便外部调用，不用加.Instance.*/
    /// <summary>
    /// 将指定数量的物品添加到指定类型的背包，默认为添加到快捷栏和背包，当背包满了会返回false
    /// </summary>
    /// <param name="item">添加物品</param>
    /// <param name="count">添加数量</param>
    /// <param name="addToType">背包类型</param>
    public static bool AddItem(Item item, int count, InventoryType[] addToType = null)
    {
        Instance.AddItem(item, count, out Item overItem, addToType);
        return overItem == null;
    }
    /// <summary>
    /// 生成InventoryItem
    /// </summary>
    /// <param name="item">生成物品</param>
    /// <param name="count">生成数量</param>
    /// <param name="slot">对应Slot</param>
    /// <param name="raycast">是否在生成时激活raycast(鼠标感应)</param>
    /// <returns></returns>
    public static InventoryItem InstantiateItem(Item item, int count, Transform slot, bool raycast = true)
    {
        InventoryItem inventoryItem = Instantiate(Instance.inventoryItem_Prefab, slot);
        inventoryItem.InitialItem(item, count, raycast);
        return inventoryItem;
    }
    /// <summary>
    /// 循环快捷栏和背包，如果有Slot的InventoryItem不为空，删除其InventoryItem
    /// </summary>
    public static void DeleteAllItem()
    {
        List<InventoryItem> allItem = GetAllItem();

        foreach (InventoryItem inventoryItem in allItem) inventoryItem.Delete();
    }
    /// <summary>
    /// 获取所有在快捷栏和背包中存在的InventoryItem
    /// </summary>
    /// <returns></returns>
    public static List<InventoryItem> GetAllItem()
    {
        List<InventoryItem> itemList = new List<InventoryItem>();

        foreach (InventoryList list in Instance.inventoryList)
            if (list.inventoryType == InventoryType.Bag || list.inventoryType == InventoryType.Shortcut)
                foreach (Slot slot in list.slots)
                {
                    InventoryItem inventoryItem = slot.GetItemInSlot();
                    if (inventoryItem != null) itemList.Add(inventoryItem);
                }
        return itemList;
    }
    /// <summary>
    /// 控制全局InventoyItem的射线检测
    /// </summary>
    /// <param name="raycast"></param>
    public static void OnItemRaycast(bool raycast)
    {
        ItemRaycast?.Invoke(raycast); //InventoryItem在激活和禁用时会自动订阅和卸载
    }
    /// <summary>
    /// 将物品转移到对应类型的包
    /// </summary>
    /// <param name="switchItem">要转移的物品</param>
    /// <param name="currentType">要转移哪一个类型的包</param>
    public static void SwitchInventory(InventoryItem switchItem, InventoryType currentType)
    {
        InventoryType[] targetType = GetSwitchType(currentType);

        //如果成功转移物品，删除本体
        if (AddItem(switchItem.Item, switchItem.Count, targetType))
            switchItem.Delete();
    }
    /// <summary>
    /// 模拟物品双击时的聚集效果
    /// </summary>
    /// <param name="_dragItem">被双击的物品</param>
    /// <param name="mouseNum">鼠标索引，0代表左键</param>
    public void OnDoubleClick(InventoryItem _dragItem, int mouseNum)
    {
        if (mouseNum == 0)
        {
            //搜索背包中的同类物品，并叠加到_draItem上，直到背包没有同类物品或_draItem达到堆叠上限
            List<InventoryItem> items = GetItemList(_dragItem.Item);
            // 对items按数量从小到大排序，确保优先聚集数量较小的物品
            items.Sort((item1, item2) => item1.Count.CompareTo(item2.Count));

            Item targetItem = _dragItem.Item;

            foreach (InventoryItem item in items)
            {
                if (item.Count + _dragItem.Count > targetItem.stackCount) //溢出
                {
                    int dif = targetItem.stackCount - _dragItem.Count;
                    _dragItem.Count += dif;
                    item.Count -= dif;
                }
                else //没溢出
                {
                    _dragItem.Count += item.Count;
                    item.Delete();
                }

                if (_dragItem.Count == targetItem.stackCount)
                    break;
            }
        }
    }
    /// <summary>
    /// 模拟LeftShift+拖拽物品双击其他物品的成堆堆放效果(参考Minecraft)
    /// </summary>
    /// <param name="targetItem">被堆放的物品</param>
    /// <param name="currentType">双击时所处的包类型</param>
    public void OnLeftShiftDoubleClick(Item targetItem, InventoryType currentType)
    {
        InventoryType[] targetType = GetSwitchType(currentType);

        //搜索对应包的所有同类物品。*这里加了额外判断，如果是从背包或快捷栏移动的，那么搜索范围就扩大到背包+快捷栏*
        List<InventoryItem> items;
        if (currentType == InventoryType.Chest)
            items = GetItemList(targetItem, true, new InventoryType[] { currentType });
        else
            items = GetItemList(targetItem, true, new InventoryType[] { currentType,
                currentType == InventoryType.Shortcut ? InventoryType.Bag : InventoryType.Shortcut });

        foreach (InventoryItem inventoryItem in items)
        {
            if (AddItem(targetItem, inventoryItem.Count, targetType))
                inventoryItem.Delete();
        }

        //防Bug，为了确保在转移后继续关闭InventoryItem的射线检测，好像是因为物品生成会自动打开射线检测
        StartCoroutine(SetRaycast()); 
        IEnumerator SetRaycast()
        {
            yield return null; OnItemRaycast(false);
        }
    }
    /// <summary>
    /// 获取快捷栏选中的物品
    /// </summary>
    /// <returns></returns>
    public static InventoryItem GetSelectItem()
    {
        InventoryItem itemInSlot = Instance.GetInventory(InventoryType.Shortcut).slots[Instance.curSelectSlot].GetItemInSlot();
        if (itemInSlot != null)
        {
            return itemInSlot;
        }
        return null;
    }
    #endregion



    #region 内部方法(可自定义公开)
    private void AddItem(Item item, int count, out Item overItem, InventoryType[] addToType = null)
    {
        #region 初始化
        overItem = null;
        if (count > item.stackCount) count = item.stackCount; //一次添加的数量不能超过该物品的堆叠上限
        #endregion

        #region Type初始化
        if (addToType == null) //addToType代表可以被循环的背包，意思是如果有多个addToType，当前面的type的背包满了就会遍历后面type的背包
        {
            //举例：这里是默认遍历快捷栏先，然后是背包
            addToType = new InventoryType[2] { InventoryType.Shortcut, InventoryType.Bag };
        }
        #endregion

        #region 类型循环
        foreach (InventoryType type in addToType)
        {
            #region 激活包检测
            //当所有该类型的包处于未激活状态，将同时为所有该类型的包AddItem
            //如果该类型的某一个包处于激活状态，则仅为该包AddItem
            //*注意1* 如果所有该类型的包处于未激活，这些包的物品理应是同步的，不然会出bug！(写给我自己)
            //*注意2* 不要同时打开多个同类型的包。同类型的包只能打开一个，因为代码只会更新一个包

            List<InventoryList> lists = GetInventories(type); //要更新的list
            if (addItemDebug) Debug.Log(type + "有" + lists.Count + "个包");

            for (int i = 0; i < lists.Count; i++)
            {
                if (lists[i].slotParent.gameObject.activeInHierarchy) //检测到激活包，所以仅为该包AddItem
                {
                    InventoryList activeList = lists[i];
                    lists.Clear();
                    lists.Add(activeList); //确保仅更新这个list
                    break;
                }
            }
            #endregion

            //在确定要更新的包之后，进入包循环

            int index = 0; //循环次数
            bool _continue; //检测到true时，直接跳过执行下一个循环
            #region 包循环
            foreach (InventoryList list in lists)
            {
                if (addItemDebug) Debug.Log("添加物品至 " + list.inventoryName);

                _continue = false;

                List<Slot> emptySlot = new List<Slot>(); //循环中发现的空Slot

                #region Slot同类物品检测(优先寻找同类物品)
                foreach (Slot slot in list.slots)
                {
                    InventoryItem itemInSlot = slot.GetItemInSlot();

                    if (itemInSlot != null)
                    {
                        //寻找同类物品
                        if (itemInSlot.Item == item)
                        {
                            //数量没有超出堆叠上限
                            if (itemInSlot.Count + count <= item.stackCount)
                            {
                                itemInSlot.Count += count;
                                #region 跳转(成功AddItem)
                                if (index == lists.Count - 1) return; //全部包都AddItem了，返回
                                else
                                {
                                    index++;
                                    _continue = true;
                                    break; //还有包没AddItem，跳转并继续循环下一个包
                                }
                                #endregion
                            }
                            //超出数量时
                            else
                            {
                                count -= item.stackCount - itemInSlot.Count; //消耗

                                itemInSlot.Count = item.stackCount; //填满

                                //然后count就会剩下一些，继续循环
                            }
                        }
                    }
                    else
                    {
                        emptySlot.Add(slot);
                    }
                }
                #endregion
                if (_continue) continue;

                //执行到这代表没有找到同类物品
                #region Slot空位检测
                foreach (Slot slot in emptySlot) //寻找空位
                {
                    InstantiateItem(item, count, slot.transform);
                    #region 跳转(成功AddItem)
                    if (index == lists.Count - 1) return; //全部包都AddItem了
                    else
                    {
                        index++;
                        _continue = true;
                        break; //还有包没AddItem，跳转并继续循环下一个包
                    }
                    #endregion
                }
                #endregion
                if (_continue) continue;

                //执行到这代表没有找到空位
                if (addItemDebug) Debug.Log(list.inventoryName + "满了");
                break; //退出这个类型的包的循环，现在再循环这个类型的包是没有意义的，因为同类型的包内容一致，代表后面也是满的
            }
            #endregion

            //执行到这代表该类型的包都没有空位
            if (addItemDebug) Debug.Log("所有" + type + "的包都满了");
        }
        //执行到这代表所有类型的包都没有空位，返回这个物品的添加信息
        if (addItemDebug) Debug.Log("全满");
        overItem = item;
        #endregion
    }
    /// <summary>
    /// 检测是否点击到背包外的Panel，如果有则丢弃物品
    /// </summary>
    private void DetectPanel()
    {
        bool detect = false;
        foreach (GameObject bagOutsidePanel in bagOutsidePanels)
        {
            if (UIManager.ui_touchObj == bagOutsidePanel) detect = true;
        }

        if (detect)
        {
            //丢弃物品
            dragItem.Delete();
        }
    }
    /// <summary>
    /// 获取对应类型和对应名字的，且在inventoryList索引内的InventoryList，包名字为空则默认选择第一个遍历到的
    /// </summary>
    /// <param name="type">包类型</param>
    /// <param name="inventoryName">包名字</param>
    /// <returns></returns>
    private InventoryList GetInventory(InventoryType type, string inventoryName = null)
    {
        foreach (InventoryList list in inventoryList)
        {
            if (list.inventoryType == type)
            {
                if (inventoryName == null) inventoryName = list.inventoryName;

                if (list.inventoryName == inventoryName)
                {
                    Debug.Log(inventoryName);
                    return list;
                }
            }
        }

        Debug.Log("Null");
        return null;
    }
    /// <summary>
    /// 获得所有同一类型的，且在inventoryList索引内的InventoryList
    /// </summary>
    /// <param name="type">包类型</param>
    /// <returns></returns>
    private List<InventoryList> GetInventories(InventoryType type)
    {
        List<InventoryList> lists = new List<InventoryList>();
        foreach (InventoryList list in inventoryList)
        {
            if (list.inventoryType == type) lists.Add(list);
        }
        return lists;
    }
    /// <summary>
    /// 获取当前激活的包的同类物品InventoryItem，默认搜素快捷栏，背包和箱子，也可以自定义搜索的包类型
    /// 如果searchActive为false，也可以搜索未激活的包的inventoryItem
    /// </summary>
    /// <param name="targetItem">要搜索的Item</param>
    /// <param name="targetType">自定义包类型</param>
    /// <returns></returns>
    private List<InventoryItem> GetItemList(Item targetItem, bool searchActive = true, InventoryType[] targetType = null)
    {
        if (targetType == null)
        {
            targetType = new InventoryType[3];
            targetType[0] = InventoryType.Bag;
            targetType[1] = InventoryType.Shortcut;
            targetType[2] = InventoryType.Chest;
        }

        List<InventoryItem> items = new List<InventoryItem>();
        foreach (InventoryType type in targetType)
        {
            List<InventoryList> lists = GetInventories(type);//获取同一类型的所有包的list，放置重复添加

            foreach (InventoryList list in lists)
            {
                if (list == null) break;

                if (searchActive == false) //searchActive为false，即使未激活也搜索
                {
                    Search();
                    break; //只搜索其中一个同类型包就好，防止重复搜索
                }
                else if (list.slotParent.gameObject.activeInHierarchy) //如果该包处于激活状态
                {
                    Search();
                }
                void Search()
                {
                    foreach (Slot slot in list.slots)
                    {
                        InventoryItem inventoryItem = slot.GetItemInSlot();
                        if (inventoryItem != null && inventoryItem.Item == targetItem)
                        {
                            items.Add(inventoryItem);
                        }
                    }
                }
            }
        }

        return items;
    }
    /// <summary>
    /// 根据当前选择的包类型，获取对应的包类型，对于快捷转移或搜索对应包很有用
    /// </summary>
    /// <param name="currentType">当前包类型</param>
    /// <returns>要转移去的包类型</returns>
    private static InventoryType[] GetSwitchType(InventoryType currentType)
    {
        List<InventoryType> targetType = new List<InventoryType>();

        if (openChest != null)
        {
            if (currentType == InventoryType.Chest)
            {
                targetType.Add(InventoryType.Shortcut);
                targetType.Add(InventoryType.Bag);
            }
            else targetType.Add(InventoryType.Chest);
        }
        else
        {
            targetType.Add(currentType == InventoryType.Shortcut ? InventoryType.Bag : InventoryType.Shortcut);
        }

        return targetType.ToArray();
    }
    /// <summary>
    /// 切换快捷栏选择栏位，
    /// </summary>
    /// <param name="index">数字键</param>
    private void ChangeSelectSlot(int index)
    {
        InventoryList shortcutList = GetInventory(InventoryType.Shortcut);
        index = Mathf.Clamp(index, 0, shortcutList.slots.Length);//确保数字键不会超过当前的快捷栏格子数量

        if (onlyShortcut) //当游戏画面只有快捷栏时，数字键旋转对应快捷栏
        {
            if (curSelectSlot >= 0) shortcutList.slots[curSelectSlot].DeSelect();

            shortcutList.slots[index].Select();
            curSelectSlot = index;
        }
        else //当打开背包时，数字键根据鼠标所在Slot选取该Slot的inventoryItem并移动到快捷栏
        //快捷栏满了就移动到背包
        {
            if (enterSlot == null) return;

            InventoryItem inventoryItem = enterSlot.GetItemInSlot();
            //鼠标Slot不是快捷栏物品，且鼠标Slot有inventoryItem时
            if (enterSlot.Type != InventoryType.Shortcut && inventoryItem != null)
            {
                InventoryItem shortcutSlotItem = shortcutList.slots[index].GetItemInSlot();

                //快捷栏有物品，交换位置
                if (shortcutSlotItem != null) shortcutSlotItem.transform.SetParent(enterSlot.transform);

                inventoryItem.transform.SetParent(shortcutList.slots[index].transform);
            }
        }
    }
    #endregion



    #region 箱子、背包与同步
    public static void OpenBag(bool open) => Instance._OpenBag(open);
    public static void OpenChest(Chest chest) => Instance._OpenChest(chest);
    public static void CloseChest() => Instance._CloseChest();
    private void _OpenBag(bool open)
    {
        bagPanel.SetActive(open);
    }
    private void _OpenChest(Chest chest)
    {
        chest.chestCanvas.SetActive(true);
        chestBag.gameObject.SetActive(true);

        openChest = chest;
        //将箱子的库存放入库存
        inventoryList.Add(new InventoryList
        {
            inventoryName = chest.slotParent.name,
            inventoryType = InventoryType.Chest,
            slotParent = chest.slotParent.GetComponent<SlotList>(),
            slots = chest.slots
        });
    }
    private void _CloseChest()
    {
        openChest.chestCanvas.SetActive(false);
        chestBag.gameObject.SetActive(false);

        //移除该箱子的库存
        inventoryList.RemoveAll(item => item.inventoryName == openChest.slotParent.name);
        openChest = null;
    }
    /// <summary>
    /// 同步某一类型的多个包的内容，当SlotList被禁用时调用 (人话：当某个背包被关闭时同步所有背包)
    /// </summary>
    /// <param name="inventoryName">最近关闭的背包的名字(参照背包)</param>
    /// <param name="inventoryType">最近关闭的背包的类型(参照背包)</param>
    public void SyncBag(string inventoryName, InventoryType inventoryType)
    {
        if (inventoryType == InventoryType.Chest) return; //箱子不需要被同步，除非以后有多个同一箱子的引用再来修改

        if (syncBagDebug) Debug.Log(inventoryName + "已被关闭，同步其他包");

        //获取参照背包的库存
        InventoryList referenceBag = GetInventory(inventoryType, inventoryName);

        foreach (InventoryList syncBag in inventoryList)
        {
            //选择同类型但不同名的背包作为[被同步背包]
            if (syncBag.inventoryType == inventoryType && syncBag.inventoryName != inventoryName)
            {
                if (syncBagDebug) Debug.Log("同步" + syncBag.inventoryName);

                if (Instance != null) //防止游戏关闭瞬间调用
                {
                    StartCoroutine(RefreshSlot(syncBag.slots, referenceBag.slots));
                }
            }
        }
        //同步，就是删除[被同步背包]的物品然后再将参照背包的物品生成上去
        //*使用携程是避免卡顿*
        IEnumerator RefreshSlot(Slot[] syncSlots, Slot[] referenceSlot)
        {
            for (int i = 0; i < syncSlots.Length; i++)
            {
                Transform syncSlot = syncSlots[i].transform;

                if (syncSlot.childCount > 0)
                {
                    Destroy(syncSlot.GetChild(0).gameObject);
                }

                InventoryItem inventoryItem = referenceSlot[i].GetItemInSlot();
                if (inventoryItem != null)
                {
                    InstantiateItem(inventoryItem.Item, inventoryItem.Count, syncSlot);
                }
                yield return null;
            }
        }
    }
    #endregion
}

[Serializable]
public class InventoryList
{
    public string inventoryName; //包名字，可用来代码搜索和辨识
    public InventoryType inventoryType; //包类型
    public SlotList slotParent; //Slot的父对象
    public Slot[] slots; //你好，我是Slots
}
public enum InventoryType
{
    Bag, Shortcut, Chest
}
