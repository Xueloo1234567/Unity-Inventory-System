using UnityEngine;
using UnityEngine.EventSystems;

public class Chest : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    public GameObject chestCanvas;
    public Transform slotParent;
    public Slot[] slots;

    [SerializeField] int size; //箱子格子数量
    [SerializeField] GameObject hint;




    private void Start() => RefreshSlot();



    /// <summary>
    /// 根据指定Size，在初始化时生成指定数量的Slot
    /// </summary>
    private void RefreshSlot()
    {
        if (slots.Length != size - 1)
        {
            foreach (Slot slot in slots)
            {
                if (slot != null) Destroy(slot.gameObject);
            }

            slots = new Slot[size];

            for (int i = 0; i < size; i++)
            {
                Slot newSlot = Instantiate(InventorySystem.Instance.slot_Prefab, slotParent);
                slots[i] = newSlot;
            }
        }
    }



    public void OnPointerDown(PointerEventData eventData) => InventorySystem.OpenChest(this);
    public void OnPointerEnter(PointerEventData eventData) => hint.SetActive(true);
    public void OnPointerExit(PointerEventData eventData) => hint.SetActive(false);
}
