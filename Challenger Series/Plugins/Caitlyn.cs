﻿using System;
using System.Collections.Generic;
using System.Linq;
using Challenger_Series.Utils;
using LeagueSharp;
using LeagueSharp.SDK;
using SharpDX;
using Color = System.Drawing.Color;
using Challenger_Series.Utils;
using System.Windows.Forms;
using LeagueSharp.Data.Enumerations;
using LeagueSharp.SDK.Enumerations;
using LeagueSharp.SDK.UI;
using LeagueSharp.SDK.Utils;
using Menu = LeagueSharp.SDK.UI.Menu;

namespace Challenger_Series.Plugins
{
    public class Caitlyn : CSPlugin
    {
        public Caitlyn()
        {
            Q = new Spell(SpellSlot.Q, 1200);
            W = new Spell(SpellSlot.W, 820);
            E = new Spell(SpellSlot.E, 770);
            R = new Spell(SpellSlot.R, 2000);

            Q.SetSkillshot(0.25f, 50f, 2000f, false, SkillshotType.SkillshotLine);
            W.SetSkillshot(1.00f, 100f, float.MaxValue, false, SkillshotType.SkillshotCircle);
            E.SetSkillshot(0.25f, 80f, 1600f, true, SkillshotType.SkillshotLine);
            R.SetSkillshot(3.00f, 50f, 1000f, false, SkillshotType.SkillshotLine);
            InitMenu();
            Orbwalker.OnAction += OnAction;
            DelayedOnUpdate += OnUpdate;
            Drawing.OnDraw += OnDraw;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            Obj_AI_Base.OnPlayAnimation += OnPlayAnimation;
            Events.OnGapCloser += OnGapCloser;
            Events.OnInterruptableTarget += OnInterruptableTarget;
        }

        public override void OnUpdate(EventArgs args)
        {
            if (Q.IsReady()) this.QLogic();
            if (W.IsReady()) this.WLogic();
            if (R.IsReady()) this.RLogic();
        }

        private void OnPlayAnimation(Obj_AI_Base sender, GameObjectPlayAnimationEventArgs args)
        {
            if (sender.IsMe && args.Animation == "Spell3")
            {
                var target = TargetSelector.GetTarget(1000, DamageType.Physical);
                var pred = Prediction.GetPrediction(target, Q);
                if (AlwaysQAfterE)
                {
                    if ((int)pred.Item1 >= (int)HitChance.Medium
                        && pred.Item2.Distance(ObjectManager.Player.ServerPosition) < 1100) Q.Cast(pred.Item2);
                }
                else
                {
                    if ((int)pred.Item1 > (int)HitChance.Medium
                        && pred.Item2.Distance(ObjectManager.Player.ServerPosition) < 1100) Q.Cast(pred.Item2);
                }
            }
        }

        private void OnGapCloser(object oSender, Events.GapCloserEventArgs args)
        {
            var sender = args.Sender;
            if (UseEAntiGapclose)
            {
                if (args.IsDirectedToPlayer && args.Sender.Distance(ObjectManager.Player) < 750)
                {
                    if (E.IsReady())
                    {
                        E.Cast(sender.ServerPosition);
                    }
                }
            }
        }

        private void OnInterruptableTarget(object oSender, Events.InterruptableTargetEventArgs args)
        {
            var sender = args.Sender;
            if (!GameObjects.AllyMinions.Any(m => !m.IsDead && m.CharData.BaseSkinName.Contains("trap") && m.Distance(sender.ServerPosition) < 100) && ObjectManager.Player.Distance(sender) < 550)
            {
                W.Cast(sender.ServerPosition);
            }
        }

        private void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            base.OnProcessSpellCast(sender, args);
            if (sender is Obj_AI_Hero && sender.IsEnemy)
            {
                if (args.SData.Name == "summonerflash" && args.End.Distance(ObjectManager.Player.ServerPosition) < 650)
                {
                    var pred = Prediction.GetPrediction((Obj_AI_Hero) args.Target, E);
                    if (!pred.Item3.Any(o => o.IsMinion && !o.IsDead && !o.IsAlly))
                    {
                        E.Cast(args.End);
                    }
                }
            }
        }

