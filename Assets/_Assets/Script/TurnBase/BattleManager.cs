using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public enum BattleState { START, PLAYERTURN, WAITING_FOR_TARGET, BUSY, ENEMYTURN, WON, LOST }
public enum ActionType { NONE, ATTACK, SKILL, ITEM }

[System.Serializable]
public class BattleItemSlot
{
    public ItemData itemSO;      
    public int initialStock;     
    [HideInInspector] public int currentStock;     
}

public class BattleManager : MonoBehaviour
{
    [Header("Status Pertarungan")]
    public BattleState state;

    [Header("Referensi Cetakan (Prefab)")]
    public GameObject battleUnitPrefab;

    [Header("Pengaturan Party Pemain")]
    public List<Transform> playerStations;
    public List<UnitData> playerTeamData;
    public List<Unit> activePlayerUnits = new List<Unit>(); 

    [Header("Pengaturan Party Musuh")]
    public List<Transform> enemyStations;
    public List<UnitData> enemyTeamData;
    public List<Unit> activeEnemyUnits = new List<Unit>();

    [Header("UI Pertarungan")]
    public GameObject actionPanel; 
    public GameObject skillListPanel;      
    public Transform skillListContainer;   
    public GameObject skillItemPrefab;     
    public GameObject itemListPanel;      
    public Transform itemListContainer;   
    public GameObject itemButtonPrefab;

    [Header("Inventory Sementara")]
    public List<BattleItemSlot> battleItems = new List<BattleItemSlot>();

    // --- MEKANIK ANTREAN & TARGETING ---
    private int currentPlayerIndex = 0; 
    private Unit currentActingUnit;     
    private ActionType pendingAction;   
    private int pendingSkillIndex; // Menyimpan memori skill mana yang baru ditekan
    private int pendingItemIndex;  // Menyimpan memori item mana yang baru ditekan
    private bool hasUsedItemThisTurn = false;
    // --- BARU: Variabel Navigasi WASD ---
    private List<Unit> validTargets = new List<Unit>();
    private int currentTargetIndex = 0;

    private void Start()
    {
        activePlayerUnits.Clear();
        activeEnemyUnits.Clear();
        state = BattleState.START;
        actionPanel.SetActive(false);
        CloseAllSubPanels();

        foreach (BattleItemSlot slot in battleItems)
        {
            slot.currentStock = slot.initialStock;
        }
        StartCoroutine(SetupBattleTeam());
    }
    private void Update()
    {
        if (state == BattleState.WAITING_FOR_TARGET && validTargets.Count > 0)
        {
            HandleTargetSelection();
        }
    }

    private void HandleTargetSelection()
    {
        if (Keyboard.current == null) return;

        // W atau A untuk ke atas/kiri
        if (Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame)
        {
            ChangeTarget(1); // --- UBAH: Sekarang pakai 1 ---
        }
        // S atau D untuk ke bawah/kanan
        else if (Keyboard.current.sKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame)
        {
            ChangeTarget(-1); // --- UBAH: Sekarang pakai -1 ---
        }
        else if (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame)
        {
            ConfirmTarget();
        }
    }

    private void ChangeTarget(int direction)
    {
        // 1. Matikan bayangan target saat ini
        validTargets[currentTargetIndex].SetTargetIndicator(false);

        // 2. Geser indeks (ke atas atau ke bawah)
        currentTargetIndex += direction;

        // 3. Logika Wrap-Around (Mentok bawah tembus ke atas, mentok atas tembus ke bawah)
        if (currentTargetIndex < 0) currentTargetIndex = validTargets.Count - 1;
        if (currentTargetIndex >= validTargets.Count) currentTargetIndex = 0;

        // 4. Nyalakan bayangan target yang baru
        validTargets[currentTargetIndex].SetTargetIndicator(true);
    }

