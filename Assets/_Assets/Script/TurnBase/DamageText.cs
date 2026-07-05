using UnityEngine;
using TMPro; // Wajib untuk menggunakan TextMeshPro
using System.Collections;

public class DamageText : MonoBehaviour
{
    private TextMeshPro textMesh;
    private Color textColor;
    
    [Header("Pengaturan Animasi")]
    public float moveSpeed = 2f; // Kecepatan melayang ke atas
    public float fadeSpeed = 2f; // Kecepatan memudar (hilang)

// --- UBAH int damageAmount MENJADI string textContent ---
    public void Setup(string textContent)
    {
        textMesh = GetComponent<TextMeshPro>();
        textMesh.text = textContent; // --- UBAH INI JUGA ---
        textColor = textMesh.color;
        
        StartCoroutine(FloatAndFade());
    }

    private IEnumerator FloatAndFade()
    {
        float timer = 0f;
        
        // Animasi akan berjalan selama 1 detik
        while (timer < 1f)
        {
            // 1. Geser posisi ke atas sedikit demi sedikit
            transform.position += new Vector3(0, moveSpeed * Time.deltaTime, 0);
            
            // 2. Kurangi nilai Alpha (transparansi) agar memudar
            textColor.a -= fadeSpeed * Time.deltaTime;
            textMesh.color = textColor;
            
            timer += Time.deltaTime;
            yield return null; // Tunggu frame berikutnya
        }
        
        // Hancurkan objek setelah 1 detik agar tidak nyampah di memori
        Destroy(gameObject);
    }
}