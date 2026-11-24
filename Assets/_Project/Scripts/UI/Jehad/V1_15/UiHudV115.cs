using System;
using Echoheart.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Echoheart.UI
{
    public sealed class UiHudV115
    {
        private readonly RectTransform _root,_healthFill,_healthTrail,_relicFill,_bossFill,_bossTrail,_boss,_prompt,_crosshair,_objectiveCard,_controls;
        private readonly RectTransform _powerSlot,_pauseShortcut;
        private readonly RectTransform[] _clusters;
        private readonly Text _healthText,_healthLabel,_powerText,_powerHint,_relicText,_bossText,_objective,_promptText,_key;
        private readonly UiGlyphV16 _powerIcon;
        private readonly UiRoundedV16 _healthPaint,_healthTrailPaint,_bossTrailPaint;
        private readonly HealthBarV115 _vital=new HealthBarV115(),_guardian=new HealthBarV115();
        private float _relicPulse;
        private bool _bossVisible,_ranged,_reduceMotion;
        public UiHudV115(UiFactoryV17 ui,RectTransform parent,Action pause)
        {
            _root=ui.Rect("HUD",parent,0,0,1600,900,new Vector2(.5f,.5f));
            var vital=ui.Panel("Vitality",_root,24,-24,300,92,UiFactoryV17.Forest);
            ui.Icon(vital,UiGlyphKindV16.Heart,18,-24,34,UiFactoryV17.Coral);
            _healthLabel=ui.Label(vital,"VITALITY",72,-14,130,22,14,UiFactoryV17.Muted,true);
            _healthText=ui.Label(vital,"",208,-12,72,28,22,UiFactoryV17.Cream,true,TextAnchor.UpperRight);
            var track=ui.Panel("Health track",vital,72,-54,206,10,new Color(.15f,.30f,.24f));
            _healthTrail=ui.Rect("Health recent loss",track,0,0,206,10);_healthTrailPaint=ui.Paint(_healthTrail,new Color(.93f,.78f,.45f,.65f),5);
            _healthFill=ui.Rect("Health fill",track,0,0,206,10);_healthPaint=ui.Paint(_healthFill,UiFactoryV17.Mint,5);
            var power=ui.Panel("Held power",_root,24,-130,300,90,UiFactoryV17.Forest);
            _powerSlot=power;
            _powerIcon=ui.Icon(power,UiGlyphKindV16.None,18,-22,42,UiFactoryV17.Muted);
            _powerText=ui.Label(power,"No power",80,-15,200,30,23,UiFactoryV17.Cream,true);
            _powerHint=ui.Label(power,"No ability held",80,-51,200,25,17,UiFactoryV17.Muted);
            var relic=ui.Panel("Relic collection",_root,-24,-24,260,82,UiFactoryV17.Forest,new Vector2(1,1));
            ui.Icon(relic,UiGlyphKindV16.Relic,18,-21,35,UiFactoryV17.Gold);
            ui.Label(relic,"RELICS",72,-10,150,22,14,UiFactoryV17.Muted,true);
            _relicText=ui.Label(relic,"0 / 16",72,-34,160,34,26,UiFactoryV17.Cream,true);
            var rt=ui.Panel("Relic track",relic,18,-71,224,4,new Color(.15f,.30f,.24f));_relicFill=ui.Rect("Relic fill",rt,0,0,0,4);ui.Paint(_relicFill,UiFactoryV17.Gold,2);
            _objectiveCard=ui.Panel("Current objective",_root,-24,-122,330,104,UiFactoryV17.Forest,new Vector2(1,1));
            ui.Label(_objectiveCard,"YOUR NEXT STEP",18,-13,290,22,14,UiFactoryV17.Mint,true);
            _objective=ui.Label(_objectiveCard,"Follow the trails to the Guardian",18,-44,294,50,21,UiFactoryV17.Cream);
            var pauseRoot=ui.Rect("Pause shortcut",_root,-24,-242,146,50,new Vector2(1,1));Button pauseButton=ui.Button(pauseRoot,"Pause",0,0,146,pause,false,50);
            pauseButton.navigation=new Navigation{mode=Navigation.Mode.None};pauseButton.GetComponent<UiFocusV16>().MouseSelects=false;
            _pauseShortcut=pauseRoot;
            _boss=ui.Panel("Guardian health",_root,0,-24,580,82,UiFactoryV17.Forest,new Vector2(.5f,1));
            ui.Label(_boss,"CORRUPTED GUARDIAN",22,-13,430,27,19,UiFactoryV17.Cream,true);
            _bossText=ui.Label(_boss,"",446,-13,112,27,18,UiFactoryV17.Muted,false,TextAnchor.UpperRight);
            var bt=ui.Panel("Guardian track",_boss,22,-53,536,10,new Color(.20f,.18f,.24f));
            _bossTrail=ui.Rect("Guardian recent loss",bt,0,0,536,10);_bossTrailPaint=ui.Paint(_bossTrail,new Color(.93f,.78f,.45f,.65f),5);
            _bossFill=ui.Rect("Guardian fill",bt,0,0,536,10);ui.Paint(_bossFill,new Color(.75f,.48f,.86f),5);
            _prompt=ui.Panel("Interaction prompt",_root,0,96,650,66,UiFactoryV17.Forest,new Vector2(.5f,0));
            var cap=ui.Panel("Interaction key",_prompt,13,-13,40,40,new Color(.18f,.35f,.28f));_key=ui.Label(cap,"E",0,0,40,40,24,UiFactoryV17.Cream,true,TextAnchor.MiddleCenter);
            _promptText=ui.Label(_prompt,"",70,-4,558,58,24,UiFactoryV17.Cream,true,TextAnchor.MiddleLeft);
            _controls=ui.Panel("Exploration hints",_root,24,24,1000,42,UiFactoryV17.Forest,new Vector2(0,0));ui.Label(_controls,"WASD  Move     Shift  Run     Space  Jump     Hold RMB  Inhale     LMB  Use power     E  Interact     Esc  Pause",15,-1,970,40,17,UiFactoryV17.Muted,false,TextAnchor.MiddleLeft);
            _crosshair=ui.Rect("Ranged aim",_root,0,0,18,18,new Vector2(.5f,.5f));ui.Icon(_crosshair,UiGlyphKindV16.None,0,0,18,UiFactoryV17.Cream);
            _clusters=new[]{vital,power,relic,_objectiveCard,pauseRoot,_boss,_prompt,_controls};
            _boss.gameObject.SetActive(false);_prompt.gameObject.SetActive(false);_crosshair.gameObject.SetActive(false);
        }
        public void State(GameState state,UiValuesV16 values) { _vital.Snap();_guardian.Snap();_root.gameObject.SetActive(state==GameState.Playing);Apply(values); }
        public void Fit(float width,float height)
        {
            UiSurfaceV19 surface=UiLayoutV19.Surface(width,height);
            _root.sizeDelta=new Vector2(surface.Width,surface.Height);_root.localScale=Vector3.one*surface.Scale;
        }
        public void Apply(UiValuesV16 values)
        {
            _reduceMotion=values.ReduceMotion;if(_reduceMotion){_vital.Snap();_guardian.Snap();}
            Color trail=UiFactoryV17.Gold;trail.a=values.HighContrast?1f:.65f;_healthTrailPaint.color=_bossTrailPaint.color=trail;
            RenderBars();
            foreach(RectTransform rect in _clusters)rect.localScale=Vector3.one*values.HudScale;
            _powerSlot.anchoredPosition=new Vector2(24,-(38+92*values.HudScale));
            _objectiveCard.anchoredPosition=new Vector2(-24,-(40+82*values.HudScale));
            _pauseShortcut.anchoredPosition=new Vector2(-24,-(values.Hints?56+186*values.HudScale:40+82*values.HudScale));
            _objectiveCard.gameObject.SetActive(values.Hints);_controls.gameObject.SetActive(values.Hints);
            _crosshair.gameObject.SetActive(values.Crosshair&&_ranged);_boss.gameObject.SetActive(_bossVisible);
        }
        public void Health(int current,int max)
        {
            _vital.Observe(current,max);if(_reduceMotion)_vital.Snap();
            _healthText.text=_vital.Current+" / "+_vital.Maximum;_healthLabel.text=_vital.Low?"LOW VITALITY":"VITALITY";
            _healthLabel.color=_vital.Low?UiFactoryV17.Coral:UiFactoryV17.Muted;_healthPaint.color=_vital.Low?UiFactoryV17.Coral:UiFactoryV17.Mint;RenderBars();
        }
        public void Power(AbilityDefinition ability)
        {
            AbilityType type=ability!=null?ability.Type:AbilityType.None;_ranged=type==AbilityType.Bow||type==AbilityType.Fire;
            _powerText.text=ability!=null?ability.DisplayName:"No power";_powerHint.text=type==AbilityType.None?"No ability held":_ranged?"Ranged ability":"Melee ability";
            _powerIcon.Kind=type==AbilityType.Sword?UiGlyphKindV16.Sword:type==AbilityType.Bow?UiGlyphKindV16.Bow:type==AbilityType.Fire?UiGlyphKindV16.Fire:UiGlyphKindV16.None;
            _powerIcon.color=type==AbilityType.Sword?UiFactoryV17.Gold:type==AbilityType.Fire?UiFactoryV17.Coral:UiFactoryV17.Mint;
        }
        public void Relics(int count,int total) { _relicText.text=count+" / "+total;_relicFill.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,total>0?224f*Mathf.Clamp01((float)count/total):0);_relicPulse=.6f; }
        public void Boss(bool visible) { if(visible!=_bossVisible)_guardian.Snap();_bossVisible=visible;_boss.gameObject.SetActive(visible);RenderBars(); }
        public void BossHealth(int current,int max) { _guardian.Observe(current,max);if(_reduceMotion)_guardian.Snap();_bossText.text=_guardian.Current+" / "+_guardian.Maximum;RenderBars(); }
        public void Objective(bool defeated,bool inBoss) => _objective.text=defeated?"Restore the Island Heart":inBoss?"Defeat the Corrupted Guardian":"Follow the trails to the Guardian";
        public void Prompt(string text)
        {
            bool show=!string.IsNullOrWhiteSpace(text);_prompt.gameObject.SetActive(show);if(!show)return;
            string value=text.Trim();bool key=value.StartsWith("E ",StringComparison.Ordinal);_key.text=key?"E":"!";_promptText.text=key?value.Substring(1).Trim():value;
        }
        public void Tick(UiValuesV16 values)
        {
            _vital.Tick(Time.unscaledDeltaTime,values.ReduceMotion);_guardian.Tick(Time.unscaledDeltaTime,values.ReduceMotion);RenderBars();
            _relicPulse=Mathf.Max(0,_relicPulse-Time.unscaledDeltaTime);_relicText.color=values.ReduceMotion?UiFactoryV17.Cream:Color.Lerp(UiFactoryV17.Cream,UiFactoryV17.Gold,_relicPulse/.6f);
        }
        private void RenderBars()
        {
            Width(_healthFill,206*_vital.Fraction);Width(_healthTrail,206*_vital.Trail);
            Width(_bossFill,536*_guardian.Fraction);Width(_bossTrail,536*_guardian.Trail);
        }
        private static void Width(RectTransform rect,float width){if(rect.sizeDelta.x!=width)rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,width);}
    }
}
