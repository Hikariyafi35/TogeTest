using UnityEngine;

public class NpcFollower : MonoBehaviour
{
    [Header("Pengaturan Target")]
    public Transform playerTarget; // Masukkan karakter Bob ke sini
    
    [Header("Pengaturan Gerak & Animasi")]
    public float speed = 4f;
    public float stoppingDistance = 1.5f; // Jarak NPC berhenti di belakang player
    public Animator animator; // --- BARU: Referensi untuk memutar animasi ---
    
    [Header("Status (Bisa diubah dari Fungus)")]
    public bool isFollowing = false; // Jika false, NPC diam di tempat

    private void Update()
    {
        // Hanya bergerak jika isFollowing = true dan targetnya ada
        if (isFollowing && playerTarget != null)
        {
            // Hitung jarak antara NPC dan Player
            float distance = Vector2.Distance(transform.position, playerTarget.position);

            // Jika jaraknya masih lebih besar dari batas berhenti, terus maju
            if (distance > stoppingDistance)
            {
                transform.position = Vector2.MoveTowards(transform.position, playerTarget.position, speed * Time.deltaTime);
                
                // --- BARU: Nyalakan animasi jalan ---
                if (animator != null) animator.SetBool("isWalking", true);

                // --- UBAH: Logika Flip Dibalik ---
                if (playerTarget.position.x < transform.position.x)
                    transform.localScale = new Vector3(1, 1, 1);  // Sekarang hadap kiri pakai 1
                else
                    transform.localScale = new Vector3(-1, 1, 1); // Sekarang hadap kanan pakai -1
            }
            else
            {
                // NPC sudah cukup dekat, jadi berhenti
                // --- BARU: Matikan animasi jalan ---
                if (animator != null) animator.SetBool("isWalking", false);
            }
        }
        else
        {
            // Jika isFollowing diubah jadi false, pastikan animasi jalannya mati
            if (animator != null) animator.SetBool("isWalking", false);
        }
    }

    // --- FUNGSI INI YANG AKAN DIPANGGIL OLEH FUNGUS ---
    public void StartFollowing()
    {
        isFollowing = true;
        Debug.Log(gameObject.name + " mulai mengikuti player!");
    }
}