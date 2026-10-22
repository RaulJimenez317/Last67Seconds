using System;
using UnityEngine;
namespace Codice.Multiplayer
{
    [Serializable] public sealed class LanMessage
    {
        public string type,version,name,scene,state,error;
        public int id,playerClass,level,phase,kills,defeated,total,slot=-1,spell=-1,passive=-1,buy=-1;
        public float elapsed,roundTime,yaw,pitch;
        public Vector3 point; public float scale,lifetime;
        public Vector2 move; public bool attack,block,jump,dash,special,reload,interact,closeShop,continueRound;
        public LanEntity[] entities; public LanMember[] members;
        public Codice.Characters.MerchantOffer[] offers;public bool shop,portal;public int customer=-1;
    }
    [Serializable] public sealed class LanMember { public int id,playerClass; public string name; }
    [Serializable] public sealed class LanEntity
    {
        public int id,owner=-1;public string key,animation,state,name;
        public Vector3 end;
        public Vector3 position;public Quaternion rotation;public float health,maxHealth;
        public Codice.Characters.WeaponInstance weapon;
        public int kind,amount,passive; public bool available;
    }
}
