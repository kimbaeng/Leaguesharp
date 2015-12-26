using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace Kimbaeng_Shen
{

    class Program
    {
        private static Menu _Menu;

        private static Orbwalking.Orbwalker _Orbwalker;


        private static Spell Q, W, E, R, EFlash;

        private static SpellSlot IgniteSlot;

        private static SpellSlot FlashSlot;

        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        static void Game_OnGameLoad(EventArgs args)
        {
            if (ObjectManager.Player.ChampionName != "Shen") return;

            Q = new Spell(SpellSlot.Q, 475f);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 600);
            R = new Spell(SpellSlot.R);
            EFlash = new Spell(SpellSlot.E, 990);
            EFlash.SetSkillshot(
                E.Instance.SData.SpellCastTime, E.Instance.SData.LineWidth, E.Speed, false, SkillshotType.SkillshotLine);

            Q.SetTargetted(0.15f, float.MaxValue);
            E.SetSkillshot(0.25f, 150f, float.MaxValue, false, SkillshotType.SkillshotLine);
            IgniteSlot = ObjectManager.Player.GetSpellSlot("SummonerDot");
            FlashSlot = ObjectManager.Player.GetSpellSlot("SummonerFlash");
            (_Menu = new Menu("Kimbaeng Shen", "kimbaengshen", true)).AddToMainMenu();

            var targetSelectorMenu = new Menu("Target Selector", "TargetSelector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            _Menu.AddSubMenu(targetSelectorMenu);

            _Orbwalker = new Orbwalking.Orbwalker(_Menu.AddSubMenu(new Menu("Orbwalking", "Orbwalking")));

            var comboMenu = _Menu.AddSubMenu(new Menu("combo", "Combo"));
            comboMenu.AddItem(new MenuItem("useCQ", "Use Q").SetValue(true));
            comboMenu.AddItem(new MenuItem("useCE", "Use E").SetValue(true));
            comboMenu.AddItem(new MenuItem("UseI", "Use Ignite").SetValue(true));
            comboMenu.AddItem(new MenuItem("useEF", "Use E+Flash"))
                .SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press));

            var harassMenu = _Menu.AddSubMenu(new Menu("Harass", "Harass"));
            harassMenu.AddItem(new MenuItem("useHQ", "UseQ").SetValue(true));
            harassMenu.AddItem(new MenuItem("useHE", "UseE").SetValue(true));
            harassMenu.AddItem(new MenuItem("autoHQ", "Auto Q Harass")
                .SetValue(new KeyBind("G".ToCharArray()[0], KeyBindType.Toggle))).Permashow(true, "Shen Toggle Q Harass");
            harassMenu.AddItem(new MenuItem("keepE", "Keep energy for E").SetValue(true));

            var laneMenu = _Menu.AddSubMenu(new Menu("Lane Clear", "laneclear"));
            laneMenu.AddItem(new MenuItem("useLQ", "Use Q").SetValue(true));

            var LastHitMenu = _Menu.AddSubMenu(new Menu("LastHit", "LastHit"));
            LastHitMenu.AddItem(new MenuItem("useLHQ", "Use Q").SetValue(true));
            var DrawMenu = _Menu.AddSubMenu(new Menu("Drawing", "drawing"));

            DrawMenu.AddItem(new MenuItem("noDraw", "Disable Drawing").SetValue(false));
            DrawMenu.AddItem(new MenuItem("drawQ", "DrawQ").SetValue(new Circle(true, System.Drawing.Color.Olive)));
            DrawMenu.AddItem(new MenuItem("drawE", "DrawE").SetValue(new Circle(true, System.Drawing.Color.Olive)));
            DrawMenu.AddItem(new MenuItem("drawEF", "DrawEFlash").SetValue(new Circle(true, System.Drawing.Color.Olive)));
            Game.OnUpdate += Game_onUpdate;
            Drawing.OnDraw += Drawing_Ondraw;
            //Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;

            Game.PrintChat("<font color=\"#0000A5\">Kimbaeng Shen</font> Loaded ");
            Game.PrintChat("If You like this Assembly plz <font color=\"#41FF3A\">Upvote</font> XD ");
        }

        private static void Game_onUpdate(EventArgs args)
        {

            if (_Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                Combo();
            }

            if (_Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
            {
                //Harass();
            }

            if (_Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {
            }

            if (_Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit)
            {
                LastHit();
            }

            if (_Menu.Item("useEF").GetValue<KeyBind>().Active)
            {
                FlashECombo();
            }
        }

        //public static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        //{
        //    if (_Menu.Item("autow").GetValue<bool>())
        //    {
        //        if (W.IsReady() && sender.IsEnemy && !sender.IsMe && (sender is Obj_AI_Hero || sender is Obj_AI_Turret)
        //                 && args.Target.IsMe)
        //        {
        //            W.Cast();
        //        }
        //    }
        //}


        private static void Combo()
        {
            var Target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);

            var useQ = _Menu.Item("useCQ").GetValue<bool>();
            var useE = _Menu.Item("useCE").GetValue<bool>();
            var UseI = _Menu.Item("UseI").GetValue<bool>();

            if (E.IsReady() && useE && ObjectManager.Player.Distance(Target) < E.Range)
            {
                E.Cast(Target);
            }
            if (Q.IsReady() && useQ)
            {
                Q.Cast(Target);
            }

            if (IgniteSlot != SpellSlot.Unknown &&
                ObjectManager.Player.Spellbook.CanUseSpell(IgniteSlot) == SpellState.Ready &&
                ObjectManager.Player.Distance(Target.ServerPosition) < 600 &&
                ObjectManager.Player.GetSummonerSpellDamage(Target, Damage.SummonerSpell.Ignite) > Target.Health && UseI)
            {
                ObjectManager.Player.Spellbook.CastSpell(IgniteSlot, Target);
            }
        }

        private static void FlashECombo()
        {
            var FTarget = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
            var STarget = TargetSelector.GetTarget(EFlash.Range, TargetSelector.DamageType.Magical, false, FTarget != null ? new[] { FTarget } : null);
            var EFTarget = TargetSelector.GetTarget(EFlash.Range, TargetSelector.DamageType.Magical);

            if (EFTarget != null)
            {
                if (E.IsReady() && FlashSlot != SpellSlot.Unknown
                    && ObjectManager.Player.Spellbook.CanUseSpell(FlashSlot) == SpellState.Ready)
                {
                    E.Cast(EFTarget.Position);
                }
                if (ObjectManager.Player.IsDashing() && ObjectManager.Player.Distance(EFTarget.Position) < 400)
                {
                    ObjectManager.Player.Spellbook.CastSpell(FlashSlot, EFTarget);
                }
            }



            if (STarget != null)
            {
                //if (FTarget != null && E.IsReady() && FlashSlot != SpellSlot.Unknown
                //    && Player.Spellbook.CanUseSpell(FlashSlot) == SpellState.Ready)
                //{
                //    E.Cast(FTarget.Position);
                //    if (FTarget.HasBuffOfType(BuffType.Taunt))
                //    {
                //        ObjectManager.Player.Spellbook.CastSpell(FlashSlot, STarget);
                //    }

                //}
                //else
                //{
                //if (E.IsReady() && FlashSlot != SpellSlot.Unknown
                //    && ObjectManager.Player.Spellbook.CanUseSpell(FlashSlot) == SpellState.Ready)
                //    {
                //        E.Cast(STarget.Position);
                    //Utility.DelayAction.Add(
                    //      450,
                    //      () => ObjectManager.Player.Spellbook.CastSpell(FlashSlot, STarget.Position));
                    //}
                //}
             }
            ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
        }

        static void LastHit()
        {
            var useQ = _Menu.Item("useLHQ").GetValue<bool>();

            if (!Q.IsReady())
            {
                return;
            }

            var minions = MinionManager.GetMinions(
                ObjectManager.Player.Position,
                Q.Range,
                MinionTypes.All,
                MinionTeam.NotAlly,
                MinionOrderTypes.MaxHealth);
            if (minions.Count > 0 && useQ)
            {
                foreach (var minion in minions)
                {
                    if (ObjectManager.Player.Distance(minion) <= ObjectManager.Player.AttackRange)
                    {
                        return;
                    }
                    if (HealthPrediction.GetHealthPrediction(
                        minion,
                        (int)(Q.Delay + (minion.Distance(ObjectManager.Player.Position) / Q.Speed)))
                        < ObjectManager.Player.GetSpellDamage(minion, SpellSlot.Q))
                    {
                        Q.Cast(minion);
                    }
                }
            }
        }

        private static void Drawing_Ondraw(EventArgs args)
        {
            var FTarget = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
var STarget = TargetSelector.GetTarget(EFlash.Range, TargetSelector.DamageType.Magical, false, FTarget != null ? new[] { FTarget } : null);


            if (FTarget != null)
            {
                Render.Circle.DrawCircle(FTarget.Position, 50, System.Drawing.Color.Red);
                Drawing.DrawText(
                   Drawing.WorldToScreen(FTarget.Position)[0] - 30,
                   Drawing.WorldToScreen(FTarget.Position)[1] + 20,
                   System.Drawing.Color.Red,
                   "First Target");
            }

            if (STarget != null)
            {
                Render.Circle.DrawCircle(STarget.Position, 50, System.Drawing.Color.OrangeRed);
                Drawing.DrawText(
                    Drawing.WorldToScreen(STarget.Position)[0] - 30,
                    Drawing.WorldToScreen(STarget.Position)[1] + 20,
                    System.Drawing.Color.OrangeRed,
                    "Second Target");
            }
            if (_Menu.Item("noDraw").GetValue<bool>())
            {
                return;
            }

            var Qvalue = _Menu.Item("drawQ").GetValue<Circle>();
            var Evalue = _Menu.Item("drawE").GetValue<Circle>();
            var EFvalue = _Menu.Item("drawEF").GetValue<Circle>();


            if (Qvalue.Active)
            {
                if (Q.Instance.Level != 0)
                Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range, Qvalue.Color);

            }

            if (Evalue.Active)
            {
                if (E.Instance.Level != 0)
                Render.Circle.DrawCircle(ObjectManager.Player.Position, E.Range, Evalue.Color);
            }

            if (EFvalue.Active)
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, EFlash.Range, EFvalue.Color);
            }
            
            
         }
    }
}