    private IEnumerator SetupBattleTeam()
    {
        // SPAWN TIM PEMAIN
        for (int i = 0; i < playerTeamData.Count; i++)
        {
            if (i < playerStations.Count) 
            {
                GameObject pGo = Instantiate(battleUnitPrefab, playerStations[i]);
                Unit pUnit = pGo.GetComponent<Unit>();
                pUnit.SetupUnit(playerTeamData[i]);
                pUnit.originalPosition = playerStations[i].position;
                activePlayerUnits.Add(pUnit);
            }
        }

        // SPAWN TIM MUSUH
        for (int i = 0; i < enemyTeamData.Count; i++)
        {
            if (i < enemyStations.Count)
            {
                GameObject eGo = Instantiate(battleUnitPrefab, enemyStations[i]);
                Unit eUnit = eGo.GetComponent<Unit>();
                eUnit.SetupUnit(enemyTeamData[i]);
                eUnit.originalPosition = enemyStations[i].position;
                activeEnemyUnits.Add(eUnit);
            }
        }

        yield return new WaitForSeconds(1.5f);
        
        currentPlayerIndex = 0;
        StartPlayerTurn();
    }

    private void CloseAllSubPanels()
    {
        if (skillListPanel != null) skillListPanel.SetActive(false);
        if (itemListPanel != null) itemListPanel.SetActive(false);
    }

    // --- MENGELOLA GILIRAN PEMAIN INDIVIDU ---
    private void StartPlayerTurn()
    {
        if (activePlayerUnits.Count == 0) return;

        currentActingUnit = activePlayerUnits[currentPlayerIndex];

        // Cek jika pemain ini mati karena efek bakar sebelum gilirannya mulai
        if (currentActingUnit.burnTurnsLeft > 0)
        {
            bool deadByBurn = currentActingUnit.TakeBurnDamage();
            if (deadByBurn)
            {
                activePlayerUnits.Remove(currentActingUnit);
                currentActingUnit.Die();
                NextTurnProcessor(); // Lanjut ke antrean berikutnya
                return;
            }
        }

        Debug.Log("Giliran: " + currentActingUnit.unitName);
        currentActingUnit.SetDefending(false);
        currentActingUnit.SetTurnIndicator(true);
        hasUsedItemThisTurn = false;
        pendingAction = ActionType.NONE;

        state = BattleState.PLAYERTURN;
        CloseAllSubPanels();
        actionPanel.SetActive(true);
    }
    
    // --- FUNGSI BARU: Menyiapkan Mode Targeting ---
    private void StartTargetingMode(ActionType action)
    {
        pendingAction = action;
        state = BattleState.WAITING_FOR_TARGET;
        
        CloseAllSubPanels();
        actionPanel.SetActive(false);

        // Kumpulkan target yang valid (Kawan atau Lawan)
        validTargets.Clear();
        if (action == ActionType.ATTACK || action == ActionType.SKILL)
        {
            validTargets.AddRange(activeEnemyUnits);
            Debug.Log("Pilih Musuh! (WASD untuk geser, Spasi/Enter untuk konfirmasi)");
        }
        else if (action == ActionType.ITEM)
        {
            validTargets.AddRange(activePlayerUnits);
            Debug.Log("Pilih Kawan! (WASD untuk geser, Spasi/Enter untuk konfirmasi)");
        }

        // Set kursor ke target pertama
        currentTargetIndex = 0;
        validTargets[currentTargetIndex].SetTargetIndicator(true);
    }

    // ==========================================
    // BAGIAN 1: TOMBOL-TOMBOL UI (KLIK PERTAMA)
    // ==========================================

    public void OnAttackButton()
    {
        if (state != BattleState.PLAYERTURN) return;
        StartTargetingMode(ActionType.ATTACK);
    }

    public void OnDefendButton()
    {
        if (state != BattleState.PLAYERTURN) return;

        actionPanel.SetActive(false);
        CloseAllSubPanels();
        StartCoroutine(PlayerDefend());
    }

    public void ToggleSkillPanel()
    {
        if (state != BattleState.PLAYERTURN) return;

        bool isActive = skillListPanel.activeSelf;
        CloseAllSubPanels();
        skillListPanel.SetActive(!isActive);
        if (!isActive) PopulateSkillList();
    }

