using UnityEngine;
using TMPro;

public class MeteorController : MonoBehaviour
{
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private int health = 3;
    [SerializeField] private TMP_Text textHealth;
    [SerializeField] private float jumpForce = 5f;

    private void Start()
    {
        UpdateUI();
        rb.velocity = Vector2.right; // Di chuyển ngang ban đầu
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Missile"))
        {
            TakeDamage(1);
            Destroy(other.gameObject); // Hủy missile khi trúng
        }

        if (other.CompareTag("Wall"))
        {
            float posX = transform.position.x;
            if (posX > 0)
                rb.AddForce(Vector2.left * 8f, ForceMode2D.Impulse);
            else
                rb.AddForce(Vector2.right * 8f, ForceMode2D.Impulse);
        }

        if (other.CompareTag("Ground"))
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        }
    }

    private void TakeDamage(int damage)
    {
        health -= damage;
        UpdateUI();

        if (health <= 0)
            Destroy(gameObject);
    }

    private void UpdateUI()
    {
        if (textHealth != null)
            textHealth.text = health.ToString();
    }
}
