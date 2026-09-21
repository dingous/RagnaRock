using System;
using RagnaRock.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace RagnaRock
{
    public sealed class GameHud : IDisposable
    {
        private enum Page{State,Options,Codex,Confirm}
        private readonly GameSession game;
        private readonly UiKit ui;
        private readonly RectTransform canvasRoot,safeRoot;
        private readonly GameObject hudRoot,menuRoot,pauseRoot,breakRoot,upgradeRoot,resultRoot,optionsRoot,codexRoot,confirmRoot;
        private GameObject ownEventSystem;
        private readonly Text healthText,actText,waveText,threatText,timeText,scoreText,xpText,bossText,toastText,furyText,focusText;
        private readonly Image healthBar,xpBar,bossBar,furyBar,toastBack,healthFrame;
        private readonly GameObject bossPanel,toastPanel;
        private readonly Text[] memberTexts=new Text[4];
        private readonly Image[] memberBars=new Image[4];
        private readonly Button superButton,continueButton,startWaveButton;
        private readonly Text intermissionTitle,intermissionBody,intermissionCountdown,intermissionThreat;
        private readonly Text upgradeTitle,upgradeSubtitle;
        private readonly Button[] upgradeButtons=new Button[3];
        private readonly Text[] upgradeNames=new Text[3],upgradeBodies=new Text[3],upgradeRanks=new Text[3];
        private readonly Text resultTitle,resultBody,menuRecord,confirmBody,codexTitle,codexStyle,codexLore,codexReferences,codexCounter;
        private readonly Text qualityCaption;
        private readonly Toggle reducedToggle,damageToggle,muteToggle;
        private Page page;
        private int codexIndex;
        private Action confirmed;
        private Rect safeArea;
        private int lastWidth,lastHeight;
        private float hitUntil;
        public bool BlocksGameplayInput{get{return page!=Page.State;}}
        private static Vector2 V(float x,float y){return new Vector2(x,y);}

        public GameHud(GameSession game,Transform parent)
        {
            this.game=game;ui=new UiKit(game.Art.Font);
            canvasRoot=ui.Rect(parent,"RagnaRock UI",Vector2.zero,Vector2.one);
            var canvas=canvasRoot.gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=50;
            var scaler=canvasRoot.gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            canvasRoot.gameObject.AddComponent<GraphicRaycaster>();
            safeRoot=ui.Rect(canvasRoot,"Safe area",Vector2.zero,Vector2.one);
            if(EventSystem.current==null)
            {
                ownEventSystem=new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
                ownEventSystem.transform.SetParent(parent,false);
            }
            hudRoot=ui.Rect(safeRoot,"HUD",Vector2.zero,Vector2.one).gameObject;
            healthFrame=ui.Image(hudRoot.transform,"Stage vitality",UiKit.Panel,V(.015f,.873f),V(.273f,.982f));
            ui.Label(healthFrame.transform,"Brand","RAGNAROCK",31,UiKit.Gold,V(.05f,.55f),V(.9f,.96f),TextAnchor.MiddleLeft,FontStyle.Bold);
            healthText=ui.Label(healthFrame.transform,"Vitality value","PALCO",17,UiKit.TextColor,V(.05f,.28f),V(.95f,.53f));
            healthBar=ui.Bar(healthFrame.transform,"Vitality",new Color(.94f,.35f,.34f),V(.05f,.12f),V(.95f,.22f));

            var chapter=ui.Image(hudRoot.transform,"Chapter",UiKit.Panel,V(.286f,.873f),V(.704f,.982f));
            actText=ui.Label(chapter.transform,"Act title","",26,UiKit.TextColor,V(.04f,.43f),V(.96f,.95f),TextAnchor.MiddleCenter,FontStyle.Bold);
            waveText=ui.Label(chapter.transform,"Wave count","",19,UiKit.Gold,V(.03f,.05f),V(.97f,.43f),TextAnchor.MiddleCenter);
            var status=ui.Image(hudRoot.transform,"Live status",UiKit.Panel,V(.717f,.873f),V(.985f,.982f));
            scoreText=ui.Label(status.transform,"Score","",21,UiKit.TextColor,V(.05f,.58f),V(.72f,.96f),TextAnchor.MiddleLeft,FontStyle.Bold);
            timeText=ui.Label(status.transform,"Run clock","",18,UiKit.Muted,V(.05f,.27f),V(.5f,.61f));
            threatText=ui.Label(status.transform,"Threats","",15,UiKit.Muted,V(.05f,.02f),V(.97f,.3f));
            ui.Button(status.transform,"Pause","II",V(.78f,.36f),V(.96f,.9f),()=>game.TogglePause(),UiKit.Ink,23);

            var boss=ui.Image(hudRoot.transform,"Boss",new Color(.12f,.05f,.08f,.94f),V(.31f,.795f),V(.69f,.856f));bossPanel=boss.gameObject;
            bossText=ui.Label(boss.transform,"Boss name","",18,new Color(1,.70f,.64f),V(.035f,.37f),V(.965f,.97f),TextAnchor.MiddleCenter,FontStyle.Bold);
            bossBar=ui.Bar(boss.transform,"Boss health",new Color(.90f,.22f,.30f),V(.035f,.14f),V(.965f,.3f));
            toastBack=ui.Image(hudRoot.transform,"Announcement",new Color(.08f,.12f,.19f,.93f),V(.18f,.721f),V(.82f,.78f));toastPanel=toastBack.gameObject;
            toastText=ui.Label(toastBack.transform,"Announcement text","",20,UiKit.TextColor,V(.025f,.04f),V(.975f,.96f),TextAnchor.MiddleCenter,FontStyle.Bold);
            focusText=ui.Label(hudRoot.transform,"Focus feedback","",16,UiKit.Gold,V(.34f,.678f),V(.66f,.716f),TextAnchor.MiddleCenter);

            xpBar=ui.Bar(hudRoot.transform,"Experience",new Color(.25f,.86f,.75f),V(.015f,.166f),V(.985f,.176f));
            xpText=ui.Label(hudRoot.transform,"Experience label","",15,UiKit.Muted,V(.016f,.179f),V(.41f,.207f));
            ui.Label(hudRoot.transform,"Controls","CLIQUE: priorizar inimigo   |   ESPAÇO: muralha   |   ESC: pausa   |   F1: arquivo do metal",15,UiKit.Muted,V(.41f,.179f),V(.984f,.207f),TextAnchor.MiddleRight);
            string[] roles={"VOCAL • CONE SÔNICO","GUITARRA • RIFF PERFURANTE","BAIXO • ONDA DE PRESSÃO","BATERIA • BOMBAS RÍTMICAS"};
            for(int i=0;i<4;i++)
            {
                var card=ui.Image(hudRoot.transform,"Musician "+i,UiKit.Panel,V(.015f+i*.193f,.023f),V(.198f+i*.193f,.151f));
                ui.Image(card.transform,"Signature color",StageArt.MemberColors[i],V(.025f,.15f),V(.037f,.85f),false);
                ui.Label(card.transform,"Name",StageArt.MemberNames[i],24,StageArt.MemberColors[i],V(.075f,.54f),V(.95f,.94f),TextAnchor.MiddleLeft,FontStyle.Bold);
                ui.Label(card.transform,"Role",roles[i],13,UiKit.Muted,V(.075f,.35f),V(.96f,.58f));
                memberTexts[i]=ui.Label(card.transform,"Weapon tier","",16,UiKit.TextColor,V(.075f,.14f),V(.95f,.36f));
                memberBars[i]=ui.Bar(card.transform,"Instrument rank",StageArt.MemberColors[i],V(.075f,.075f),V(.925f,.105f));
            }
            superButton=ui.Button(hudRoot.transform,"Wall of sound","",V(.8f,.023f),V(.985f,.151f),()=>game.ActivateSuper(),new Color(.24f,.20f,.35f));
            furyText=ui.Label(superButton.transform,"Fury text","",21,UiKit.TextColor,V(.03f,.23f),V(.97f,.9f),TextAnchor.MiddleCenter,FontStyle.Bold);
            furyBar=ui.Bar(superButton.transform,"Fury",UiKit.Gold,V(.07f,.105f),V(.93f,.16f));

            menuRoot=Overlay("Main menu",.25f);
            var menu=ui.Image(menuRoot.transform,"Menu column",new Color(.035f,.055f,.085f,.96f),V(.05f,.085f),V(.427f,.92f));
            ui.Label(menu.transform,"Eyebrow","SOBREVIVA AO SILÊNCIO",20,UiKit.Gold,V(.065f,.845f),V(.95f,.91f),TextAnchor.MiddleLeft,FontStyle.Bold);
            ui.Label(menu.transform,"Logo","RAGNA\nROCK",88,UiKit.TextColor,V(.055f,.60f),V(.96f,.847f),TextAnchor.MiddleLeft,FontStyle.Bold);
            ui.Label(menu.transform,"Pitch","Quatro músicos. Um palco.\nUma turnê pelo peso do metal.",25,UiKit.Muted,V(.065f,.465f),V(.94f,.60f));
            ui.Button(menu.transform,"New run","COMEÇAR UM NOVO SHOW",V(.065f,.34f),V(.935f,.433f),RequestNewRun,new Color(.48f,.27f,.16f),23);
            continueButton=ui.Button(menu.transform,"Continue","CONTINUAR CHECKPOINT",V(.065f,.232f),V(.935f,.325f),()=>game.ContinueRun(),UiKit.Panel,22);
            ui.Button(menu.transform,"Archive","ARQUIVO DO METAL",V(.065f,.141f),V(.62f,.214f),ShowCodex,UiKit.Ink,18);
            ui.Button(menu.transform,"Options","OPÇÕES",V(.65f,.141f),V(.935f,.214f),ShowOptions,UiKit.Ink,18);
            menuRecord=ui.Label(menu.transform,"Best","",16,UiKit.Muted,V(.065f,.044f),V(.95f,.12f));
            var detail=ui.Image(menuRoot.transform,"Premise",new Color(.035f,.055f,.085f,.93f),V(.66f,.13f),V(.95f,.70f));
            ui.Label(detail.transform,"Tour badge","18 ATOS  /  54 ONDAS",30,UiKit.Gold,V(.065f,.82f),V(.94f,.96f),TextAnchor.MiddleLeft,FontStyle.Bold);
            ui.Label(detail.transform,"Instructions","DEFENDA O PALCO\nOs músicos ficam parados e atacam automaticamente. Os inimigos chegam por todos os lados.\n\nEVOLUA OS INSTRUMENTOS\nO drone coleta os itens. Escolha melhorias ao subir de nível e encontre núcleos nos chefes.\n\nQUEBRE O SILÊNCIO\nClique em uma ameaça para priorizá-la. Use a Muralha de Som para causar dano e proteger a banda.",21,UiKit.TextColor,V(.065f,.19f),V(.935f,.82f));
            ui.Label(detail.transform,"Original assets note","PERSONAGENS, GEOMETRIA E ÁUDIO ORIGINAIS\nReferências culturais no arquivo do metal.",14,UiKit.Muted,V(.065f,.045f),V(.935f,.17f));
            ui.Label(menuRoot.transform,"Build identity","RAGNAROCK • VERSÃO DE DESENVOLVIMENTO 0.1.0",14,UiKit.Muted,V(.06f,.025f),V(.7f,.065f));
            if(!Application.isEditor)ui.Button(menuRoot.transform,"Quit","SAIR",V(.86f,.04f),V(.95f,.095f),()=>game.Quit(),UiKit.Ink,18);

            breakRoot=Overlay("Intermission",.32f);
            var breakCard=Modal(breakRoot,V(.265f,.26f),V(.735f,.735f));
            ui.Label(breakCard,"Badge","PASSAGEM DE SOM",18,UiKit.Gold,V(.06f,.82f),V(.94f,.95f),TextAnchor.MiddleCenter,FontStyle.Bold);
            intermissionTitle=ui.Label(breakCard,"Next act","",32,UiKit.TextColor,V(.045f,.64f),V(.955f,.85f),TextAnchor.MiddleCenter,FontStyle.Bold);
            intermissionThreat=ui.Label(breakCard,"Incoming threat","",20,UiKit.Gold,V(.05f,.53f),V(.95f,.66f),TextAnchor.MiddleCenter);
            intermissionBody=ui.Label(breakCard,"Act context","",20,UiKit.Muted,V(.075f,.28f),V(.925f,.53f),TextAnchor.MiddleCenter);
            startWaveButton=ui.Button(breakCard,"Start wave","COMEÇAR ONDA  [ENTER]",V(.19f,.10f),V(.81f,.255f),()=>game.BeginWave(),new Color(.38f,.25f,.15f),24);
            intermissionCountdown=ui.Label(breakCard,"Countdown","",16,UiKit.Muted,V(.05f,.01f),V(.95f,.095f),TextAnchor.MiddleCenter);

            upgradeRoot=Overlay("Upgrade draft",.76f);
            var upgradeCard=Modal(upgradeRoot,V(.10f,.23f),V(.90f,.77f));
            upgradeTitle=ui.Label(upgradeCard,"Upgrade title","AUMENTE O VOLUME",36,UiKit.TextColor,V(.04f,.78f),V(.96f,.94f),TextAnchor.MiddleCenter,FontStyle.Bold);
            upgradeSubtitle=ui.Label(upgradeCard,"Upgrade instructions","",19,UiKit.Muted,V(.04f,.68f),V(.96f,.8f),TextAnchor.MiddleCenter);
            for(int i=0;i<3;i++)
            {
                int choice=i;
                upgradeButtons[i]=ui.Button(upgradeCard,"Choice "+(i+1),"",V(.027f+i*.325f,.095f),V(.323f+i*.325f,.65f),()=>game.ChooseUpgrade(choice),UiKit.Panel);
                var card=upgradeButtons[i].transform;
                upgradeRanks[i]=ui.Label(card,"Rank","",17,UiKit.Gold,V(.075f,.76f),V(.925f,.94f));
                upgradeNames[i]=ui.Label(card,"Name","",25,UiKit.TextColor,V(.075f,.54f),V(.925f,.79f),TextAnchor.MiddleLeft,FontStyle.Bold);
                upgradeBodies[i]=ui.Label(card,"Effect","",19,UiKit.Muted,V(.075f,.18f),V(.925f,.55f));
                ui.Label(card,"Select shortcut","ESCOLHER  ["+(i+1)+"]",18,UiKit.Gold,V(.075f,.045f),V(.925f,.20f),TextAnchor.MiddleLeft,FontStyle.Bold);
            }

            pauseRoot=Overlay("Pause",.72f);
            var pause=Modal(pauseRoot,V(.34f,.23f),V(.66f,.77f));
            ui.Label(pause,"Pause title","O SHOW ESTÁ PAUSADO",30,UiKit.TextColor,V(.06f,.77f),V(.94f,.93f),TextAnchor.MiddleCenter,FontStyle.Bold);
            ui.Button(pause,"Resume","CONTINUAR  [ESC]",V(.09f,.60f),V(.91f,.745f),()=>game.TogglePause(),new Color(.35f,.26f,.17f),24);
            ui.Button(pause,"Settings","ÁUDIO E ACESSIBILIDADE",V(.09f,.435f),V(.91f,.57f),ShowOptions,UiKit.Panel,20);
            ui.Button(pause,"Codex","ARQUIVO DO METAL",V(.09f,.275f),V(.91f,.41f),ShowCodex,UiKit.Panel,21);
            ui.Button(pause,"Back to menu","VOLTAR AO MENU",V(.09f,.115f),V(.91f,.25f),()=>Confirm("O checkpoint guarda o início da onda. Ao continuar, esta onda será reiniciada. Voltar ao menu?",()=>game.ReturnToMenu()),UiKit.Ink,21);

            resultRoot=Overlay("Result",.72f);
            var result=Modal(resultRoot,V(.24f,.245f),V(.76f,.755f));
            resultTitle=ui.Label(result,"Result title","",38,UiKit.Gold,V(.065f,.69f),V(.935f,.93f),TextAnchor.MiddleCenter,FontStyle.Bold);
            resultBody=ui.Label(result,"Run summary","",24,UiKit.TextColor,V(.075f,.285f),V(.925f,.69f),TextAnchor.MiddleCenter);
            ui.Button(result,"New show","NOVO SHOW",V(.075f,.09f),V(.49f,.25f),()=>game.StartNewRun(),new Color(.40f,.27f,.17f),23);
            ui.Button(result,"Menu","MENU PRINCIPAL",V(.515f,.09f),V(.93f,.25f),()=>game.ReturnToMenu(),UiKit.Panel,22);

            optionsRoot=Overlay("Settings",.78f);
            var options=Modal(optionsRoot,V(.24f,.16f),V(.76f,.84f));
            ui.Label(options,"Options title","AJUSTE O SEU SHOW",34,UiKit.TextColor,V(.07f,.85f),V(.93f,.97f),TextAnchor.MiddleCenter,FontStyle.Bold);
            Volume(options,"VOLUME GERAL",.70f,game.Options.master,v=>game.Options.master=v);
            Volume(options,"TRILHA ORIGINAL",.565f,game.Options.music,v=>game.Options.music=v);
            Volume(options,"EFEITOS SONOROS",.43f,game.Options.effects,v=>game.Options.effects=v);
            reducedToggle=Toggle(options,"REDUZIR MOVIMENTO / TREPIDAÇÃO",V(.075f,.325f),V(.92f,.397f),game.Options.reducedMotion,v=>game.Options.reducedMotion=v);
            damageToggle=Toggle(options,"MOSTRAR NÚMEROS DE DANO",V(.075f,.242f),V(.92f,.315f),game.Options.showDamage,v=>game.Options.showDamage=v);
            muteToggle=Toggle(options,"SILENCIAR TODO O ÁUDIO",V(.075f,.159f),V(.92f,.23f),game.Options.mute,v=>game.Options.mute=v);
            var quality=ui.Button(options,"Visual quality","",V(.075f,.04f),V(.59f,.135f),CycleQuality,UiKit.Panel,19);
            qualityCaption=ui.Label(quality.transform,"Quality caption","",19,UiKit.TextColor,V(.02f,.03f),V(.98f,.97f),TextAnchor.MiddleCenter,FontStyle.Bold);
            ui.Button(options,"Close options","VOLTAR  [ESC]",V(.63f,.04f),V(.925f,.135f),ClosePage,new Color(.36f,.26f,.17f),19);

            codexRoot=Overlay("Metal archive",.78f);
            var codex=Modal(codexRoot,V(.18f,.14f),V(.82f,.86f));
            codexCounter=ui.Label(codex,"Act counter","",18,UiKit.Gold,V(.06f,.86f),V(.94f,.96f),TextAnchor.MiddleCenter,FontStyle.Bold);
            codexTitle=ui.Label(codex,"Chapter title","",37,UiKit.TextColor,V(.06f,.72f),V(.94f,.88f),TextAnchor.MiddleCenter,FontStyle.Bold);
            codexStyle=ui.Label(codex,"Style and period","",22,UiKit.Gold,V(.07f,.60f),V(.93f,.73f),TextAnchor.MiddleCenter);
            codexLore=ui.Label(codex,"Lore","",25,UiKit.TextColor,V(.08f,.415f),V(.92f,.595f),TextAnchor.MiddleLeft);
            codexReferences=ui.Label(codex,"References","",18,UiKit.Muted,V(.08f,.205f),V(.92f,.415f),TextAnchor.MiddleLeft);
            ui.Label(codex,"Curation note","Rota artística, não uma cronologia exaustiva. Estilos coexistem e as fronteiras são discutidas.\nNomes citados como referências culturais; não há participação ou endosso das bandas.",15,UiKit.Muted,V(.075f,.115f),V(.925f,.21f),TextAnchor.MiddleCenter);
            ui.Button(codex,"Previous","ANTERIOR",V(.075f,.03f),V(.30f,.11f),()=>MoveCodex(-1),UiKit.Panel,18);
            ui.Button(codex,"Close archive","VOLTAR  [ESC]",V(.36f,.03f),V(.64f,.11f),ClosePage,new Color(.36f,.26f,.17f),18);
            ui.Button(codex,"Next","PRÓXIMO",V(.70f,.03f),V(.925f,.11f),()=>MoveCodex(1),UiKit.Panel,18);

            confirmRoot=Overlay("Confirm action",.85f);
            var confirm=Modal(confirmRoot,V(.29f,.32f),V(.71f,.68f));
            ui.Label(confirm,"Confirm title","ANTES DE CONTINUAR",29,UiKit.Gold,V(.065f,.70f),V(.935f,.92f),TextAnchor.MiddleCenter,FontStyle.Bold);
            confirmBody=ui.Label(confirm,"Question","",24,UiKit.TextColor,V(.08f,.31f),V(.92f,.71f),TextAnchor.MiddleCenter);
            ui.Button(confirm,"Cancel","CANCELAR",V(.075f,.08f),V(.485f,.26f),ClosePage,UiKit.Panel,21);
            ui.Button(confirm,"Confirm","CONFIRMAR",V(.515f,.08f),V(.925f,.26f),()=>{var action=confirmed;confirmed=null;page=Page.State;action?.Invoke();RefreshState();},new Color(.44f,.26f,.18f),21);
            page=Page.State;UpdateSafeArea();
        }

        private GameObject Overlay(string name,float opacity)
        {return ui.Image(safeRoot,name,new Color(.015f,.025f,.04f,opacity),Vector2.zero,Vector2.one,false).gameObject;}
        private Transform Modal(GameObject parent,Vector2 min,Vector2 max)
        {
            var border=ui.Image(parent.transform,"Metal border",new Color(.29f,.34f,.42f,.96f),min,max);
            var panel=ui.Image(border.transform,"Content",new Color(.045f,.07f,.105f,.985f),V(.003f,.004f),V(.997f,.996f));
            return panel.transform;
        }
        private void Volume(Transform parent,string name,float y,float initial,UnityEngine.Events.UnityAction<float> changed)
        {
            var label=ui.Label(parent,name,name+"  "+Mathf.RoundToInt(initial*100)+"%",19,UiKit.Muted,V(.075f,y+.065f),V(.92f,y+.12f));
            var control=ui.Rect(parent,name+" slider",V(.09f,y+.014f),V(.91f,y+.071f));
            var track=ui.Image(control,"Track",new Color(.025f,.04f,.065f),V(0,.36f),V(1,.64f));
            var fill=ui.Image(track.transform,"Fill",UiKit.Gold,Vector2.zero,Vector2.one);
            var handle=ui.Image(control,"Handle",UiKit.TextColor,V(.5f,.1f),V(.5f,.9f));
            handle.rectTransform.sizeDelta=new Vector2(20,0);
            var slider=control.gameObject.AddComponent<Slider>();slider.fillRect=fill.rectTransform;slider.handleRect=handle.rectTransform;
            slider.targetGraphic=handle;slider.minValue=0;slider.maxValue=1;slider.value=initial;
            slider.onValueChanged.AddListener(v=>{changed(v);label.text=name+"  "+Mathf.RoundToInt(v*100)+"%";});
        }
        private Toggle Toggle(Transform parent,string caption,Vector2 min,Vector2 max,bool initial,UnityEngine.Events.UnityAction<bool> changed)
        {
            var root=ui.Rect(parent,caption,min,max);
            var background=ui.Image(root,"Checkbox",UiKit.Panel,V(0,.09f),V(.062f,.91f));
            var check=ui.Image(background.transform,"Checked",UiKit.Gold,V(.19f,.19f),V(.81f,.81f));
            ui.Label(root,"Caption",caption,19,UiKit.TextColor,V(.09f,0),V(1,1));
            var toggle=root.gameObject.AddComponent<Toggle>();toggle.targetGraphic=background;toggle.graphic=check;
            toggle.isOn=initial;toggle.onValueChanged.AddListener(changed);return toggle;
        }
        public void RefreshState()
        {
            menuRoot.SetActive(false);pauseRoot.SetActive(false);breakRoot.SetActive(false);upgradeRoot.SetActive(false);
            resultRoot.SetActive(false);optionsRoot.SetActive(false);codexRoot.SetActive(false);confirmRoot.SetActive(false);
            hudRoot.SetActive(game.Mode!=RunMode.Menu);
            if(page==Page.Options){optionsRoot.SetActive(true);QualityText();return;}
            if(page==Page.Codex){codexRoot.SetActive(true);RenderCodex();return;}
            if(page==Page.Confirm){confirmRoot.SetActive(true);return;}
            switch(game.Mode)
            {
                case RunMode.Menu:
                    menuRoot.SetActive(true);continueButton.interactable=game.HasCheckpoint;
                    menuRecord.text="RECORDE "+game.Saves.BestScore.ToString("N0")+"   •   MELHOR ONDA "+game.Saves.BestWave;
                    break;
                case RunMode.Intermission:
                    breakRoot.SetActive(true);intermissionTitle.text=game.Chapter.title;
                    intermissionThreat.text="ONDA "+game.Wave.Number+" / "+game.Campaign.TotalWaves+(game.Wave.HasBoss?"  •  CHEFE DO ATO":"  •  "+game.Chapter.style);
                    intermissionBody.text=game.WaveIndex==0?"Os quatro músicos defendem o palco sem se mover.\nClique em inimigos para focar. Os itens são coletados pelo drone.\nAo completar a fúria, use ESPAÇO para atacar e proteger a banda.":game.Chapter.lore;
                    break;
                case RunMode.Upgrade:
                    upgradeRoot.SetActive(true);upgradeTitle.text="AUMENTE O VOLUME";
                    upgradeSubtitle.text="NÍVEL "+game.Stats.level+"  •  ESCOLHA UMA MELHORIA  •  "+game.Stats.pendingChoices+" ESCOLHA(S) DISPONÍVEL(IS)";
                    for(int i=0;i<3;i++)
                    {
                        bool present=i<game.Draft.Length;upgradeButtons[i].gameObject.SetActive(present);if(!present)continue;
                        var kind=game.Draft[i];int next=game.Stats.Rank(kind)+1;
                        upgradeNames[i].text=UpgradeName(kind);upgradeBodies[i].text=UpgradeDescription(kind,next);
                        upgradeRanks[i].text=((int)kind<4?"INSTRUMENTO":"EQUIPAMENTO")+"  •  "+next+" / "+BandStats.MaxRank(kind);
                        upgradeNames[i].color=(int)kind<4?StageArt.MemberColors[(int)kind]:UiKit.Gold;
                    }
                    break;
                case RunMode.Paused:pauseRoot.SetActive(true);break;
                case RunMode.Victory:
                case RunMode.Defeat:
                    resultRoot.SetActive(true);bool won=game.Mode==RunMode.Victory;
                    resultTitle.text=won?"O ÚLTIMO PALCO RESISTIU":"O SILÊNCIO VENCEU ESTA VEZ";
                    resultBody.text=(won?"A turnê chegou ao fim. O metal continua.\n\n":"A banda cai, mas o próximo show já espera.\n\n")+
                        "ONDA "+game.Wave.Number+" / "+game.Campaign.TotalWaves+"    •    NÍVEL "+game.Stats.level+"\n"+
                        game.Kills.ToString("N0")+" INIMIGOS    •    "+game.Score.ToString("N0")+" PONTOS\n"+
                        "TEMPO DE COMBATE  "+FormatTime(game.Elapsed);
                    break;
            }
            RefreshValues();
        }
        public void RefreshValues()
        {
            healthText.text="PALCO  "+Mathf.CeilToInt(game.Health)+" / "+Mathf.CeilToInt(game.Stats.MaxHealth)+
                (game.Mode==RunMode.Playing&&game.Clock<game.InvulnerableUntil?"  •  PROTEGIDO":"");
            UiKit.Fill(healthBar,game.Health/game.Stats.MaxHealth);
            healthFrame.color=Time.unscaledTime<hitUntil?new Color(.26f,.10f,.12f):UiKit.Panel;
            actText.text="ATO "+(game.Wave.ChapterIndex+1)+"  •  "+game.Chapter.title;
            waveText.text="ONDA "+game.Wave.Number+" / "+game.Campaign.TotalWaves+"   ·   "+game.Chapter.style;
            scoreText.text="PONTOS  "+game.Score.ToString("N0");timeText.text=FormatTime(game.Elapsed);
            threatText.text="EM CAMPO "+game.Enemies.Count+"   ·   A CHEGAR "+Mathf.Max(0,game.Wave.Quota-game.Spawned);
            xpText.text="NÍVEL "+game.Stats.level+"   ·   "+game.Stats.experience+" / "+game.Stats.RequiredExperience+" XP";
            UiKit.Fill(xpBar,game.Stats.experience/(float)game.Stats.RequiredExperience);
            for(int i=0;i<4;i++)
            {
                int rank=game.Stats.WeaponRank((BandRole)i);
                memberTexts[i].text="NÍVEL "+rank+"  ·  "+(rank==8?"LENDÁRIO":rank>=4?"RESSONÂNCIA":"ANALÓGICO");
                UiKit.Fill(memberBars[i],rank/8f);
            }
            superButton.interactable=game.Mode==RunMode.Playing&&game.Fury>=100;
            furyText.text=game.Fury>=100?"MURALHA DE SOM\nPRONTA  [ESPAÇO]":"MURALHA DE SOM\nFÚRIA  "+Mathf.FloorToInt(game.Fury)+"%";
            UiKit.Fill(furyBar,game.Fury/100f);
            bool boss=game.Enemies.Boss!=null&&game.Enemies.Boss.Active;bossPanel.SetActive(boss);
            if(boss){bossText.text=game.Chapter.bossName;UiKit.Fill(bossBar,game.Enemies.Boss.Health/game.Enemies.Boss.MaxHealth);}
            bool announce=game.AnnouncementRemaining>0;toastPanel.SetActive(announce);
            if(announce)
            {
                toastText.text=game.Announcement;toastBack.color=game.DangerousAnnouncement?new Color(.31f,.12f,.09f,.97f):new Color(.07f,.12f,.18f,.94f);
            }
            focusText.text=game.FocusTarget!=null&&game.FocusTarget.Active?"ALVO PRIORITÁRIO • "+(game.FocusTarget.IsBoss?game.Chapter.bossName:EnemyName(game.FocusTarget.Kind)):"";
            intermissionCountdown.text="INÍCIO AUTOMÁTICO EM "+Mathf.Max(0,Mathf.CeilToInt(game.IntermissionRemaining))+"s   ·   ESC PARA PAUSAR";
        }
        public void StageHit(){hitUntil=Time.unscaledTime+.35f;}
        public void RequestNewRun()
        {
            game.Audio.Click();
            if(game.HasCheckpoint)Confirm("Um checkpoint está salvo. Começar um novo show apagará esse progresso de partida. Seus recordes serão mantidos.",()=>game.StartNewRun());
            else game.StartNewRun();
        }
        public void ShowOptions()
        {
            game.PauseForOverlay();page=Page.Options;
            reducedToggle.SetIsOnWithoutNotify(game.Options.reducedMotion);damageToggle.SetIsOnWithoutNotify(game.Options.showDamage);muteToggle.SetIsOnWithoutNotify(game.Options.mute);
            RefreshState();game.Audio.Click();
        }
        public void ShowCodex()
        {
            game.PauseForOverlay();page=Page.Codex;codexIndex=Mathf.Clamp(game.Wave.ChapterIndex,0,game.Campaign.chapters.Length-1);
            RefreshState();game.Audio.Click();
        }
        private void RenderCodex()
        {
            var chapter=game.Campaign.chapters[codexIndex];
            codexCounter.text="ARQUIVO DO METAL  •  ATO "+(codexIndex+1)+" / "+game.Campaign.chapters.Length;
            codexTitle.text=chapter.title;codexTitle.color=StageArt.Hex(chapter.colorHex);
            codexStyle.text=chapter.style+"\n"+chapter.period;codexLore.text=chapter.lore;
            codexReferences.text="REFERÊNCIAS CULTURAIS\n"+chapter.tribute+(string.IsNullOrEmpty(chapter.songReference)?"":"\n\nALUSÃO CENOGRÁFICA\n"+chapter.songReference)+"\n\nA trilha desta arena é uma composição sintetizada original, não uma faixa dessas bandas.";
        }
        private void MoveCodex(int direction)
        {
            int count=game.Campaign.chapters.Length;codexIndex=(codexIndex+direction+count)%count;RenderCodex();game.Audio.Click();
        }
        private void CycleQuality()
        {game.Options.quality=(game.Options.quality+1)%3;game.Art.SetQuality(game.Options.quality);QualityText();game.Audio.Click();}
        private void QualityText(){qualityCaption.text="VISUAL: "+(game.Options.quality==0?"LEVE":game.Options.quality==1?"EQUILIBRADO":"ALTO");}
        private void Confirm(string body,Action action)
        {confirmed=action;confirmBody.text=body;page=Page.Confirm;RefreshState();}
        private void ClosePage()
        {if(page==Page.Options)game.ApplyOptions();page=Page.State;confirmed=null;RefreshState();game.Audio.Click();}
        public bool HandleBack()
        {if(page==Page.State)return false;ClosePage();return true;}
        public void UpdateSafeArea()
        {
            Rect current=Screen.safeArea;if(current==safeArea&&lastWidth==Screen.width&&lastHeight==Screen.height)return;
            safeArea=current;lastWidth=Screen.width;lastHeight=Screen.height;
            if(Screen.width<=0||Screen.height<=0)return;
            safeRoot.anchorMin=new Vector2(current.xMin/Screen.width,current.yMin/Screen.height);
            safeRoot.anchorMax=new Vector2(current.xMax/Screen.width,current.yMax/Screen.height);
        }
        public static string FormatTime(float seconds)
        {int value=Mathf.Max(0,Mathf.FloorToInt(seconds));return (value/60).ToString("00")+":"+(value%60).ToString("00");}
        private static string EnemyName(EnemyKind kind)
        {
            switch(kind){case EnemyKind.Rusher:return "CORREDOR";case EnemyKind.Brute:return "BRUTAMONTES";case EnemyKind.Spitter:return "CUSPIDOR";
                case EnemyKind.Shielded:return "BLINDADO";case EnemyKind.Splitter:return "FRAGMENTADOR";case EnemyKind.Summoner:return "INVOCADOR";default:return "ERRANTE";}
        }
        private static string UpgradeName(UpgradeKind kind)
        {
            switch(kind)
            {
                case UpgradeKind.Voice:return "VOZ DO ABISMO";case UpgradeKind.Guitar:return "RIFF DE RELÂMPAGO";
                case UpgradeKind.Bass:return "GRAVIDADE DO BAIXO";case UpgradeKind.Drums:return "TEMPESTADE DE TONS";
                case UpgradeKind.Amplifier:return "PAREDE DE AMPLIFICADORES";case UpgradeKind.Tempo:return "CLIQUE IMPLACÁVEL";
                case UpgradeKind.Range:return "TORRE DE RESSONÂNCIA";case UpgradeKind.Armor:return "GRADE DE AÇO";
                case UpgradeKind.Vitality:return "PALCO REFORÇADO";case UpgradeKind.Magnet:return "ROADIE TURBINADO";
                case UpgradeKind.Critical:return "PALHETA DE PRECISÃO";default:return "EQUIPE DE ESTRADA";
            }
        }
        private static string UpgradeDescription(UpgradeKind kind,int next)
        {
            switch(kind)
            {
                case UpgradeKind.Voice:return next==8?"Evolução final: explosão vocal em 360°. Mais dano e frequência.":next==4?"Ressonância: repulsão mais forte. O cone cresce, com mais dano e frequência.":"Mais dano, alcance do cone e frequência para Nyx.";
                case UpgradeKind.Guitar:return next==8?"Evolução final: ignora blindagem e explode em eletricidade ao atingir o alvo.":next==4?"Ressonância: o riff atravessa três inimigos. Mais dano e frequência.":"Mais dano e frequência. A cada dois níveis, perfura mais um inimigo.";
                case UpgradeKind.Bass:return next==8?"Evolução final: repulsão pesada e lentidão em área. O palco ganha espaço.":next==4?"Ressonância: inimigos atingidos ficam mais lentos. Mais pressão e frequência.":"Mais dano, alcance e frequência para a onda de pressão de Atlas.";
                case UpgradeKind.Drums:return next==8?"Evolução final: três bombas, lentidão e repulsão nas explosões.":next==4?"Ressonância: duas bombas por batida. Mais dano e área de explosão.":"Mais dano, frequência e área para as bombas rítmicas de Knox.";
                case UpgradeKind.Amplifier:return "+14% de dano-base para os quatro instrumentos por nível.";
                case UpgradeKind.Tempo:return "+8,5% de frequência-base de ataque para toda a banda por nível.";
                case UpgradeKind.Range:return "+0,9 metro de alcance de ataque para os quatro músicos.";
                case UpgradeKind.Armor:return "+6,5 pontos percentuais de redução do dano recebido pelo palco.";
                case UpgradeKind.Vitality:return "+32 de vida máxima e +32 de reparo imediato no palco.";
                case UpgradeKind.Magnet:return "+2,2 m/s de velocidade-base para a atração de itens.";
                case UpgradeKind.Critical:return "+4,5 pontos percentuais de chance crítica. Críticos causam 1,8x o dano.";
                default:return "+3 de reparo no palco ao terminar cada onda.";
            }
        }
        public void Dispose(){ui.Dispose();if(canvasRoot!=null)Object.Destroy(canvasRoot.gameObject);if(ownEventSystem!=null)Object.Destroy(ownEventSystem);}
    }
}