    private void PopulateSkillList()
    {
        foreach (Transform child in skillListContainer) Destroy(child.gameObject);

        // Hanya tampilkan skill milik karakter yang sedang jalan!
        for (int i = 0; i < currentActingUnit.maxMP; i++) // Trick agar rapi
        {
            if (i >= playerTeamData[currentPlayerIndex].skills.Count) break;

            int index = i; 
            SkillAction skill = playerTeamData[currentPlayerIndex].skills[i];

            GameObject newBtnObj = Instantiate(skillItemPrefab, skillListContainer);
            TextMeshProUGUI btnText = newBtnObj.GetComponentInChildren<TextMeshProUGUI>();
            btnText.text = skill.skillName + " (MP: " + skill.mpCost + ")";

            Button btn = newBtnObj.GetComponent<Button>();
            btn.onClick.AddListener(() => ExecuteSkillButton(index));
        }
    }

    public void ExecuteSkillButton(int skillIndex)
    {
        SkillAction chosenSkill = playerTeamData[currentPlayerIndex].skills[skillIndex];
        if (currentActingUnit.currentMP >= chosenSkill.mpCost)
        {
            pendingSkillIndex = skillIndex;
            StartTargetingMode(ActionType.SKILL);
        }
        else Debug.Log("MP Tidak Cukup!");
    }

    public void ToggleItemPanel()
    {
        if (state != BattleState.PLAYERTURN) return;
        if (hasUsedItemThisTurn)
        {
            Debug.Log("Hanya bisa pakai 1 item per giliran!");
            return; 
        }

        bool isActive = itemListPanel.activeSelf;
        CloseAllSubPanels();
        itemListPanel.SetActive(!isActive);
        if (!isActive) PopulateItemList();
    }

    private void PopulateItemList()
    {
        foreach (Transform child in itemListContainer) Destroy(child.gameObject);

        for (int i = 0; i < battleItems.Count; i++)
        {
            int index = i;
            BattleItemSlot slot = battleItems[i]; 

            if (slot.currentStock > 0)
            {
                GameObject newBtnObj = Instantiate(itemButtonPrefab, itemListContainer);
                TextMeshProUGUI btnText = newBtnObj.GetComponentInChildren<TextMeshProUGUI>();
                btnText.text = slot.itemSO.itemName + " (x" + slot.currentStock + ")";

                Transform iconTransform = newBtnObj.transform.Find("Icon");
                if (iconTransform != null && slot.itemSO.itemIcon != null)
                {
                    iconTransform.GetComponent<Image>().sprite = slot.itemSO.itemIcon;
                }

                Button btn = newBtnObj.GetComponent<Button>();
                btn.onClick.AddListener(() => ExecuteItemButton(index));
            }
        }
    }

    public void ExecuteItemButton(int itemIndex)
    {
        pendingItemIndex = itemIndex;
        StartTargetingMode(ActionType.ITEM);
    }


    // ==========================================
    // BAGIAN 2: MENDETEKSI KLIK MOUSE KE KARAKTER
    // ==========================================

// --- FUNGSI BARU: Eksekusi Target saat Spasi/Enter ditekan ---
    private void ConfirmTarget()
    {
        Unit selectedUnit = validTargets[currentTargetIndex];
        selectedUnit.SetTargetIndicator(false); // Matikan bayangan
        
        state = BattleState.BUSY; 

        if (pendingAction == ActionType.ATTACK)
        {
            StartCoroutine(PlayerAttack(selectedUnit));
        }
        else if (pendingAction == ActionType.SKILL)
        {
            SkillAction skill = playerTeamData[currentPlayerIndex].skills[pendingSkillIndex];
            StartCoroutine(PlayerUseSkill(skill, selectedUnit));
        }
        else if (pendingAction == ActionType.ITEM)
        {
            BattleItemSlot slot = battleItems[pendingItemIndex];
            slot.currentStock--;
            StartCoroutine(PlayerUseItem(slot.itemSO, selectedUnit));
        }
    }


    // ==========================================
    // BAGIAN 3: EKSEKUSI AKSI 
    // ==========================================

    private IEnumerator PlayerDefend()
    {
        Debug.Log(currentActingUnit.unitName + " mengambil posisi bertahan!");
        currentActingUnit.SetDefending(true);
        yield return new WaitForSeconds(0.5f); 
        NextTurnProcessor(); // Lanjut ke orang berikutnya
    }