        public override void OnDraw(EventArgs args)
        {
            var drawRange = DrawRange.Value;
            if (drawRange > 0)
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, drawRange, Color.Gold);
            }
            var victims =
                   GameObjects.EnemyHeroes.Where(
                       x =>
                           x.IsValid && !x.IsDead && x.IsEnemy &&
                           (x.IsVisible && x.IsValidTarget()) &&
                           ObjectManager.Player.GetSpellDamage(x, SpellSlot.R) >= x.Health - 150)
                       .Aggregate("", (current, target) => current + (target.ChampionName + " "));

            if (victims != "" && R.IsReady())
            {
                    Drawing.DrawText(Drawing.Width * 0.44f, Drawing.Height * 0.7f, System.Drawing.Color.GreenYellow,
                        "Ult can kill: " + victims);
            }
        }

        private void OnAction(object sender, OrbwalkingActionArgs orbwalkingActionArgs)
        {
            if (orbwalkingActionArgs.Type == OrbwalkingType.BeforeAttack)
            {
                /*if (orbwalkingActionArgs.Target is Obj_AI_Minion && HasPassive && FocusOnHeadShotting &&
                    Orbwalker.ActiveMode == OrbwalkingMode.LaneClear)
                {
                    var target = orbwalkingActionArgs.Target as Obj_AI_Minion;
                    if (target != null && !target.CharData.BaseSkinName.Contains("MinionSiege") && target.Health > 60)
                    {
                        var tg = (Obj_AI_Hero)TargetSelector.GetTarget(715, DamageType.Physical);
                        if (tg != null && tg.IsHPBarRendered)
                        {
                            Orbwalker.ForceTarget = tg;
                            orbwalkingActionArgs.Process = false;
                        }
                    }
                }*/
            }
            if (orbwalkingActionArgs.Type == OrbwalkingType.AfterAttack)
            {
                Orbwalker.ForceTarget = null;
                if (E.IsReady() && this.UseECombo)
                {
                    if (!OnlyUseEOnMelees)
                    {
                        var eTarget = TargetSelector.GetTarget(UseEOnEnemiesCloserThanSlider.Value, DamageType.Physical);
                        if (eTarget != null)
                        {
                            var pred = Prediction.GetPrediction(eTarget, E);
                            if (pred.Item3.Count == 0 && (int)pred.Item1 >= (int)HitChance.High)
                            {
                                E.Cast(pred.Item2);
                            }
                        }
                    }
                    else
                    {
                        var eTarget =
                            ValidTargets.FirstOrDefault(
                                e =>
                                e.IsMelee && e.Distance(ObjectManager.Player) < UseEOnEnemiesCloserThanSlider.Value
                                && !e.IsZombie);
                        var pred = Prediction.GetPrediction(eTarget, E);
                        if (pred.Item3.Count == 0 && (int)pred.Item1 > (int)HitChance.Medium)
                        {
                            E.Cast(pred.Item2);
                        }
                    }
                }
            }
        }

        private Menu ComboMenu;

        private MenuBool UseQCombo;

        private MenuBool UseECombo;
        

        private MenuKeyBind UseRCombo;

        private MenuBool AlwaysQAfterE;

        private MenuBool FocusOnHeadShotting;

        private MenuList<string> QHarassMode;

        private MenuBool UseWInterrupt;

        private Menu AutoWConfig;

        private MenuSlider UseEOnEnemiesCloserThanSlider;

        private MenuBool OnlyUseEOnMelees;

        private MenuBool UseEAntiGapclose;

        private MenuSlider DrawRange;

        public void InitMenu()
        {
            ComboMenu = MainMenu.Add(new Menu("caitcombomenu", "Combo Settings: "));
            UseQCombo = ComboMenu.Add(new MenuBool("caitqcombo", "Use Q", true));
            UseECombo = ComboMenu.Add(new MenuBool("caitecombo", "Use E Combo", true));
            UseRCombo = ComboMenu.Add(new MenuKeyBind("caitrcombo", "Use R", Keys.R, KeyBindType.Press));
            AutoWConfig = MainMenu.Add(new Menu("caitautow", "W Settings: "));
            UseWInterrupt = AutoWConfig.Add(new MenuBool("caitusewinterrupt", "Use W to Interrupt", true));
            new Utils.Logic.PositionSaver(AutoWConfig, W);
            FocusOnHeadShotting =
                MainMenu.Add(new MenuBool("caitfocusonheadshottingenemies", "Try to save Headshot for poking", true));
            AlwaysQAfterE = MainMenu.Add(new MenuBool("caitalwaysqaftere", "Always Q after E (EQ combo)", true));
            QHarassMode =
                MainMenu.Add(
                    new MenuList<string>(
                        "caitqharassmode",
                        "Q HARASS MODE",
                        new[] { "FULLDAMAGE", "ALLOWMINIONS", "DISABLED" }));
            UseEAntiGapclose = MainMenu.Add(new MenuBool("caiteantigapclose", "Use E AntiGapclose", false));
            UseEOnEnemiesCloserThanSlider =
                MainMenu.Add(new MenuSlider("caitescape", "Use E on enemies closer than", 400, 100, 650));
            OnlyUseEOnMelees = MainMenu.Add(new MenuBool("caiteonlymelees", "Only use E on melees", false));
            DrawRange = MainMenu.Add(new MenuSlider("caitdrawrange", "Draw a circle with radius: ", 800, 0, 1240));
            MainMenu.Attach();
        }

        #region Logic

        void QLogic()
        {
            if (Orbwalker.ActiveMode == OrbwalkingMode.Combo)
            {
                if (UseQCombo && Q.IsReady() && ObjectManager.Player.CountEnemyHeroesInRange(800) == 0
                    && ObjectManager.Player.CountEnemyHeroesInRange(1100) > 0)
                {
                    var goodQTarget =
                        ValidTargets.FirstOrDefault(
                            t =>
                            t.Distance(ObjectManager.Player) < 950 && t.Health < Q.GetDamage(t)
                            || SquishyTargets.Contains(t.CharData.BaseSkinName));
                    if (goodQTarget != null)
                    {
                        var pred = Prediction.GetPrediction(goodQTarget, Q);
                        if ((int)pred.Item1 > (int)HitChance.Medium && pred.Item2.Distance(ObjectManager.Player.Position) < 1100)
                        {
                            Q.Cast(pred.Item2);
                        }
                    }
                }
            }
            if (Orbwalker.ActiveMode != OrbwalkingMode.None && Orbwalker.ActiveMode != OrbwalkingMode.Combo
                && ObjectManager.Player.CountEnemyHeroesInRange(850) == 0)
            {
                var qHarassMode = QHarassMode.SelectedValue;
                if (qHarassMode != "DISABLED")
                {
                    var qTarget = TargetSelector.GetTarget(1100, DamageType.Physical);
                    if (qTarget != null)
                    {
                        var pred = Prediction.GetPrediction(qTarget, Q);
                        if ((int)pred.Item1 > (int)HitChance.Medium && pred.Item2.Distance(ObjectManager.Player.Position) < 1100)
                        {
                            if (qHarassMode == "ALLOWMINIONS")
                            {
                                Q.Cast(pred.Item2);
                            }
                            else if (pred.Item3.Count == 0)
                            {
                                Q.Cast(pred.Item2);
                            }
                        }
                    }
                }
            }
        }

        void WLogic()
        {
            var goodTarget =
                ValidTargets.FirstOrDefault(
                    e =>
                    !e.IsDead && e.HasBuffOfType(BuffType.Knockup) || e.HasBuffOfType(BuffType.Snare)
                    || e.HasBuffOfType(BuffType.Stun) || e.HasBuffOfType(BuffType.Suppression) || e.IsCharmed
                    || e.IsCastingInterruptableSpell() || !e.CanMove);
            if (goodTarget != null)
            {
                var pos = goodTarget.ServerPosition;
                if (!GameObjects.AllyMinions.Any(m => !m.IsDead && m.CharData.BaseSkinName.Contains("trap") && m.Distance(goodTarget.ServerPosition) < 100) && pos.Distance(ObjectManager.Player.ServerPosition) < 820)
                {
                    W.Cast(goodTarget.ServerPosition);
                }
            }
            foreach (var enemyMinion in
                ObjectManager.Get<Obj_AI_Base>()
                    .Where(
                        m =>
                        m.IsEnemy && m.ServerPosition.Distance(ObjectManager.Player.ServerPosition) < W.Range
                        && m.HasBuff("teleport_target")))
            {

                W.Cast(enemyMinion.ServerPosition);
            }
            foreach (var hero in GameObjects.EnemyHeroes.Where(h=>h.Distance(ObjectManager.Player) < W.Range))
            {
                var pred = Prediction.GetPrediction(hero, W);
                if (!GameObjects.AllyMinions.Any(m => !m.IsDead && m.CharData.BaseSkinName.Contains("trap") && m.Distance(pred.Item2) < 100) && (int) pred.Item1 > (int) HitChance.Medium)
                {
                    W.Cast(pred.Item2);
                }
            }
        }

        void RLogic()
        {
            if (UseRCombo.Active && R.IsReady() && ObjectManager.Player.CountEnemyHeroesInRange(900) == 0)
            {
                foreach (var rTarget in
                    ValidTargets.Where(
                        e =>
                        SquishyTargets.Contains(e.CharData.BaseSkinName) && R.GetDamage(e) > 0.1 * e.MaxHealth
                        || R.GetDamage(e) > e.Health))
                {
                    if (rTarget.Distance(ObjectManager.Player) > 1400)
                    {
                        var pred = Prediction.GetPrediction(rTarget, R);
                        if (!pred.Item3.Any(obj => obj is Obj_AI_Hero))
                        {
                            R.CastOnUnit(rTarget);
                        }
                        break;
                    }
                    R.CastOnUnit(rTarget);
                }
            }
        }

        #endregion

        private bool HasPassive => ObjectManager.Player.HasBuff("caitlynheadshot");

        private string[] SquishyTargets =
            {
                "Ahri", "Anivia", "Annie", "Ashe", "Azir", "Brand", "Caitlyn", "Cassiopeia",
                "Corki", "Draven", "Ezreal", "Graves", "Jinx", "Kalista", "Karma", "Karthus",
                "Katarina", "Kennen", "KogMaw", "Kindred", "Leblanc", "Lucian", "Lux",
                "MissFortune", "Orianna", "Quinn", "Sivir", "Syndra", "Talon", "Teemo",
                "Tristana", "TwistedFate", "Twitch", "Varus", "Vayne", "Veigar", "Velkoz",
                "Viktor", "Xerath", "Zed", "Ziggs", "Jhin", "Soraka"
            };

        
    }
}