using UnityEngine;
using System.Collections.Generic;

public enum SkillType { MELEE, RANGED }

// System.Serializable wajib ditambahkan agar class ini bisa muncul di Inspector Unity
[System.Serializable]
public class SkillAction
{
    public string skillName;
    public SkillType skillType; // Maju atau diam di tempat
    public int damage;
    public int mpCost;
    public string animTriggerName; // Nama trigger di Animator (misal: "Skill1", "Magic")
}

[CreateAssetMenu(fileName = "NewUnitData", menuName = "TurnBasedRPG/Unit Data")]
public class UnitData : ScriptableObject
{
    [Header("Identitas Karakter")]
    public string unitName;
    public Sprite unitSprite; 
    public bool isPlayerTeam; 
    public RuntimeAnimatorController animatorController; 

    [Header("Status Pertarungan")]
    public int maxHP;
    public int maxMP;
    public int baseDamage;
    [Header("Status Pertarungan")]
    // --- BARU: Pengaturan tipe pergerakan untuk Basic Attack ---
    public SkillType basicAttackType;

    [Header("Daftar Skill")]
    // Ini akan memunculkan array di Inspector di mana kamu bisa menambah 2, 3, atau 10 skill sekaligus
    public List<SkillAction> skills = new List<SkillAction>(); 
}