    private IEnumerator PlayerAttack(Unit targetUnit)
    {
        bool isEnemyDead = false;
        UnitData actingData = playerTeamData[currentPlayerIndex];

        if (actingData.basicAttackType == SkillType.MELEE)
        {
            Vector3 attackPosition = targetUnit.transform.position + new Vector3(-1.5f, 0, 0);
            yield return StartCoroutine(MoveUnit(currentActingUnit.transform, attackPosition));

            currentActingUnit.PlayAttackAnimation();
            yield return new WaitForSeconds(0.5f);

            isEnemyDead = targetUnit.TakeDamage(actingData.baseDamage);
            yield return new WaitForSeconds(0.5f);

            yield return StartCoroutine(MoveUnit(currentActingUnit.transform, currentActingUnit.originalPosition));
        }
        else if (actingData.basicAttackType == SkillType.RANGED)
        {
            currentActingUnit.PlayAttackAnimation(); 
            yield return new WaitForSeconds(0.3f); 

            if (actingData.basicProjectilePrefab != null)
            {
                GameObject proj = Instantiate(actingData.basicProjectilePrefab, currentActingUnit.transform.position, Quaternion.identity);
                yield return StartCoroutine(MoveProjectile(proj, targetUnit.transform.position));

                if (actingData.basicImpactPrefab != null)
                {
                    GameObject impact = Instantiate(actingData.basicImpactPrefab, targetUnit.transform.position, Quaternion.identity);
                    Destroy(impact, 0.5f); 
                }
            }
            isEnemyDead = targetUnit.TakeDamage(actingData.baseDamage);
        }
        
        yield return new WaitForSeconds(0.5f);

        if (isEnemyDead)
        {
            activeEnemyUnits.Remove(targetUnit);
            targetUnit.Die();
        }
        NextTurnProcessor();
    }

    private IEnumerator PlayerUseSkill(SkillAction skill, Unit targetUnit)
    {
        currentActingUnit.ConsumeMP(skill.mpCost);
        bool isEnemyDead = false;

        if (skill.skillType == SkillType.MELEE)
        {
            Vector3 attackPosition = targetUnit.transform.position + new Vector3(-1.5f, 0, 0);
            yield return StartCoroutine(MoveUnit(currentActingUnit.transform, attackPosition));
            
            currentActingUnit.PlayCustomAnimation(skill.animTriggerName);
            yield return new WaitForSeconds(0.5f);
            
            isEnemyDead = targetUnit.TakeDamage(skill.damage);
            yield return new WaitForSeconds(0.5f);
            
            yield return StartCoroutine(MoveUnit(currentActingUnit.transform, currentActingUnit.originalPosition));
        }
        else if (skill.skillType == SkillType.RANGED)
        {
            currentActingUnit.PlayCustomAnimation(skill.animTriggerName);
            yield return new WaitForSeconds(0.3f);
            
            if (skill.skillEffectPrefab != null)
            {
                GameObject proj = Instantiate(skill.skillEffectPrefab, currentActingUnit.transform.position, Quaternion.identity);
                yield return StartCoroutine(MoveProjectile(proj, targetUnit.transform.position));

                if (skill.skillImpactPrefab != null)
                {
                    GameObject impact = Instantiate(skill.skillImpactPrefab, targetUnit.transform.position, Quaternion.identity);
                    Destroy(impact, 0.5f); 
                }
            }
            isEnemyDead = targetUnit.TakeDamage(skill.damage);
        }
        else if (skill.skillType == SkillType.DOT_DEBUFF)
        {
            currentActingUnit.PlayCustomAnimation(skill.animTriggerName);
            yield return new WaitForSeconds(0.5f); 
            
            if (skill.skillEffectPrefab != null)
            {
                GameObject fireEffect = Instantiate(skill.skillEffectPrefab, targetUnit.transform.position, Quaternion.identity);
                Destroy(fireEffect, 1.5f); 
            }
            targetUnit.ApplyBurn(skill.damage, skill.effectDuration);
        }

        yield return new WaitForSeconds(0.5f);

        if (isEnemyDead)
        {
            activeEnemyUnits.Remove(targetUnit);
            targetUnit.Die();
        }
        NextTurnProcessor();
    }

