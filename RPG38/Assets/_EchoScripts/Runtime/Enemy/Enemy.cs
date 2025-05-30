using System;
using Mirror;
using Unity.VisualScripting;
using UnityEngine;

namespace GameLogic.Runtime
{
    public class Enemy : NetBehaviour,IChaAttr
    {
        public virtual string Tag => "Enemy";
        public float maxHealth = 100;
        public float currentHealth = 100;
        public float attack = 10;
        public float MaxHealth { get => this.maxHealth; set => this.maxHealth = value; }
        public float CurrentHealth { get=>this.currentHealth; set=>this.currentHealth = value; }
        public float CurrentMp { get; set; }
        public float MaxMp { get; set; }
        public float Attack { get=>this.attack; set=>this.attack = value; }
        
        public bool IsDead => this.currentHealth <= 0;

        public void AddHealth(float amount)
        {
            this.currentHealth += amount;
            this.currentHealth = Mathf.Clamp(this.currentHealth, 0, this.maxHealth);
        }

        public void AddMp(float amount)
        {
            this.CurrentMp += amount;
            this.CurrentMp = Mathf.Clamp(this.CurrentMp, 0, 100);
        }

        public virtual void BeHurt(IChaAttr attacker, float damage)
        {
            this.CurrentHealth -= damage;
            if (this.CurrentHealth <= 0)
            {
                OnEnemyDeath?.Invoke(this);
                //NetworkServer.Destroy(this.gameObject);
            }
        }

        public void AddMaxHealth(float amount)
        {
            this.MaxHealth += amount;
        }

        public void AddMaxMp(float amount)
        {
            this.MaxMp += amount;
        }

        public void AddAttack(float amount)
        {
            this.Attack += amount;
        }

        public static event Action<Enemy> OnEnemyDeath;
    }
}