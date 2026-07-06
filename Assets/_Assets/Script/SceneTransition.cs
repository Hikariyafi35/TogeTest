using UnityEngine;
using UnityEngine.SceneManagement; // Wajib untuk pindah scene

public class SceneTransition : MonoBehaviour
{
    [Header("Nama Scene Tujuan")]
    public string sceneToLoad; // Ketik nama scene-nya di Inspector (misal: BattleScene1)

    // Fungsi ini otomatis berjalan saat ada objek 2D yang menabrak area ini
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Mengecek apakah yang menabrak itu memiliki Tag "Player"
        if (collision.CompareTag("Player"))
        {
            Debug.Log("Player masuk ke pintu! Pindah ke: " + sceneToLoad);
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}