    private IEnumerator PlayerUseItem(ItemData item, Unit targetUnit)
    {
        hasUsedItemThisTurn = true;

        if (item.hpHealAmount > 0) targetUnit.HealHP(item.hpHealAmount);
        if (item.mpHealAmount > 0) targetUnit.HealMP(item.mpHealAmount);

        yield return new WaitForSeconds(1f);

        // Munculkan menu lagi (Free action item)
        state = BattleState.PLAYERTURN;
        actionPanel.SetActive(true);
    }


    // ==========================================
    // BAGIAN 4: PEMINDAH GILIRAN & MUSUH
    // ==========================================

    private void NextTurnProcessor()
    {
        // --- BARU: Matikan ikon karakter yang gilirannya baru saja selesai ---
        if (currentActingUnit != null) currentActingUnit.SetTurnIndicator(false);
        if (activeEnemyUnits.Count == 0)
        {
            state = BattleState.WON;
            EndBattle();
            return;
        }

        currentPlayerIndex++;
        
        if (currentPlayerIndex >= activePlayerUnits.Count)
        {
            state = BattleState.ENEMYTURN;
            StartCoroutine(EnemyTurn());
        }
        else
        {
            StartPlayerTurn();
        }
    }

    private IEnumerator EnemyTurn()
    {
        Debug.Log("Giliran Tim Musuh!");

        for (int i = 0; i < activeEnemyUnits.Count; i++)
        {
            Unit eUnit = activeEnemyUnits[i];
            eUnit.SetTurnIndicator(true);
            
            // Cek status bakar musuh di awal gilirannya
            if (eUnit.burnTurnsLeft > 0)
            {
                bool deadByBurn = eUnit.TakeBurnDamage();
                if (deadByBurn)
                {
                    activeEnemyUnits.Remove(eUnit);
                    eUnit.Die();
                    continue; // Skip ke musuh berikutnya
                }
            }

            eUnit.SetDefending(false);
            yield return new WaitForSeconds(1f); 

            // AI Sederhana: Pilih Pemain Acak
            if (activePlayerUnits.Count == 0) break;
            int randomTarget = Random.Range(0, activePlayerUnits.Count);
            Unit pTarget = activePlayerUnits[randomTarget];

            bool useSkill = false;
            SkillAction chosenSkill = null;
            UnitData eData = enemyTeamData[i];

            if (eData.skills.Count > 0 && Random.Range(0, 100) < 50)
            {
                int randomSkillIdx = Random.Range(0, eData.skills.Count);
                chosenSkill = eData.skills[randomSkillIdx];
                if (eUnit.currentMP >= chosenSkill.mpCost) useSkill = true;
            }

            bool isPlayerDead = false;

            if (useSkill)
            {
                eUnit.ConsumeMP(chosenSkill.mpCost);

                if (chosenSkill.skillType == SkillType.MELEE)
                {
                    Vector3 attackPos = pTarget.transform.position + new Vector3(1.5f, 0, 0);
                    yield return StartCoroutine(MoveUnit(eUnit.transform, attackPos));

                    eUnit.PlayCustomAnimation(chosenSkill.animTriggerName);
                    yield return new WaitForSeconds(0.5f);

                    isPlayerDead = pTarget.TakeDamage(chosenSkill.damage);
                    yield return new WaitForSeconds(0.5f);

                    yield return StartCoroutine(MoveUnit(eUnit.transform, eUnit.originalPosition));
                }
                else if (chosenSkill.skillType == SkillType.RANGED)
                {
                    eUnit.PlayCustomAnimation(chosenSkill.animTriggerName);
                    yield return new WaitForSeconds(0.3f);

                    if (chosenSkill.skillEffectPrefab != null)
                    {
                        GameObject proj = Instantiate(chosenSkill.skillEffectPrefab, eUnit.transform.position, Quaternion.identity);
                        yield return StartCoroutine(MoveProjectile(proj, pTarget.transform.position));

                        if (chosenSkill.skillImpactPrefab != null)
                        {
                            GameObject impact = Instantiate(chosenSkill.skillImpactPrefab, pTarget.transform.position, Quaternion.identity);
                            Destroy(impact, 0.5f);
                        }
                    }
                    isPlayerDead = pTarget.TakeDamage(chosenSkill.damage);
                }
                else if (chosenSkill.skillType == SkillType.DOT_DEBUFF)
                {
                    eUnit.PlayCustomAnimation(chosenSkill.animTriggerName);
                    yield return new WaitForSeconds(0.5f);

                    if (chosenSkill.skillEffectPrefab != null)
                    {
                        GameObject fireEffect = Instantiate(chosenSkill.skillEffectPrefab, pTarget.transform.position, Quaternion.identity);
                        Destroy(fireEffect, 1.5f); 
                    }
                    pTarget.ApplyBurn(chosenSkill.damage, chosenSkill.effectDuration);
                }
            }
            else
            {
                if (eData.basicAttackType == SkillType.MELEE)
                {
                    Vector3 attackPos = pTarget.transform.position + new Vector3(1.5f, 0, 0);
                    yield return StartCoroutine(MoveUnit(eUnit.transform, attackPos));

                    eUnit.PlayAttackAnimation(); 
                    yield return new WaitForSeconds(0.5f);

                    isPlayerDead = pTarget.TakeDamage(eData.baseDamage);
                    yield return new WaitForSeconds(0.5f);

                    yield return StartCoroutine(MoveUnit(eUnit.transform, eUnit.originalPosition));
                }
                else if (eData.basicAttackType == SkillType.RANGED)
                {
                    eUnit.PlayAttackAnimation(); 
                    yield return new WaitForSeconds(0.3f);

                    if (eData.basicProjectilePrefab != null)
                    {
                        GameObject proj = Instantiate(eData.basicProjectilePrefab, eUnit.transform.position, Quaternion.identity);
                        yield return StartCoroutine(MoveProjectile(proj, pTarget.transform.position));

                        if (eData.basicImpactPrefab != null)
                        {
                            GameObject impact = Instantiate(eData.basicImpactPrefab, pTarget.transform.position, Quaternion.identity);
                            Destroy(impact, 0.5f);
                        }
                    }
                    isPlayerDead = pTarget.TakeDamage(eData.baseDamage);
                }
            }
            eUnit.SetTurnIndicator(false);
            if (isPlayerDead)
            {
                activePlayerUnits.Remove(pTarget);
                pTarget.Die();
                if (activePlayerUnits.Count == 0)
                {
                    state = BattleState.LOST;
                    EndBattle();
                    yield break;
                }
            }
        }

        // Kembali ke giliran pemain pertama
        currentPlayerIndex = 0;
        StartPlayerTurn();
    }

    private void EndBattle()
    {
        if (state == BattleState.WON) Debug.Log("Kamu Menang! Pertarungan Selesai.");
        else if (state == BattleState.LOST) Debug.Log("Kamu Kalah... Game Over.");
    }

    private IEnumerator MoveUnit(Transform unitTransform, Vector3 targetPos)
    {
        float lariSpeed = 15f; 
        while (Vector3.Distance(unitTransform.position, targetPos) > 0.01f)
        {
            unitTransform.position = Vector3.MoveTowards(unitTransform.position, targetPos, lariSpeed * Time.deltaTime);
            yield return null; 
        }
        unitTransform.position = targetPos;
    }

    private IEnumerator MoveProjectile(GameObject projectile, Vector3 targetPos)
    {
        float speed = 25f; 
        if (projectile != null && targetPos.x < projectile.transform.position.x)
        {
            projectile.transform.localRotation = Quaternion.Euler(0, 180, 0);
        }
        while (projectile != null && Vector3.Distance(projectile.transform.position, targetPos) > 0.1f)
        {
            projectile.transform.position = Vector3.MoveTowards(projectile.transform.position, targetPos, speed * Time.deltaTime);
            yield return null;
        }
        if (projectile != null) Destroy(projectile); 
    }
}