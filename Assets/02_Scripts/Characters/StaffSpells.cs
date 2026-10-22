using System;
using Codice.Mechanics;
using Codice.Presentation;
using UnityEngine;

namespace Codice.Characters
{
    public enum StaffSpell { Arcane, Ice, Lightning, Heal }

    // State owns the selected spell so portals preserve the selection with the inventory.
    public sealed class StaffSpells : MonoBehaviour
    {
        PlayerCharacter player;
        readonly float[] ready = new float[4];
        float globalReady;
        public static float Cost(StaffSpell spell) => spell == StaffSpell.Ice ? 25 : spell == StaffSpell.Lightning ? 30 : spell == StaffSpell.Heal ? 35 : 20;
        public static string Label(StaffSpell spell) => spell == StaffSpell.Ice ? "HIELO" : spell == StaffSpell.Lightning ? "RAYO" : spell == StaffSpell.Heal ? "CURA" : "ARCANO";
        public float Remaining => Mathf.Max(0, Mathf.Max(globalReady, ready[(int)player.State.SelectedSpell]) - Time.time);
        void Awake() { player = GetComponent<PlayerCharacter>(); }
        public bool TryCast(LayerMask mask)
        {
            var state = player.State;
            var spell = state.SelectedSpell;
            var weapon = state.ActiveWeapon;
            if (weapon == null || weapon.kind != WeaponKind.Staff || !state.IsAlive || !player.view || player.ControlsBlocked || player.IsStaggered ||
                Time.timeScale == 0 || Codice.UI.RunNavigation.Loading || (Codice.UI.RunHUD.Instance && Codice.UI.RunHUD.Instance.ResultsVisible) ||
                GetComponent<PlayerCombat>().Blocking || GetComponent<Codice.Mechanics.PlayerMechanics>().UltimateActive || Remaining > 0) return false;
            if (spell == StaffSpell.Heal && state.Health >= state.MaxHealth) return false;
            if (!state.SpendMana(Cost(spell))) return false;
            float cycle = spell == StaffSpell.Arcane ? 1 / (weapon.AttacksPerSecond * state.AttackSpeedMultiplier) : spell == StaffSpell.Ice ? 1.4f : .9f;
            globalReady = Time.time + cycle;
            ready[(int)spell] = Time.time + (spell == StaffSpell.Heal ? 8 : cycle);
            GetComponent<ActorPresentation>()?.Attack(true, Mathf.Min(cycle, .8f));
            Vector3 origin = player.view.transform.position, forward = player.view.transform.forward;
            if (spell == StaffSpell.Heal)
            {
                state.Heal(state.MaxHealth * .3f);
                Effect("Heal", player.transform.position + Vector3.up * .05f, .35f, 2);
                return true;
            }
            PresentationVfx.MagicCast(origin + forward * .6f, forward);
            float damage = GetComponent<PlayerMechanics>()?.RollBasicDamage(weapon) ?? weapon.Damage;
            if (spell == StaffSpell.Arcane)
            {
                var shot = new GameObject("Arcane projectile").AddComponent<PlayerProjectile>();
                shot.transform.position = origin; shot.Initialize(gameObject, forward, damage, mask, damage > weapon.Damage * 1.5f); shot.AddMagicVisual();
                return true;
            }
            // Stop at the first solid surface: spells cannot shoot through dungeon walls.
            var hits = Physics.RaycastAll(origin, forward, 35, mask, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a,b) => a.distance.CompareTo(b.distance));
            Vector3 endpoint = origin + forward * 35;
            Component target = null;
            foreach (var hit in hits)
            {
                if (hit.transform.IsChildOf(transform)) continue;
                endpoint = hit.point;
                target = hit.collider.GetComponentInParent<IDamageReceiver>() as Component;
                break;
            }
            if (spell == StaffSpell.Ice)
            {
                Effect("Ice", endpoint, .7f, 2);
                foreach (var enemy in CombatTargets.InRadius(endpoint - forward * .05f, 1.8f))
                {
                    CombatTargets.Hit(enemy, player, damage * .8f);
                    CombatTargets.Stun(enemy, enemy is Codice.Bosses.BossCharacter ? .3f : 1.5f);
                }
            }
            else
            {
                Beam(origin + forward * .4f - player.view.transform.up * .15f, endpoint);
                Effect("Lightning", endpoint, .6f, 1.5f);
                Effect("Impact", endpoint, .35f, 1.5f);
                if (CombatTargets.IsEnemy(target)) CombatTargets.Hit(target, player, damage * 1.8f, true);
            }
            return true;
        }
        public static void Effect(string name, Vector3 position, float scale, float lifetime)
        {
            if(Codice.Multiplayer.LanSession.Host)Codice.Multiplayer.LanSession.Instance.EmitEffect("Spell:"+name,position,scale,lifetime);
            var prefab = Resources.Load<GameObject>("SpellEffects/" + name);
            if (!prefab) return;
            var go = Instantiate(prefab, position, Quaternion.identity); go.transform.localScale *= scale;
            foreach (var particles in go.GetComponentsInChildren<ParticleSystem>()) { var main = particles.main; main.loop = false; main.stopAction = ParticleSystemStopAction.None; particles.Play(); }
            Destroy(go, lifetime);
        }
        static void Beam(Vector3 from, Vector3 to)
        {
            var go = new GameObject("Lightning bolt");
            var line = go.AddComponent<LineRenderer>(); line.useWorldSpace = true; line.positionCount = 13;
            line.sharedMaterial = Resources.Load<Material>("SpecialGlow");
            line.startColor = Color.cyan; line.endColor = Color.white; line.startWidth = .08f; line.endWidth = .025f;
            Vector3 side = Vector3.Cross((to-from).normalized, Vector3.up);
            for (int i=0;i<13;i++) line.SetPosition(i, Vector3.Lerp(from,to,i/12f) + side * (i==0||i==12?0:UnityEngine.Random.Range(-.2f,.2f)));
            Destroy(go,.16f);
        }
    }
}
