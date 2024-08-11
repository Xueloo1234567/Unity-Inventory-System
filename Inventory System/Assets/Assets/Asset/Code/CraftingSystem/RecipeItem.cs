using UnityEngine;

[CreateAssetMenu(fileName ="new Recipe",menuName = "Inventory/Recipe")]
public class RecipeItem : ScriptableObject
{
    public Item output;

    public Item item_00;
    public Item item_10;
    public Item item_20;

    public Item item_01;
    public Item item_11;
    public Item item_21;

    public Item item_02;
    public Item item_12;
    public Item item_22;


    public Item GetItem(int x, int y)
    {
        if (x == 0 && y == 0) return item_00;
        if (x == 1 && y == 0) return item_10;
        if (x == 2 && y == 0) return item_20;

        if (x == 0 && y == 1) return item_01;
        if (x == 1 && y == 1) return item_11;
        if (x == 2 && y == 1) return item_21;

        if (x == 0 && y == 2) return item_02;
        if (x == 1 && y == 2) return item_12;
        if (x == 2 && y == 2) return item_22;

        return null;
    }
}
