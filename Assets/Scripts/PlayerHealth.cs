
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;

    public int CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }

    private Rigidbody2D rb;

    void Awake()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        CurrentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
    }

    public void TakeDamage(int damage)
    {
        if (IsDead || damage <= 0)
            return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);

        Debug.Log("Player HP: " + CurrentHealth);

        if (CurrentHealth == 0)
        {
            Die();
        }
    }

    void Die()
    {
        IsDead = true;

        Debug.Log("GAME OVER!");

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }
    }

    void OnGUI()
    {
        GUI.Box(
            new Rect(10, 10, 140, 35),
            "HP: " + CurrentHealth + "/" + maxHealth
        );

        if (IsDead)
        {
            GUI.Box(
                new Rect(
                    Screen.width / 2f - 90,
                    Screen.height / 2f - 25,
                    180,
                    50
                ),
                "GAME OVER"
            );
        }
    }
}
