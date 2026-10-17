using System.Linq;
using Codice.Characters;
using UnityEngine;
using UnityEngine.UI;

namespace Codice.UI
{
    public sealed partial class RunHUD
    {
        RectTransform shopPanel,pausePanel,defeatPanel,victoryPanel,modalShade;
        readonly Button[] buyButtons=new Button[4],replaceButtons=new Button[3];
        readonly Text[] offerTexts=new Text[4];
        readonly Image[] offerIcons=new Image[4];
        Text shopGold,shopMessage,pauseClass;
        Merchant merchant;
        int replacement=-1;
        float previousTimeScale=1;
        GameObject selectedModal;
        void BuildMenus()
        {
            modalShade=UIWidgets.Image(transform,"Modal shade",0,0,1920,1080,new Color(.025f,.025f,.045f,.82f)).rectTransform;Stretch(modalShade);modalShade.GetComponent<Image>().raycastTarget=true;
            shopPanel=Panel("Mercader Errante",-530,-290,1060,580);Anchor(shopPanel,.5f,.5f);
            UIWidgets.Label(shopPanel,"Heading",32,24,800,45,32,ProceduralUISprites.Gold).text="EL MERCADER ERRANTE";
            shopGold=UIWidgets.Label(shopPanel,"Balance",780,29,250,35,23,ProceduralUISprites.Gold);
            UIWidgets.Label(shopPanel,"Subheading",33,75,990,30,18).text="Un respiro entre cenizas. Tu elección puede cambiar la siguiente ronda.";
            for(int i=0;i<4;i++)
            {
                int index=i;var card=UIWidgets.Image(shopPanel,"Offer "+i,30+i*250,126,240,241,ProceduralUISprites.Ink,"frame");
                offerTexts[i]=UIWidgets.Label(card.transform,"Description",18,20,204,150,21);
                offerIcons[i]=UIWidgets.Image(card.transform,"Offer icon",95,122,50,50,ProceduralUISprites.Gold,"hex");
                buyButtons[i]=UIWidgets.Button(card.transform,"COMPRAR ["+(i+1)+"]",15,181,210,45,()=>Purchase(index));
            }
            UIWidgets.Label(shopPanel,"Replace label",33,385,990,30,17).text="REEMPLAZAR PASIVA (si ya tienes 3)  •  Shift+3 / Ctrl+3 / Alt+3";
            for(int i=0;i<3;i++){int slot=i;replaceButtons[i]=UIWidgets.Button(shopPanel,"Ranura "+(i+1),30+i*333,422,320,45,()=>{replacement=slot;});}
            shopMessage=UIWidgets.Label(shopPanel,"Transaction result",32,485,620,64,18,ProceduralUISprites.Gold);
            UIWidgets.Button(shopPanel,"CONTINUAR RONDA [Enter]",680,505,350,48,()=>{if(merchant)merchant.ContinueRound();});
            pausePanel=Panel("Pause",-350,-290,700,580);Anchor(pausePanel,.5f,.5f);
            UIWidgets.Label(pausePanel,"Heading",35,30,630,60,40,ProceduralUISprites.Gold).text="ENTRE CENIZAS";
            UIWidgets.Label(pausePanel,"Pause subtitle",35,95,630,35,23).text="PAUSA  /  EL TIEMPO SE DETIENE";
            pauseClass=UIWidgets.Label(pausePanel,"Current class",35,152,630,180,21);
            UIWidgets.Label(pausePanel,"Controls",35,330,630,70,18).text="WASD mover • Espacio saltar • Shift dash • E interactuar\nClic izq. atacar • Clic der. escudo • Q especial • R recargar\n1/2/Tab armas • T/Y/U pasivas (10 s / recarga 20 s)";
            UIWidgets.Button(pausePanel,"VOLVER A LA PARTIDA [Escape]",35,415,630,50,()=>SetPaused(false));
            UIWidgets.Button(pausePanel,"IR AL MENÚ PRINCIPAL",35,490,630,50,RunNavigation.MainMenu);
            defeatPanel=Panel("Defeat",-350,-190,700,380);Anchor(defeatPanel,.5f,.5f);
            UIWidgets.Label(defeatPanel,"Heading",35,38,630,60,32,ProceduralUISprites.Crimson).text="LAS CENIZAS TE RECLAMAN";
            UIWidgets.Label(defeatPanel,"Description",35,125,630,85,22).text="Tu viaje termina aquí.\nRepite el nivel o regresa al menú principal.";
            UIWidgets.Button(defeatPanel,"VOLVER A JUGAR",35,230,630,50,()=>{SetPaused(false);if(director)director.RestartLevel();});
            UIWidgets.Button(defeatPanel,"IR AL MENÚ PRINCIPAL",35,300,630,50,RunNavigation.MainMenu);
            victoryPanel=Panel("Victory",-350,-250,700,500);Anchor(victoryPanel,.5f,.5f);
            UIWidgets.Label(victoryPanel,"Heading",35,38,630,65,40,ProceduralUISprites.Gold).text="VICTORIA EN EL COLISEO";
            UIWidgets.Label(victoryPanel,"Description",35,125,630,80,23).text="El señor del Coliseo ha caído.\nTu viaje puede continuar.";
            UIWidgets.Button(victoryPanel,"CONTINUAR VIAJE",35,230,630,55,()=>{if(director)director.Travel();});
            UIWidgets.Button(victoryPanel,"VOLVER A JUGAR",35,310,630,55,()=>{if(director)director.RestartLevel();});
            UIWidgets.Button(victoryPanel,"IR AL MENÚ PRINCIPAL",35,390,630,55,RunNavigation.MainMenu);
            victoryPanel.gameObject.SetActive(false);
            modalShade.gameObject.SetActive(false);shopPanel.gameObject.SetActive(false);pausePanel.gameObject.SetActive(false);defeatPanel.gameObject.SetActive(false);
        }
        public void SetPaused(bool paused)
        {
            if(Paused==paused)return;
            // A click on Resume must not also fire a weapon in the same frame.
            escapeFrame=Time.frameCount;
            Paused=paused;
            if(paused){previousTimeScale=Time.timeScale;Time.timeScale=0;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
            else{Time.timeScale=previousTimeScale;if(Player&&Player.State.IsAlive&&!Player.ControlsBlocked&&!ResultsVisible&&!RunNavigation.Loading){Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}}
        }
        public bool Purchase(int index)
        {
            bool success=merchant&&merchant.TryPurchase(Player,index,replacement);
            shopMessage.text=success?"Compra realizada.":"No se pudo comprar: revisa oro, salud, stock o reemplazo.";
            return success;
        }
        void UpdateMenus()
        {
            bool dead=!Player.State.IsAlive;
            bool won=Context.IsArena&&director&&director.Phase==Codice.Mechanics.RunPhase.Cleared&&!dead;
            victoryPanel.gameObject.SetActive(won&&!Paused);
            var current=Context.dungeon?Context.dungeon.merchant:null;
            bool shopping=current&&current.Customer==Player;
            if(merchant!=current){merchant=current;replacement=-1;}
            modalShade.gameObject.SetActive(Paused||dead||shopping||won);shopPanel.gameObject.SetActive(shopping&&!dead&&!Paused);pausePanel.gameObject.SetActive(Paused&&!dead);defeatPanel.gameObject.SetActive(dead);
            if(dead||won){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
            var activeModal=dead?defeatPanel.gameObject:Paused?pausePanel.gameObject:won?victoryPanel.gameObject:shopping?shopPanel.gameObject:null;
            if(activeModal!=selectedModal)
            {
                selectedModal=activeModal;
                if(activeModal)activeModal.GetComponentsInChildren<Button>().FirstOrDefault(b=>b.interactable)?.Select();
                else if(UnityEngine.EventSystems.EventSystem.current)UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            }
            if(Paused)
            {var s=Player.State;pauseClass.text=$"CLASE: {new[]{"Caballero","Tanque","Arquero"}[(int)s.Class]}\nVida base {s.MaxHealth:0} • Velocidad actual {s.MoveSpeed:0.0}\n\nCaballero 120 HP / 6.5 m/s\nTanque 180 HP / 4.8 m/s • Arquero 100 HP / 8 m/s";}
            if(!shopping)return;
            shopGold.text=$"◆ {Player.State.Gold} ORO";
            for(int i=0;i<4;i++)
            {
                var offer=merchant.GetOffer(i);if(offer==null){buyButtons[i].interactable=false;continue;}
                offerIcons[i].sprite=offer.kind==OfferKind.Weapon?ProceduralUISprites.Weapon(offer.weapon):ProceduralUISprites.Shape(offer.kind==OfferKind.Passive?"hex":"diamond");
                string title=offer.kind==OfferKind.Weapon?UIWidgets.WeaponName(offer.weapon)+"  T"+offer.tier:offer.kind==OfferKind.Passive?UIWidgets.Passive(offer.passive):"Poción de vida";
                string details=offer.kind==OfferKind.Weapon?$"{new WeaponInstance(offer.weapon,offer.tier).Damage:0} DAÑO\n{UIWidgets.Special(offer.weapon)}":offer.kind==OfferKind.Passive?"Sello pasivo\nHasta 3 equipados":"Recupera +50% HP";
                offerTexts[i].text=title+"\n<size=17>"+details+"</size>\n<color=#F5D76E>"+(offer.sold?"VENDIDO":offer.price+" ORO")+"</color>";
                offerTexts[i].transform.parent.GetComponent<Image>().color=offer.kind==OfferKind.Weapon?ProceduralUISprites.Tier(offer.tier):ProceduralUISprites.Cyan;
                buyButtons[i].interactable=!offer.sold&&Player.State.Gold>=offer.price&&(offer.kind!=OfferKind.Healing||Player.State.Health<Player.State.MaxHealth)&&(offer.kind!=OfferKind.Passive||Player.State.CanEquipPassive(offer.passive,replacement));
            }
            for(int i=0;i<3;i++){replaceButtons[i].GetComponentInChildren<Text>().text=(replacement==i?"◆ ":"")+(i+1)+" / "+(i<Player.State.PassiveCount?UIWidgets.Passive(Player.State.GetPassive(i)):"Vacío");}
        }
    }
}
