using System;
using Mirror;
using UnityEngine;

namespace GameLogic.Runtime
{

    public class Skill_2002 : NetSkillBehaviour
    {
        [Header("Prefab")]
        [Tooltip("Bullet_2002")]
        public GameObject bulletPrefab;

        public override void OnStartServer()
        {
            base.OnStartServer();
            
            var ownerEnemy = this.skillCaster.GetComponent<BossEnemy>();
            Vector3 spawnPos = ownerEnemy.skillCasterPoint.position;

            // A total of 36 pieces, with an angle interval of 10 degrees
            for (int i = 0; i < 36; i++)
            {
                // Calculate from 0 to 350 degrees, with each direction being 10 degrees apart.
                float angleInRad = i * 10f * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(angleInRad), Mathf.Sin(angleInRad));

                // Generate a bullet at the spawnPos location
                GameObject bulletObj = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
                
                // Obtain the bullet script, set its flight direction and skillCaster
                var bulletScript = bulletObj.GetComponent<Bullet_2002>();
                bulletScript.flyDir = dir;
                bulletScript.skillCaster = this.skillCaster;

                // The server-generated content needs to be handed over to Mirror for hosting.
                NetworkServer.Spawn(bulletObj);
            }

            // After generating 36 bullets, immediately destroy itself (this empty GameObject of Skill_2002)
            NetworkServer.Destroy(this.gameObject);
        }
    }
}