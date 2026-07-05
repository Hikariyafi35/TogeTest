using UnityEngine;
using UnityEngine.UI; 
using TMPro;
public class Unit : MonoBehaviour
{
    [Header("Referensi Komponen (Tarik dari Child)")]
    public SpriteRenderer spriteRenderer;
    public Animator animator;
    public Image hpFillImage; 
    public Image mpFillImage; // --- BARU: Referensi Bar Mana (Biru) ---
    public GameObject damageTextPrefab; 

    [Header("Status Dinamis (Diisi Otomatis)")]
    public string unitName;
    public int currentHP;
    public int maxHP;
    public int currentMP; 
    public int maxMP;     
    public int baseDamage;
    public bool isPlayer;

    public GameObject defendIcon;
    public bool isDefending;
    [Header("Referensi Targeting (Baru)")]
    public GameObject targetShadow;
    public GameObject turnIndicator; 
    public bool isDead = false;

    public void SetupUnit(UnitData data)
    {
        unitName = data.unitName;
        isPlayer = data.isPlayerTeam;
        
        maxHP = data.maxHP;
        currentHP = maxHP; 
        
        maxMP = data.maxMP; 
        currentMP = maxMP;  

        baseDamage = data.baseDamage;

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = data.unitSprite;
            spriteRenderer.flipX = !isPlayer;
        }

        if (animator != null && data.animatorController != null)
        {
            animator.runtimeAnimatorController = data.animatorController;
        }
        if (defendIcon != null)
        {
            // Jika Player, tameng maju ke kanan (X positif). Jika Musuh, maju ke kiri (X negatif)
            float posX = isPlayer ? 1.0f : -1.0f; 
            
            // Atur posisi lokalnya (kamu bisa ubah Y menjadi 0.5f agar tamengnya tidak menyentuh tanah)
            defendIcon.transform.localPosition = new Vector3(posX, 0.5f, 0); 
        }

