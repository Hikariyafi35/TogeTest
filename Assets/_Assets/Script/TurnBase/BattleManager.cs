using System.Collections;
using UnityEngine;
using UnityEngine.UI; 
using TMPro;

public enum BattleState { START, PLAYERTURN, ENEMYTURN, WON, LOST }

public class BattleManager : MonoBehaviour
{
    [Header("Status Pertarungan")]
    public BattleState state;

    [Header("Referensi Cetakan (Prefab)")]
    public GameObject battleUnitPrefab;

    [Header("Titik Berdiri (Spawn Points)")]
    public Transform playerSpawnPoint;
    public Transform enemySpawnPoint;

    [Header("Data Pertarungan Saat Ini")]
    public UnitData playerData; 
    public UnitData enemyData;  

    [Header("UI Pertarungan")]
    public GameObject actionPanel; // Wadah tombol-tombol aksi

    private Unit playerUnit;
    private Unit enemyUnit;

    [Header("UI Skill Dinamis")]
    public GameObject skillListPanel;      // Masukkan SkillListPanel (kotak oranye)
    public Transform skillListContainer;   // Masukkan SkillListPanel juga (sebagai induk tombol)
    public GameObject skillItemPrefab;     // Masukkan SkillButton_Prefab dari folder Prefabs

    private void Start()
    {
        state = BattleState.START;
        
        // Sembunyikan UI saat awal pertarungan
        actionPanel.SetActive(false); 
        
        StartCoroutine(SetupBattle());
    }

    private IEnumerator SetupBattle()
    {
        // --- SPAWN PLAYER ---
        GameObject playerGO = Instantiate(battleUnitPrefab, playerSpawnPoint);
        playerUnit = playerGO.GetComponent<Unit>();
        playerUnit.SetupUnit(playerData);

        // --- SPAWN ENEMY ---
        GameObject enemyGO = Instantiate(battleUnitPrefab, enemySpawnPoint);
        enemyUnit = enemyGO.GetComponent<Unit>();
        enemyUnit.SetupUnit(enemyData);

        yield return new WaitForSeconds(1.5f);

        state = BattleState.PLAYERTURN;
        PlayerTurn();
    }

    private void PlayerTurn()
    {
        Debug.Log("Giliran Pemain!");

        // --- BARU: Lepas status bertahan saat giliran baru dimulai ---
        playerUnit.SetDefending(false);

        actionPanel.SetActive(true);
    }
    public void OnDefendButton()
    {
        if (state != BattleState.PLAYERTURN) return;

        // Sembunyikan UI
        actionPanel.SetActive(false);
        skillListPanel.SetActive(false);

        StartCoroutine(PlayerDefend());
    }
    private IEnumerator PlayerDefend()
    {
        Debug.Log(playerUnit.unitName + " mengambil posisi bertahan!");

        // 1. Aktifkan status bertahan & munculkan ikon sprite tameng
        playerUnit.SetDefending(true);

        // 2. Beri jeda singkat agar pemain melihat ikon muncul sebelum giliran pindah
        yield return new WaitForSeconds(0.5f); // Jeda dikurangi dari 1 detik menjadi 0.5 detik

        // 3. Langsung lemparkan giliran ke musuh
        state = BattleState.ENEMYTURN;
        StartCoroutine(EnemyTurn());
    }
    
    public void ToggleSkillPanel()
    {
        if (state != BattleState.PLAYERTURN) return;

        // Balikkan status aktif panel (kalau mati jadi nyala, kalau nyala jadi mati)
        bool isActive = skillListPanel.activeSelf;
        skillListPanel.SetActive(!isActive);

        // Jika panel baru saja dibuka, isi dengan daftar skill
        if (!isActive)
        {
            PopulateSkillList();
        }
    }
    // --- 2. FUNGSI MEMBUAT TOMBOL DINAMIS ---
    private void PopulateSkillList()
    {
        // Bersihkan tombol-tombol lama (jika ada) agar tidak menumpuk
        foreach (Transform child in skillListContainer)
        {
            Destroy(child.gameObject);
        }

        // Loop sebanyak jumlah skill yang dimiliki karakter
        for (int i = 0; i < playerData.skills.Count; i++)
        {
            int index = i; // Penting disalin ke variabel lokal untuk Button Listener
            SkillAction skill = playerData.skills[i];

            // Munculkan cetakan tombol
            GameObject newBtnObj = Instantiate(skillItemPrefab, skillListContainer);
            
            // Ubah teksnya menjadi: "Nama Skill (MP: X)"
            TextMeshProUGUI btnText = newBtnObj.GetComponentInChildren<TextMeshProUGUI>();
            btnText.text = skill.skillName + " (MP: " + skill.mpCost + ")";

            // Pasang fungsi klik ke tombol tersebut secara otomatis lewat kode
            Button btn = newBtnObj.GetComponent<Button>();
            btn.onClick.AddListener(() => ExecuteSkill(index));
        }
    }

