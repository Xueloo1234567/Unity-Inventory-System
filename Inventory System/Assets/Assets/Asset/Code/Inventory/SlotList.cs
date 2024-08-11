using UnityEngine;

public class SlotList : MonoBehaviour
{
    [SerializeField] InventoryType awakeType;

    private void Start() => SyncTypeWithSlots();
    private void OnEnable() => InventorySystem.onlyShortcut = false;
    private void OnDisable()
    {
        InventorySystem.onlyShortcut = true;

        InventorySystem.Instance.SyncBag(gameObject.name, awakeType); //同步同类型背包
    }


    /// <summary>
    /// 同步和子对象的Slot的Type
    /// </summary>
    private void SyncTypeWithSlots()
    {
        foreach (Transform child in transform)
        {
            Slot slot = child.GetComponent<Slot>();
            if (slot == null) Debug.LogError(gameObject.name + ": SlotList下有未持有Slot组件的对象");

            slot.Type = awakeType;
        }
    }
}