        UpdateHealthBar();
        UpdateManaBar(); 
    }
    public void SetDefending(bool status)
    {
        isDefending = status;
        
        // Nyalakan atau matikan sprite penanda sesuai status (true/false)
        if (defendIcon != null)
        {
            defendIcon.SetActive(isDefending);
        }
    }
    public bool TakeDamage(int damageAmount)
    {
        bool wasDefending = isDefending;
        // --- BARU: Logika Pengurangan Damage karena Defend ---
        if (isDefending)
        {
            // Potong damage jadi setengah. Mathf.Max(1, ...) memastikan minimal damage adalah 1 (tidak jadi 0)
            damageAmount = Mathf.Max(1, damageAmount / 2); 
            SetDefending(false);
            Debug.Log(unitName + " sedang menangkis! Damage berkurang drastis.");
        }

        currentHP -= damageAmount;
        if (currentHP <= 0) currentHP = 0;

        UpdateHealthBar();

        if (animator != null)
        {
            animator.SetTrigger("TakeHit");
        }

        if (damageTextPrefab != null)
        {
            Vector3 spawnPosition = transform.position + new Vector3(0, 1.0f, 0);
            GameObject floatingText = Instantiate(damageTextPrefab, spawnPosition, Quaternion.identity);
            
            // --- BARU: Logika Teks Melayang ---
            if (wasDefending)
            {
                // Munculkan tulisan "Blocked" dan angka damage di bawahnya
                floatingText.GetComponent<DamageText>().Setup("Blocked\n" + damageAmount.ToString());
            }
            else
            {
                // Jika tidak defend, munculkan angka normal
                floatingText.GetComponent<DamageText>().Setup(damageAmount.ToString());
            }
        }

        return currentHP == 0; 
    }

    // --- BARU: Fungsi untuk mengurangi MP ---
    public bool ConsumeMP(int amount)
    {
        if (currentMP >= amount)
        {
            currentMP -= amount;
            UpdateManaBar();
            return true; // Berhasil menggunakan MP
        }
        return false; // Gagal, MP tidak cukup
    }

    public void PlayAttackAnimation()
    {
        if (animator != null) animator.SetTrigger("Attack"); 
    }

    // --- BARU: Putar animasi Skill ---
    public void PlaySkillAnimation()
    {
        if (animator != null) animator.SetTrigger("Skill"); // Pastikan buat Trigger "Skill" di Animator!
    }

    private void UpdateHealthBar()
    {
        if (hpFillImage != null) hpFillImage.fillAmount = (float)currentHP / maxHP;
    }

    // --- BARU: Pembaruan UI Mana ---
    private void UpdateManaBar()
    {
        // Cegah pembagian dengan nol jika karakter (seperti musuh) tidak punya MP
        if (mpFillImage != null && maxMP > 0)
        {
            mpFillImage.fillAmount = (float)currentMP / maxMP;
        }
        else if (mpFillImage != null)
        {
            mpFillImage.fillAmount = 0; // Kosongkan bar jika maxMP = 0
        }
    }
    // Fungsi baru untuk memutar animasi sesuai nama Trigger yang kita tulis di SO
    public void PlayCustomAnimation(string triggerName)
    {
        if (animator != null && !string.IsNullOrEmpty(triggerName))
        {
            animator.SetTrigger(triggerName);
        }
    }
    // --- FUNGSI BARU: Untuk memulihkan HP karena Item ---
    public void HealHP(int amount)
    {
        currentHP += amount;

        // Mencegah HP melebihi batas maksimal
        if (currentHP > maxHP)
        {
            currentHP = maxHP;
        }

        UpdateHealthBar();

        // Munculkan teks melayang (Kamu bisa mengubah warnanya menjadi hijau nanti di DamageText)
        if (damageTextPrefab != null)
        {
            Vector3 spawnPosition = transform.position + new Vector3(0, 1.0f, 0);
            GameObject floatingText = Instantiate(damageTextPrefab, spawnPosition, Quaternion.identity);
            floatingText.GetComponent<DamageText>().Setup("+" + amount.ToString());
        }
    }
    // --- FUNGSI BARU: Untuk memulihkan MP ---
    public void HealMP(int amount)
    {
        if (amount <= 0) return; // Jika item tidak memulihkan MP, lewati fungsi ini

        currentMP += amount;
        if (currentMP > maxMP) currentMP = maxMP;

        UpdateManaBar();

        if (damageTextPrefab != null)
        {
            // Munculkan teks sedikit lebih tinggi agar tidak menumpuk dengan teks HP
            Vector3 spawnPosition = transform.position + new Vector3(0, 1.5f, 0);
            GameObject floatingText = Instantiate(damageTextPrefab, spawnPosition, Quaternion.identity);

            floatingText.GetComponent<DamageText>().Setup("+" + amount + " MP");

            // Opsional: Ubah warna teks menjadi Biru (Cyan) khusus untuk MP
            TextMeshPro textMesh = floatingText.GetComponent<TextMeshPro>();
            if (textMesh != null) textMesh.color = Color.cyan;
        }
    }
    [Header("Status Efek (Debuff)")]
    // --- BARU: Variabel untuk mengingat efek terbakar ---
    public int burnTurnsLeft = 0;
    public int burnDamagePerTurn = 0;

    // Fungsi untuk menerima status terbakar dari Wizard
    public void ApplyBurn(int damage, int duration)
    {
        burnTurnsLeft = duration;
        burnDamagePerTurn = damage;
        Debug.Log(unitName + " terbakar! Menerima " + damage + " DMG selama " + duration + " turn.");
    }

    // Fungsi ini dipanggil Wasit di akhir giliran musuh
    public bool TakeBurnDamage()
    {
        if (burnTurnsLeft > 0)
        {
            burnTurnsLeft--; // Kurangi sisa turn
            Debug.Log("Efek Burn melukai " + unitName + "!");
            return TakeDamage(burnDamagePerTurn); // Gunakan fungsi TakeDamage yang sudah ada
        }
        return false;
    }
    // FUNGSI BARU 1: Menyalakan/Mematikan Bayangan
    public void SetTargetIndicator(bool isActive)
    {
        if (targetShadow != null) targetShadow.SetActive(isActive);
    }

    // --- FUNGSI BARU: Menyalakan/Mematikan Indikator Turn ---
    public void SetTurnIndicator(bool isActive)
    {
        if (isDead) return; // Jika mati, jangan nyalakan indikator
        if (turnIndicator != null) turnIndicator.SetActive(isActive);
    }

    // --- FUNGSI BARU: Logika Mati (Tanpa Destroy GameObject) ---
    public void Die()
    {
        isDead = true;
        currentHP = 0;
        UpdateHealthBar();

        if (animator != null) animator.SetTrigger("Die"); // Memanggil animasi mati

        // Matikan semua indikator agar bersih
        SetTargetIndicator(false);
        SetTurnIndicator(false);
        if (defendIcon != null) defendIcon.SetActive(false);

        Debug.Log(unitName + " telah gugur!");
    }
}