    // --- 3. FUNGSI EKSEKUSI SKILL (Dipanggil oleh tombol di dalam panel oranye) ---
    public void ExecuteSkill(int skillIndex)
    {
        SkillAction chosenSkill = playerData.skills[skillIndex];

        // Cek MP
        if (playerUnit.currentMP >= chosenSkill.mpCost)
        {
            // Tutup semua UI
            skillListPanel.SetActive(false); 
            actionPanel.SetActive(false); 
            
            StartCoroutine(PlayerUseSkill(chosenSkill));
        }
        else
        {
            Debug.Log("MP Tidak Cukup untuk " + chosenSkill.skillName + "!");
        }
    }

    // --- FUNGSI INI AKAN DIPANGGIL OLEH TOMBOL UI ---
    public void OnAttackButton()
    {
        // Cegah spam klik jika bukan giliran player
        if (state != BattleState.PLAYERTURN) return;

        // Sembunyikan UI agar pemain tidak bisa klik 2 kali
        actionPanel.SetActive(false); 
        
        // Mulai proses serang
        StartCoroutine(PlayerAttack());
    }

    private IEnumerator PlayerAttack()
    {
        Debug.Log(playerUnit.unitName + " melakukan Basic Attack!");

        // --- 1. FASE MAJU (HANYA JIKA MELEE) ---
        if (playerData.basicAttackType == SkillType.MELEE)
        {
            Vector3 attackPosition = enemyUnit.transform.position + new Vector3(-1.5f, 0, 0);
            yield return StartCoroutine(MoveUnit(playerUnit.transform, attackPosition));
        }

        // --- 2. FASE MUKUL ---
        playerUnit.PlayAttackAnimation();
        yield return new WaitForSeconds(0.5f); 

        bool isEnemyDead = enemyUnit.TakeDamage(playerUnit.baseDamage);
        yield return new WaitForSeconds(0.5f); 

        // --- 3. FASE MUNDUR (HANYA JIKA MELEE) ---
        if (playerData.basicAttackType == SkillType.MELEE)
        {
            yield return StartCoroutine(MoveUnit(playerUnit.transform, playerSpawnPoint.position));
            yield return new WaitForSeconds(0.5f); 
        }

        // --- 4. CEK HASIL ---
        if (isEnemyDead)
        {
            state = BattleState.WON;
            EndBattle();
        }
        else
        {
            state = BattleState.ENEMYTURN;
            StartCoroutine(EnemyTurn());
        }
    }
    // --- FUNGSI INI AKAN DIPANGGIL OLEH TOMBOL SKILL UI ---
// --- FUNGSI INI DIPANGGIL OLEH TOMBOL (KITA KIRIM ANGKA INDEX 0 ATAU 1) ---
    public void OnSkillButton(int skillIndex)
    {
        if (state != BattleState.PLAYERTURN) return;

        // Cegah error jika karakter tidak punya skill di index tersebut
        if (skillIndex >= playerData.skills.Count) 
        {
            Debug.LogWarning("Skill belum diatur di Unit Data!");
            return;
        }

        SkillAction chosenSkill = playerData.skills[skillIndex];

        if (playerUnit.currentMP >= chosenSkill.mpCost)
        {
            actionPanel.SetActive(false); 
            StartCoroutine(PlayerUseSkill(chosenSkill)); // Lempar paket data skill ke wasit
        }
        else
        {
            Debug.Log("MP Tidak Cukup!"); 
        }
    }

    private IEnumerator PlayerUseSkill(SkillAction skill)
    {
        Debug.Log(playerUnit.unitName + " menggunakan " + skill.skillName + "!");
        playerUnit.ConsumeMP(skill.mpCost);

        // --- FASE MAJU (HANYA JIKA MELEE) ---
        if (skill.skillType == SkillType.MELEE)
        {
            Vector3 attackPosition = enemyUnit.transform.position + new Vector3(-1.5f, 0, 0);
            yield return StartCoroutine(MoveUnit(playerUnit.transform, attackPosition));
        }

        // --- FASE EKSEKUSI ANIMASI ---
        playerUnit.PlayCustomAnimation(skill.animTriggerName);
        yield return new WaitForSeconds(0.5f); // Sesuaikan dengan durasi animasimu

        bool isEnemyDead = enemyUnit.TakeDamage(skill.damage); 
        yield return new WaitForSeconds(0.5f); 

        // --- FASE MUNDUR (HANYA JIKA MELEE) ---
        if (skill.skillType == SkillType.MELEE)
        {
            yield return StartCoroutine(MoveUnit(playerUnit.transform, playerSpawnPoint.position));
            yield return new WaitForSeconds(0.5f); 
        }

        // --- CEK HASIL ---
        if (isEnemyDead)
        {
            state = BattleState.WON;
            EndBattle();
        }
        else
        {
            state = BattleState.ENEMYTURN;
            StartCoroutine(EnemyTurn());
        }
    }

