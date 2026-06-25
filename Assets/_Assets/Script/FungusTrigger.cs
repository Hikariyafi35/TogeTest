using UnityEngine;
using Fungus;

public class FungusTrigger : MonoBehaviour
{
    [Header("Referensi Fungus")]
    [Tooltip("Tarik GameObject Flowchart Fungus dari Hierarchy ke sini")]
    public Flowchart targetFlowchart;

    [Tooltip("Ketik nama Block Fungus yang ingin diputar persis seperti di Flowchart")]
    public string blockName = "MulaiScene1";

    // Fungsi bawaan Unity untuk mendeteksi objek yang masuk ke area trigger (versi 2D)
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Cek apakah yang menyentuh collider ini adalah Player
        if (collision.CompareTag("Player"))
        {
            // Pastikan target flowchart tidak kosong
            if (targetFlowchart != null)
            {
                // Memerintahkan Fungus untuk menjalankan block tersebut
                targetFlowchart.ExecuteBlock(blockName);
                
                // (Opsional) Hancurkan trigger ini agar cutscene tidak terputar dua kali 
                // jika player mundur dan maju lagi
                Destroy(gameObject);
            }
            else
            {
                Debug.LogWarning("Flowchart belum dimasukkan ke dalam script FungusTrigger!");
            }
        }
    }
}
