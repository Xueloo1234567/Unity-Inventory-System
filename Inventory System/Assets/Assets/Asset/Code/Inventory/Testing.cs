using UnityEngine;

public class Testing : MonoBehaviour
{
    [SerializeField] Item[] itemToPickUp;
    [SerializeField] int[] pickUpCount;



    public void PickUpItem(int index)
    {
        InventorySystem.AddItem(itemToPickUp[index], pickUpCount[index]);
    }
    public void SpawnResource()
    {
        InventorySystem.Instance.RandomSpawnItemInWorld();
    }
}
