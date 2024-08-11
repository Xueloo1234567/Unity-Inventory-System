using UnityEngine;

/// <summary>
/// 物品，包含所有有关物品的属性
/// </summary>

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    /// <summary>
    /// 物品名称
    /// </summary>
    public string itemName;
    /// <summary>
    /// 物品图片
    /// </summary>
    public Sprite icon;
    /// <summary>
    /// 物品介绍
    /// </summary>
    public string introduce;
    /// <summary>
    /// 物品堆叠上限
    /// </summary>
    public int stackCount = 1;
}
