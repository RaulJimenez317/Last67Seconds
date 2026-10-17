using Codice.Characters;
using UnityEngine;

namespace Codice.Bosses
{
    public sealed class BossMud : MonoBehaviour
    {
        BossCharacter owner;
        float remaining = 8, tick;
        public static void Create(BossCharacter boss, Vector3 position)
        {
            // Use the walkable floor, never the player's head or a summoned enemy.
            if (UnityEngine.AI.NavMesh.SamplePosition(position, out var floor, 4, UnityEngine.AI.NavMesh.AllAreas)) position = floor.position;
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder); obj.name = "Boss mud (8 seconds)";
            Destroy(obj.GetComponent<Collider>()); obj.transform.position = position + Vector3.up * .025f;
            obj.transform.localScale = new Vector3(5, .025f, 5);
            obj.GetComponent<Renderer>().sharedMaterial = boss.mudMaterial;
            obj.AddComponent<BossMud>().owner = boss; boss.Track(obj);
        }
        void Update()
        {
            if (!owner || !owner.IsAlive || !owner.Target || !owner.Target.State.IsAlive) { Destroy(gameObject); return; }
            if (owner.Paused) return;
            remaining -= Time.deltaTime; tick -= Time.deltaTime;
            if (remaining <= 0) { Destroy(gameObject); return; }
            var player = owner.Target; var delta = player.transform.position - transform.position;
            if (Mathf.Abs(delta.y) > 2 || new Vector2(delta.x, delta.z).magnitude > 2.5f) return;
            player.ApplyBossSlow(.15f);
            if (owner.Evolved && tick <= 0) { tick = 1; player.ReceiveDamage(new DamageInfo { amount = 8, source = owner.gameObject }); }
        }
    }
}
