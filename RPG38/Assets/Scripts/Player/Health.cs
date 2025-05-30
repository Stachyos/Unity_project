using UnityEngine;

public class Health : MonoBehaviour
{
    [Header("最大生命值")]
    [SerializeField] private int maxHealth = 100;

    private int currentHealth;
    private Animator anim;

    public bool IsDead => currentHealth <= 0;

    private void Awake()
    {
        currentHealth = maxHealth;
        anim = GetComponentInChildren<Animator>();
    }

    public void TakeDamage(int damage)
    {
        if (IsDead) return;

        currentHealth -= damage;
        Debug.Log($"{gameObject.name} took {damage}, left {currentHealth}");

        if (currentHealth <= 0)
        {

            SendMessage("OnDeath", SendMessageOptions.DontRequireReceiver);
        }
        else
        {

            SendMessage("OnGetHit", SendMessageOptions.DontRequireReceiver);
        }
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }
}