    private IEnumerator EnemyTurn()
    {
        Debug.Log("Giliran Musuh!");
        enemyUnit.SetDefending(false);
        yield return new WaitForSeconds(1f); // Jeda musuh "berpikir"

        // --- KECERDASAN BUATAN (AI) MUSUH SEDERHANA ---
        bool useSkill = false;
        SkillAction chosenSkill = null;

        // 1. Cek apakah musuh punya skill di SO-nya
        if (enemyData.skills.Count > 0)
        {
            // 2. Beri peluang 50% musuh akan mencoba pakai skill (agar tidak spam skill terus)
            if (Random.Range(0, 100) < 50) 
            {
                // Pilih skill secara acak dari daftar skill yang dia miliki
                int randomIndex = Random.Range(0, enemyData.skills.Count);
                chosenSkill = enemyData.skills[randomIndex];

                // 3. Pastikan MP musuh cukup untuk memanggil skill tersebut
                if (enemyUnit.currentMP >= chosenSkill.mpCost)
                {
                    useSkill = true;
                }
            }
        }

        // Variabel untuk menampung apakah serangan (apa pun itu) berhasil membunuh player
        bool isPlayerDead = false;

        // --- EKSEKUSI SERANGAN ---
        if (useSkill)
        {
            // MUSUH MENGGUNAKAN SKILL
            Debug.Log(enemyUnit.unitName + " menggunakan skill: " + chosenSkill.skillName + "!");
            enemyUnit.ConsumeMP(chosenSkill.mpCost);

            // FASE MAJU (Hanya jika Melee)
            if (chosenSkill.skillType == SkillType.MELEE)
            {
                // Musuh bergeraknya ke arah kiri (positif 1.5f)
                Vector3 attackPosition = playerUnit.transform.position + new Vector3(1.5f, 0, 0);
                yield return StartCoroutine(MoveUnit(enemyUnit.transform, attackPosition));
            }

            // MUKUL PAKAI ANIMASI SKILL
            enemyUnit.PlayCustomAnimation(chosenSkill.animTriggerName);
            yield return new WaitForSeconds(0.5f);

            // PENTING: Gunakan damage dari skill, bukan base damage!
            isPlayerDead = playerUnit.TakeDamage(chosenSkill.damage);
            yield return new WaitForSeconds(0.5f);

            // FASE MUNDUR (Hanya jika Melee)
            if (chosenSkill.skillType == SkillType.MELEE)
            {
                yield return StartCoroutine(MoveUnit(enemyUnit.transform, enemySpawnPoint.position));
                yield return new WaitForSeconds(0.5f);
            }
        }
        else
        {
            // MUSUH MENGGUNAKAN BASIC ATTACK
            Debug.Log(enemyUnit.unitName + " melakukan Basic Attack!");
            
            // FASE MAJU (HANYA JIKA MELEE)
            if (enemyData.basicAttackType == SkillType.MELEE)
            {
                Vector3 attackPosition = playerUnit.transform.position + new Vector3(1.5f, 0, 0);
                yield return StartCoroutine(MoveUnit(enemyUnit.transform, attackPosition));
            }
            
            enemyUnit.PlayAttackAnimation();
            yield return new WaitForSeconds(0.5f);

            isPlayerDead = playerUnit.TakeDamage(enemyUnit.baseDamage);
            yield return new WaitForSeconds(0.5f);

            // FASE MUNDUR (HANYA JIKA MELEE)
            if (enemyData.basicAttackType == SkillType.MELEE)
            {
                yield return StartCoroutine(MoveUnit(enemyUnit.transform, enemySpawnPoint.position));
                yield return new WaitForSeconds(0.5f);
            }
        }

        // --- CEK HASIL PERTARUNGAN ---
        if (isPlayerDead)
        {
            state = BattleState.LOST;
            EndBattle();
        }
        else
        {
            state = BattleState.PLAYERTURN;
            PlayerTurn();
        }
    }

    private void EndBattle()
    {
        if (state == BattleState.WON)
        {
            Debug.Log("Kamu Menang! Pertarungan Selesai.");
            // Logika kembali ke Scene Eksplorasi akan kita buat nanti
        }
        else if (state == BattleState.LOST)
        {
            Debug.Log("Kamu Kalah... Game Over.");
        }
    }
    // --- FUNGSI BARU: Menggerakkan karakter secara halus ---
    private IEnumerator MoveUnit(Transform unitTransform, Vector3 targetPos)
    {
        float lariSpeed = 15f; // Kecepatan maju/mundur (bisa kamu ubah)
        
        // Selama jarak karakter dan target masih jauh, terus geser posisinya
        while (Vector3.Distance(unitTransform.position, targetPos) > 0.01f)
        {
            // Vector3.MoveTowards mengatur pergerakan dari titik A ke titik B
            unitTransform.position = Vector3.MoveTowards(unitTransform.position, targetPos, lariSpeed * Time.deltaTime);
            yield return null; // Tunggu frame berikutnya
        }
        
        // Pastikan posisi akhirnya pas
        unitTransform.position = targetPos; 
    }
}