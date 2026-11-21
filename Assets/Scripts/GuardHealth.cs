
using UnityEngine;

public class GuardHealth : MonoBehaviour
{
    public int maxHealth = 60;

    public int currentHealth;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (currentHealth <= 0)
            return;

        currentHealth = Mathf.Max(
            0,
            currentHealth - damage
        );

        Debug.Log(
            name + " HP: " + currentHealth
        );

        if (currentHealth == 0)
        {
            Debug.Log(name + " defeated!");
            gameObject.SetActive(false);
        }
    }
}
