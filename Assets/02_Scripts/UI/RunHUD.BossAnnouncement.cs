using Codice.Mechanics;
using Codice.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace Codice.UI
{
    public sealed partial class RunHUD
    {
        RectTransform bossAnnouncement;
        CanvasGroup bossAnnouncementFade;
        Text bossAnnouncementTitle, bossAnnouncementDetail;
        int announcedBossRound=-1;
        float bossAnnouncementTime;
        void UpdateBossAnnouncement()
        {
            if (!director) return;
            if (RoundRules.IsBoss(director.Level) && director.CombatActive && Context.arena && Context.arena.ActiveBoss && announcedBossRound!=director.Level)
            {
                announcedBossRound=director.Level;
                if (!bossAnnouncement)
                {
                    bossAnnouncement=Panel("Boss announcement",-560,-130,1120,220);Anchor(bossAnnouncement,.5f,.5f);
                    bossAnnouncementFade=bossAnnouncement.gameObject.AddComponent<CanvasGroup>();
                    bossAnnouncementFade.blocksRaycasts=false;bossAnnouncementFade.interactable=false;
                    UIWidgets.Image(bossAnnouncement,"Gold line",110,12,900,3,ProceduralUISprites.Gold);
                    bossAnnouncementTitle=UIWidgets.Label(bossAnnouncement,"JEFE",20,22,1080,100,84,ProceduralUISprites.Gold);
                    bossAnnouncementTitle.alignment=TextAnchor.MiddleCenter;bossAnnouncementTitle.fontStyle=FontStyle.Bold;
                    bossAnnouncementTitle.resizeTextForBestFit=true;bossAnnouncementTitle.resizeTextMinSize=36;bossAnnouncementTitle.resizeTextMaxSize=74;
                    bossAnnouncementDetail=UIWidgets.Label(bossAnnouncement,"Boss details",20,128,1080,64,25,Color.white);
                    bossAnnouncementDetail.alignment=TextAnchor.MiddleCenter;
                    UIWidgets.Image(bossAnnouncement,"Gold line bottom",110,205,900,3,ProceduralUISprites.Gold);
                }
                bossAnnouncementTitle.text=BossName(Context.arena.ActiveBoss.kind);
                bossAnnouncementDetail.text=$"{(RoundRules.IsFinal(director.Level)?"JEFE FINAL":"JEFE")}  •  RONDA {director.Level:00}";
                bossAnnouncementTime=Time.time;bossAnnouncement.gameObject.SetActive(true);
                GameAudio.Play(SoundCue.Boss);
            }
            if (!bossAnnouncement) return;
            float age=Time.time-bossAnnouncementTime;
            bossAnnouncement.gameObject.SetActive(age<3.8f && director.CombatActive && !Paused);
            bossAnnouncementFade.alpha=Mathf.Clamp01(age/.25f)*Mathf.Clamp01((3.8f-age)/.65f);
            bossAnnouncement.localScale=Vector3.one*Mathf.Lerp(1.12f,1,Mathf.Clamp01(age/.5f));
        }
    }
}
