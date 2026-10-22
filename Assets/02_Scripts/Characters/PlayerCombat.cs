using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Codice.Characters
{
    [RequireComponent(typeof(PlayerCharacter))]
    public sealed class PlayerCombat : MonoBehaviour
    {
        public LayerMask hitMask = ~0;
        public GameObject[] weaponModels;
        PlayerCharacter player;
        float nextAttack, lastAttack;
        WeaponInstance swinging;
        float swingElapsed, swingDuration;
        readonly HashSet<IDamageReceiver> swingHits = new HashSet<IDamageReceiver>();
        public bool IsSwinging => swinging != null;
        public static float MeleeRange(WeaponKind kind) => kind == WeaponKind.Dagger ? 1.35f : kind == WeaponKind.Shield ? 1.5f : kind == WeaponKind.Polearm ? 3.2f : kind == WeaponKind.Scythe ? 2.8f : kind == WeaponKind.Axe || kind == WeaponKind.Hammer ? 2.5f : 2.2f;
        public static float ContactStart(WeaponKind kind) => kind == WeaponKind.Axe || kind == WeaponKind.Hammer || kind == WeaponKind.Scythe ? .32f : .2f;
        public static float ContactEnd(WeaponKind kind) => kind == WeaponKind.Axe || kind == WeaponKind.Hammer || kind == WeaponKind.Scythe ? .62f : .52f;
        void CancelSwing(){swinging=null;swingHits.Clear();}
        void UpdateSwing(float deltaTime)
        {
            if(swinging==null)return;
            if(!player.State.IsAlive||player.State.ActiveWeapon!=swinging||player.IsStaggered||Codice.UI.RunNavigation.Loading){CancelSwing();return;}
            if(Blocking){CancelSwing();return;}
            if(Time.timeScale==0||player.ControlsBlocked)return;
            float before=swingElapsed/swingDuration;
            swingElapsed+=deltaTime;
            float after=swingElapsed/swingDuration;
            // An interval crossing still hits on low frame rates; each receiver is struck once.
            if(after>=ContactStart(swinging.kind)&&before<=ContactEnd(swinging.kind))ResolveMelee(swinging);
            if(after>=1)CancelSwing();
        }
        int combo;
        WeaponKind previousKind; WeaponInstance previousWeaponInstance;
        int previousSlot = -1, previousTier = -1;
        Vector3 restPosition;
        Transform displayed;
        public int Combo => combo;
        public bool Blocking => Codice.Multiplayer.LanAvatar.IsRemote(this)?GetComponent<Codice.Multiplayer.LanAvatar>().block : player && (player.State.Class == PlayerClass.Warrior || player.State.ActiveWeapon?.kind == WeaponKind.Shield) &&
            player.State.IsAlive && !player.ControlsBlocked && !player.IsStaggered && !Codice.UI.RunNavigation.Loading && Time.timeScale > 0 &&
            !(Codice.UI.RunHUD.Instance && Codice.UI.RunHUD.Instance.ResultsVisible) && Cursor.lockState == CursorLockMode.Locked && Mouse.current != null && Mouse.current.rightButton.isPressed;
        WeaponInstance reloading;
        float reloadElapsed;
        public const float ReloadSeconds = 2;
        public bool IsReloading => reloading != null;
        public float ReloadFraction => IsReloading ? reloadElapsed / ReloadSeconds : 0;
        public bool TryReload()
        {
            var w = player.State.ActiveWeapon;
            if (!player.State.IsAlive || player.ControlsBlocked || player.IsStaggered || Codice.UI.RunNavigation.Loading || Time.timeScale == 0 || IsReloading ||
                (Codice.UI.RunHUD.Instance && Codice.UI.RunHUD.Instance.ResultsVisible) ||
                w == null || w.kind != WeaponKind.Musket || w.ammunition >= w.Capacity || w.reserveAmmunition <= 0) return false;
            reloading = w; reloadElapsed = 0; Codice.Presentation.GameAudio.Play(Codice.Presentation.SoundCue.Reload); return true;
        }
        void OnDisable() { StopAllCoroutines(); CancelSwing(); reloading = null; reloadElapsed = 0; }
        public WeaponPickup CreatePickup(WeaponInstance weapon, Vector3 position)
        {
            var pickup = new GameObject("Dropped " + weapon.kind + " T" + weapon.tier).AddComponent<WeaponPickup>();
            pickup.weapon = weapon; pickup.transform.position = position;
            var collider = pickup.gameObject.AddComponent<SphereCollider>(); collider.radius = .5f; collider.isTrigger = true;
            AddPickupVisual(pickup);
            return pickup;
        }
        public void AddPickupVisual(WeaponPickup pickup)
        {
            if (pickup.visual) Destroy(pickup.visual);
            var catalog = Codice.Presentation.WeaponVisualCatalog.Load();
            var tierModel = catalog ? catalog.Get(pickup.weapon) : null;
            if(tierModel){pickup.visual=Instantiate(tierModel,pickup.transform);return;}
            var specialModel = pickup.weapon.katana ? Resources.Load<GameObject>("Katana") : pickup.weapon.shuriken ? Resources.Load<GameObject>("Shuriken") : null;
            if (specialModel) { pickup.visual = Instantiate(specialModel, pickup.transform); pickup.visual.transform.localPosition = Vector3.zero; pickup.visual.transform.localRotation = Quaternion.identity; pickup.visual.SetActive(true); return; }
            int index = (int)pickup.weapon.kind;
            if (weaponModels == null || index >= weaponModels.Length || !weaponModels[index]) return;
            pickup.visual = Instantiate(weaponModels[index], pickup.transform);
            pickup.visual.transform.localPosition = Vector3.zero;
            pickup.visual.transform.localScale = weaponModels[index].transform.lossyScale;
            pickup.visual.transform.localRotation = Quaternion.identity;
            pickup.visual.SetActive(true);
        }
        void Awake() { player = GetComponent<PlayerCharacter>(); }
        void Update()
        {
            var weapon = player.State.ActiveWeapon;
            if(Codice.Multiplayer.LanSession.Client){if(weapon!=null)UpdateWeapon(weapon);return;}
            UpdateSwing(Time.deltaTime);
            if (reloading != null)
            {
                if (!player.State.IsAlive || weapon != reloading) { reloading = null; reloadElapsed = 0; }
                else if (!player.ControlsBlocked && !(Codice.UI.RunHUD.Instance && Codice.UI.RunHUD.Instance.ResultsVisible))
                {
                    reloadElapsed += Time.deltaTime;
                    if (reloadElapsed >= ReloadSeconds)
                    {
                        int rounds = Mathf.Min(reloading.Capacity - reloading.ammunition, reloading.reserveAmmunition);
                        reloading.ammunition += rounds; reloading.reserveAmmunition -= rounds;
                        reloading = null; reloadElapsed = 0; player.State.NotifyChanged();
                    }
                }
            }
            if (weapon == null) return;
            UpdateWeapon(weapon);
            if(weaponModels != null && weaponModels.Length > (int)WeaponKind.Shield && weaponModels[(int)WeaponKind.Shield])
                weaponModels[(int)WeaponKind.Shield].SetActive(weapon.kind == WeaponKind.Shield || Blocking);
            if (!Codice.Multiplayer.LanAvatar.IsRemote(this) && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) TryReload();
            if (displayed && !GetComponent<Codice.Presentation.ActorPresentation>())
            {
                float phase = Mathf.Clamp01((Time.time - lastAttack) / .25f);
                displayed.localRotation = Quaternion.Euler(-30 * Mathf.Sin(phase * Mathf.PI), 0, -25 * Mathf.Sin(phase * Mathf.PI));
                displayed.localPosition = restPosition + Vector3.forward * (.15f * Mathf.Sin(phase * Mathf.PI));
            }
            if (!Codice.Multiplayer.LanAvatar.IsRemote(this) && Mouse.current != null && Mouse.current.leftButton.isPressed && Cursor.lockState == CursorLockMode.Locked && Time.timeScale > 0)
                TryAttack();
        }
        void UpdateWeapon(WeaponInstance weapon)
        {
            if (previousSlot == player.State.ActiveSlot && previousKind == weapon.kind && previousWeaponInstance==weapon && previousTier==weapon.tier) return;
            previousTier=weapon.tier;
            if(weaponModels!=null)Codice.Presentation.WeaponVisualCatalog.Apply(weaponModels[(int)weapon.kind],weapon);
            previousWeaponInstance=weapon; previousSlot = player.State.ActiveSlot; previousKind = weapon.kind; combo = 0;
            displayed = null;
            if (weaponModels == null) return;
            for (int i = 0; i < weaponModels.Length; i++)
            {
                if (!weaponModels[i]) continue;
                weaponModels[i].SetActive(i == (int)weapon.kind);
                if (i == (int)weapon.kind) { displayed = weaponModels[i].transform; restPosition = displayed.localPosition; }
            }
        }
        public bool TryAttack()
        {
            if(Codice.Multiplayer.LanSession.Client)return false;
            if(Codice.Multiplayer.LanSession.Active&&!Codice.Multiplayer.LanAvatar.IsRemote(this)&&Codice.UI.RunHUD.Instance&&Codice.UI.RunHUD.Instance.Paused)return false;
            if (Codice.UI.RunNavigation.Loading || Codice.UI.RunHUD.HandlesEscapeThisFrame) return false;
            if (Codice.UI.RunHUD.Instance && Codice.UI.RunHUD.Instance.ResultsVisible) return false;
            var weapon = player.State.ActiveWeapon;
            if (!player.State.IsAlive || player.ControlsBlocked || player.IsStaggered || Time.timeScale == 0 || !player.view || weapon == null || Time.time < nextAttack) return false;
            if (GetComponent<Codice.Mechanics.PlayerMechanics>().UltimateQueued || IsReloading || Blocking || IsSwinging || GetComponent<Codice.Mechanics.PlayerMechanics>().UltimateActive || (GetComponent<ShinobiGuard>()?.Guarding??false)) return false;
            if (weapon.kind == WeaponKind.Staff) return GetComponent<StaffSpells>().TryCast(hitMask);
            if (!player.State.ConsumeAmmo()) { TryReload(); return false; }
            if (Time.time - lastAttack > 1.2f) combo = 0;
            combo = combo % 3 + 1;
            lastAttack = Time.time; nextAttack = Time.time + 1 / (weapon.AttacksPerSecond * player.State.AttackSpeedMultiplier);
            var mechanics = GetComponent<Codice.Mechanics.PlayerMechanics>();
            GetComponent<Codice.Presentation.ActorPresentation>()?.Attack(weapon.Ranged, 1 / (weapon.AttacksPerSecond * player.State.AttackSpeedMultiplier));
            var origin = player.view.transform.position;
            var forward = player.view.transform.forward;
            if (weapon.Ranged)
            {
                if (weapon.kind != WeaponKind.Bow)
                {
                    Vector3 muzzle=displayed?displayed.position:origin+forward*.6f;
                    // Imported staff and musket meshes extend along their local Y axis.
                    // Keep the flash on the animated weapon tip, rather than at the hand.
                    var mesh=displayed?displayed.GetComponentInChildren<MeshFilter>():null;
                    if(mesh&&mesh.sharedMesh)
                    {
                        var bounds=mesh.sharedMesh.bounds;
                        muzzle=mesh.transform.TransformPoint(new Vector3(bounds.center.x,bounds.max.y,bounds.center.z));
                    }
                    if(displayed)foreach(var child in displayed.GetComponentsInChildren<Transform>())if(child.name=="Muzzle"){muzzle=child.position;break;}
                    if(weapon.kind==WeaponKind.Staff)Codice.Presentation.PresentationVfx.MagicCast(muzzle,forward);
                    else Codice.Presentation.PresentationVfx.Muzzle(muzzle,forward);
                    if(weapon.kind==WeaponKind.Musket)Codice.Presentation.PresentationVfx.GunSmoke(muzzle,forward);
                }
                if (weapon.kind == WeaponKind.Musket)
                {
                    var hits = Physics.RaycastAll(origin, forward, 60, hitMask, QueryTriggerInteraction.Ignore);
                    System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                    var struck = new HashSet<IDamageReceiver>();
                    foreach (var hit in hits)
                    {
                        if (hit.transform.IsChildOf(transform)) continue;
                        Codice.Presentation.PresentationVfx.Impact(hit.point,hit.normal);
                        var receiver = hit.collider.GetComponentInParent<IDamageReceiver>();
                        if (receiver == null) break;
                        if (struck.Add(receiver)) Hit(hit.collider, weapon, forward);
                        if (!Codice.Mechanics.CombatTargets.IsEnemy(receiver as Component)) break;
                    }
                }
                else if(weapon.shuriken)StartCoroutine(ThrowShuriken(weapon));
                else
                {
                    var shot = new GameObject("Player projectile").AddComponent<PlayerProjectile>();
                    shot.transform.position = origin;
                    float damage = mechanics ? mechanics.RollBasicDamage(weapon) : weapon.Damage;
                    shot.Initialize(gameObject, forward, damage, hitMask, damage > weapon.Damage * 1.5f);
                    if(weapon.shuriken)shot.AddShurikenVisual();
                    else if (weapon.kind == WeaponKind.Bow) shot.AddArrowVisual();
                    else shot.AddMagicVisual();
                }
            }
            else
            {
                if(weapon.kind==WeaponKind.Sword)Codice.Presentation.PresentationVfx.SwordSwing(origin+forward*.8f-Vector3.up*.25f,forward);
                swinging=weapon;swingElapsed=0;swingDuration=nextAttack-Time.time;swingHits.Clear();
            }
            return true;
        }
        System.Collections.IEnumerator ThrowShuriken(WeaponInstance weapon)
        {
            yield return new WaitForSeconds(.12f);
            if(!player.State.IsAlive||player.State.ActiveWeapon!=weapon||player.ControlsBlocked||player.IsStaggered||Codice.UI.RunNavigation.Loading)yield break;
            var shot=new GameObject("Thrown shuriken").AddComponent<PlayerProjectile>();shot.transform.position=player.view.transform.position;
            shot.Initialize(gameObject,player.view.transform.forward,weapon.Damage,hitMask);shot.AddShurikenVisual();
        }
        void ResolveMelee(WeaponInstance weapon)
        {
            var origin=player.view.transform.position;
            var forward=player.view.transform.forward;
            float range = MeleeRange(weapon.kind);
            bool wide = weapon.kind == WeaponKind.Axe || weapon.kind == WeaponKind.Hammer || weapon.kind == WeaponKind.Scythe;
            var receivers = swingHits;
            foreach (var collider in Physics.OverlapSphere(origin, range, hitMask, QueryTriggerInteraction.Ignore))
            {
                if (collider.transform.IsChildOf(transform)) continue;
                var receiver = collider.GetComponentInParent<IDamageReceiver>();
                if (receiver == null || receivers.Contains(receiver)) continue;
                Vector3 point = collider.ClosestPoint(origin);
                Vector3 offset = point - origin;
                // Aim at the volume's centre for the cone test. ClosestPoint can be
                // directly below the camera when an enemy is touching the player.
                Vector3 aim = collider.bounds.center - origin;
                Vector3 planarAim = Vector3.ProjectOnPlane(aim, Vector3.up);
                Vector3 planarForward = Vector3.ProjectOnPlane(forward, Vector3.up);
                if (Vector3.Angle(planarForward, planarAim) > (wide ? 65 : weapon.kind == WeaponKind.Dagger || weapon.kind == WeaponKind.Polearm ? 30 : 45)) continue;
                if (offset.sqrMagnitude > range * range) continue;
                // Cover blocks melee damage too. A multi-collider receiver is hit only once.
                bool blocked = false;
                foreach (var obstruction in Physics.RaycastAll(origin, offset.normalized, offset.magnitude, hitMask, QueryTriggerInteraction.Ignore))
                {
                    if (obstruction.transform.IsChildOf(transform) || obstruction.collider.GetComponentInParent<IDamageReceiver>() == receiver) continue;
                    // Other actors do not absorb a wide swing; solid scenery does.
                    if (Codice.Mechanics.CombatTargets.IsEnemy(obstruction.collider.GetComponentInParent<IDamageReceiver>() as Component)) continue;
                    blocked = true; break;
                }
                if (blocked) continue;
                if (receiver != null && receivers.Add(receiver)) Hit(collider, weapon, offset.normalized);
            }
        }
        void Hit(Collider collider, WeaponInstance weapon, Vector3 direction)
        {
            if (collider.transform.IsChildOf(transform)) return;
            float damage = GetComponent<Codice.Mechanics.PlayerMechanics>()?.RollBasicDamage(weapon) ?? weapon.Damage;
            collider.GetComponentInParent<IDamageReceiver>()?.ReceiveDamage(new DamageInfo
            {
                amount = damage*(player.State.DualWield?1.25f:1), critical = damage > weapon.Damage * 1.5f, hasImpactPoint = true, impactPoint = collider.ClosestPoint(player.view.transform.position), direction = direction, source = gameObject, melee = !weapon.Ranged,
                breaksGuard = weapon.kind == WeaponKind.Axe || weapon.kind == WeaponKind.Hammer,
                knockback = weapon.kind == WeaponKind.Axe ? 5 : 0
            });
        }
    }
}
