using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "TurnBasedRPG/Item Data")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite itemIcon;
    public int hpHealAmount;
    public int mpHealAmount;
    [Header("Pengaturan Audio")]
    public string useItemSfxId;
    
    // Perhatikan: Kita sengaja TIDAK menaruh currentStock di sini.
}