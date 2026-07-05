using UnityEngine;
using UnityEngine.Playables;
using Fungus;
using UnityEngine.InputSystem;
public class CutsceneManager : MonoBehaviour
{
    [Header("Referensi Timeline")]
    [Tooltip("Masukkan GameObject yang memiliki komponen Playable Director (Timeline)")]
    public PlayableDirector timelineDirector;

    [Header("Referensi Fungus")]
    public Flowchart targetFlowchart;
    public string dialogBlockName = "MulaiScene1";

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // 1. Matikan pergerakan Player
            movement player = collision.GetComponentInParent<movement>();
            if (player != null)
            {
                player.SetMovementEnabled(false);
            }

            // 2. Daftarkan event: "Apa yang terjadi setelah Timeline selesai?"
            // Kita suruh Unity menjalankan fungsi OnTimelineFinished
            timelineDirector.stopped += OnTimelineFinished;

            // 3. Mainkan animasi jalan otomatis di Timeline
            timelineDirector.Play();

            // 4. Matikan collider ini agar cutscene tidak ter-trigger dua kali
            GetComponent<Collider2D>().enabled = false;
        }
    }

    // Fungsi ini otomatis dipanggil oleh Unity tepat saat durasi Timeline habis
    private void OnTimelineFinished(PlayableDirector director)
    {
        // Cabut pendaftaran event agar tidak terjadi memory leak (penting!)
        timelineDirector.stopped -= OnTimelineFinished;

        // 5. Setelah Timeline selesai, panggil dialog Fungus
        if (targetFlowchart != null)
        {
            targetFlowchart.ExecuteBlock(dialogBlockName);
        }
        else
        {
            Debug.LogWarning("Flowchart belum dimasukkan!");
        }
    }
}
