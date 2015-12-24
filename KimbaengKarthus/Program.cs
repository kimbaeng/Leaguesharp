using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace Kimbaeng_KarThus
{

    internal class Program
    {
        public static Menu _menu;

        public static HpBarIndicator Hpi = new HpBarIndicator();

        private static Obj_AI_Hero Player;

        private static Orbwalking.Orbwalker _orbwalker;

        public static Spell Q;

        public static Spell W;

        public static Spell E;

        public static Spell R;

        private static bool _comboE;

        private static Vector2 PingLocation;

        private static int LastPingT = 0;

        private const float SpellQWidth = 160f;

        private const float SpellWWidth = 160f;

        public static SpellSlot IgniteSlot;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (ObjectManager.Player.ChampionName != "Karthus")
            {
                return;
            }

            Player = ObjectManager.Player;
            IgniteSlot = ObjectManager.Player.GetSpellSlot("SummonerDot");
            Q = new Spell(SpellSlot.Q, 875);
            W = new Spell(SpellSlot.W, 1000);
            E = new Spell(SpellSlot.E, 550);
            R = new Spell(SpellSlot.R, 20000f);

            Q.SetSkillshot(0.66f, 160f, 2000, false, SkillshotType.SkillshotCircle);
            W.SetSkillshot(0.65f, 100f, 1600f, false, SkillshotType.SkillshotLine);
            E.SetSkillshot(1f, 505, float.MaxValue, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(3f, float.MaxValue, float.MaxValue, false, SkillshotType.SkillshotCircle);
            (_menu = new Menu("Kimbaeng Karthus", "kimbaengkarthus", true)).AddToMainMenu();

            var targetSelectorMenu = new Menu("Target Selector", "TargetSelector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            _menu.AddSubMenu(targetSelectorMenu);

            _orbwalker = new Orbwalking.Orbwalker(_menu.AddSubMenu(new Menu("Orbwalking", "Orbwalking")));
            _orbwalker.SetAttack(true);

            var HitchanceMenu = _menu.AddSubMenu(new Menu("Hitchance", "Hitchance"));
            HitchanceMenu.AddItem(
                new MenuItem("Hitchance", "Hitchance").SetValue(
                    new StringList(new[] { "Low", "Medium", "High", "VeryHigh", "Impossible" }, 3)));

            var comboMenu = _menu.AddSubMenu(new Menu("combo", "Combo"));
            comboMenu.AddItem(new MenuItem("useQ", "Use Q").SetValue(true));
            comboMenu.AddItem(new MenuItem("useW", "Use W").SetValue(true));
            comboMenu.AddItem(new MenuItem("useE", "Use E").SetValue(true));
            comboMenu.AddItem(new MenuItem("comboAA", "Use AA").SetValue(false));
            comboMenu.AddItem(new MenuItem("string", "if No Mana(100↓), Allow Use AA"));
            comboMenu.AddItem(new MenuItem("UseI", "Use Ignite").SetValue(true));

            var harassMenu = _menu.AddSubMenu(new Menu("Harass", "Harass"));
            harassMenu.AddItem(new MenuItem("useQHarass", "UseQ").SetValue(true));
            harassMenu.AddItem(new MenuItem("useEHarass", "UseE").SetValue(true));
            harassMenu.AddItem(new MenuItem("harassAA", "Use AA").SetValue(false));
            harassMenu.AddItem(new MenuItem("autoqh", "Auto Q Harass").SetValue(false));
            harassMenu.AddItem(new MenuItem("harassmana", "Mana %").SetValue(new Slider(50)));

            var LastHitMenu = _menu.AddSubMenu(new Menu("LastHit", "LastHit"));
            LastHitMenu.AddItem(new MenuItem("useqlasthit", "Use Q").SetValue(true));

            var MiscMenu = _menu.AddSubMenu(new Menu("Misc", "Misc"));
            var ultMenu = MiscMenu.AddSubMenu(new Menu("Ult", "Ult"));
            ultMenu.AddItem(new MenuItem("NotifyUlt", "Notify Ult Text").SetValue(true));
            ultMenu.AddItem(new MenuItem("NotifyPing", "Notify Ult Ping").SetValue(true));

            MiscMenu.AddItem(new MenuItem("estate", "Auto E if No Target").SetValue(true));

            var DrawMenu = _menu.AddSubMenu(new Menu("Draw", "drawing"));
            DrawMenu.AddItem(new MenuItem("noDraw", "Disable Drawing").SetValue(true));
            DrawMenu.AddItem(new MenuItem("drawQ", "DrawQ").SetValue(new Circle(true, System.Drawing.Color.Goldenrod)));
            DrawMenu.AddItem(new MenuItem("drawW", "DrawW").SetValue(new Circle(false, System.Drawing.Color.Goldenrod)));
            DrawMenu.AddItem(new MenuItem("drawE", "DrawE").SetValue(new Circle(false, System.Drawing.Color.Goldenrod)));

            Drawing.OnDraw += Drawing_Ondraw;
            Game.OnUpdate += Game_OnUpdate;

            Game.PrintChat("Kimbaeng<font color=\"#030066\">Karthus</font> Loaded");
            Game.PrintChat("If You like this Assembly plz <font color=\"#1DDB16\">Upvote</font> XD ");
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (_menu.Item("NotifyUlt").GetValue<bool>())
            {
                AutoUlt();
            }

            if (_menu.Item("NotifyPing").GetValue<bool>())
            {
                NotifyPing();
            }

            if (_menu.Item("autoqh").GetValue<bool>())
            {
                Harass();
            }
            if (_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                _orbwalker.SetAttack(_menu.Item("comboAA").GetValue<bool>() || ObjectManager.Player.Mana < 100);
                Combo();
            }


            if (_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
            {
                _orbwalker.SetAttack(_menu.Item("harassAA").GetValue<bool>());
                Harass();
                LastHit();
            }

            if (_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {
                LaneClear();
            }
            if (_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit)
            {
                LastHit();
            }
                RegulateEState();
        }

        private static void Drawing_Ondraw(EventArgs args)
        {
            if (_menu.Item("noDraw").GetValue<bool>())
            {
                return;
            }

            var qValue = _menu.Item("drawQ").GetValue<Circle>();
            var wValue = _menu.Item("drawW").GetValue<Circle>();
            var eValue = _menu.Item("drawE").GetValue<Circle>();

            if (qValue.Active)
            {
                if (Q.Instance.Level != 0)
                Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range, qValue.Color);
            }

            if (wValue.Active)
            {
                if (W.Instance.Level != 0)
                Render.Circle.DrawCircle(ObjectManager.Player.Position, W.Range, wValue.Color);
            }

            if (eValue.Active)
            {
                if (E.Instance.Level != 0)
                Render.Circle.DrawCircle(ObjectManager.Player.Position, E.Range, eValue.Color);
            }

        }

        private static void AutoUlt()
        {
            if (R.Instance.Level == 0 && !R.IsReady())
            {
                return;
            }
            else
            {
                foreach (var hero in
                    ObjectManager.Get<Obj_AI_Hero>()
                        .Where(
                            x => ObjectManager.Player.GetSpellDamage(x, SpellSlot.R) >= x.Health && x.IsValidTarget()))
                {
                    Drawing.DrawText(
                        Drawing.WorldToScreen(Player.Position)[0] - 30,
                        Drawing.WorldToScreen(Player.Position)[1] + 20,
                        System.Drawing.Color.Gold,
                        "Can Kill : " + hero.ChampionName);
                }
            }
        }

        private static void NotifyPing()
        {
            if (R.Instance.Level == 0 && !R.IsReady())
            {
                return;
            }
            else
                foreach (var enemy in
                    HeroManager.Enemies.Where(
                        t =>
                        ObjectManager.Player.Spellbook.CanUseSpell(SpellSlot.R) == SpellState.Ready && t.IsValidTarget()
                        && R.GetDamage(t) > t.Health && t.Distance(ObjectManager.Player.Position) > Q.Range))
                {
                    Ping(enemy.Position.To2D());
                }
        }


        private static void Farm()
        {
            ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);

            var allMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range);
            if (Q.IsReady())
            {
                foreach (
                    var minion in
                        allMinions.Where(
                            x =>
                            Player.GetSpellDamage(x, SpellSlot.Q) >= HealthPrediction.GetHealthPrediction(x, (int)(800)))
                    )
                {
                    FarmCast(minion);
                }
            }
        }

        private static void LastHit()
        {
            if (!_menu.Item("useqlasthit").GetValue<bool>()) return;

            if (!Orbwalking.CanMove(40))
                return;

            var eminions = MinionManager.GetMinions(Player.ServerPosition,E.Range,MinionTypes.All,MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
            {

                var minions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All,
                MinionTeam.NotAlly);
                minions.RemoveAll(x => x.MaxHealth <= 5);
                if (minions.Count > 3)
                {
                    foreach (var minion in
                        minions.Where(
                            x => ObjectManager.Player.GetSpellDamage(x, SpellSlot.Q, 1) >=
                            HealthPrediction.GetHealthPrediction(x, (int)(Q.Delay * 1000))))
                    {
                        Q.Cast(minion);
                    }
                }
                else
                {
                    Farm();
                }
            }
            if (eminions.Count == 0)
            {
                RegulateEState();
            }
        }

        private static void LaneClear()
        {

            List<Obj_AI_Base> minions;

            bool jungleMobs;
            if (Q.IsReady())
            {
                minions = MinionManager.GetMinions(
                    ObjectManager.Player.ServerPosition,
                    Q.Range,
                    MinionTypes.All,
                    MinionTeam.NotAlly);
                var eminions = MinionManager.GetMinions(
                    ObjectManager.Player.ServerPosition,
                    E.Range,
                    MinionTypes.All,
                    MinionTeam.NotAlly);
                minions.RemoveAll(x => x.MaxHealth <= 5);

                jungleMobs = minions.Any(x => x.Team == GameObjectTeam.Neutral);

                Q.Width = SpellQWidth;
                var farmInfo = Q.GetCircularFarmLocation(minions, Q.Width);

                if (farmInfo.MinionsHit >= 1)
                {
                    Q.Cast(farmInfo.Position, jungleMobs);
                }
                if (farmInfo.MinionsHit >= 3)
                {
                    E.Cast();
                }
                else if (eminions.Count == 0)
                {
                    RegulateEState();
                }
            }
        }

        public static Vector3 FindHitPosition(PredictionOutput minion)
        {
            int multihit = 1;
            for (int i = -100; i < 100; i = i + 10)
            {
                for (int a = -100; a < 100; a = a + 10)
                {
                    Vector3 tempposition = new Vector3(
                        minion.UnitPosition.X + i,
                        minion.UnitPosition.Y + a,
                        minion.UnitPosition.Z);
                    multihit = CheckMultiHit(tempposition);
                    if (multihit == 1)
                    {
                        return tempposition;
                    }
                }
            }
            return new Vector3(0, 0, 0);
        }

        private static int CheckMultiHit(Vector3 minion)
        {
            var count = 0;
            var allMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range);
            foreach (Obj_AI_Base minionvar in
                allMinions.Where(x => Vector3.Distance(minion, Prediction.GetPrediction(x, 250f).UnitPosition) < 200))
            {
                count++;
            }


            return count;
        }

        private static void FarmCast(Obj_AI_Base minion)
        {
            var position = FindHitPosition(Prediction.GetPrediction(minion, 250f));
            if (!(position.X == 0 && position.Y == 0 && position.Z == 0))
            {
                Q.Cast(position);
            }
        }

        private static void RegulateEState(bool ignoreTargetChecks = false)
        {
            if (_menu.Item("estate").GetValue<bool>())
            {
                if (!E.IsReady() || IsInPassiveForm()
                    || ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E).ToggleState != 2) return;
                var target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
                var minions = MinionManager.GetMinions(
                    ObjectManager.Player.ServerPosition,
                    E.Range,
                    MinionTypes.All,
                    MinionTeam.NotAlly);

                if (!ignoreTargetChecks && (target != null || (!_comboE && minions.Count != 0))) return;
                E.CastOnUnit(ObjectManager.Player);
                _comboE = false;
            }
            else
            {
                return;
            }
        }

        private static bool IsInPassiveForm()
        {
            return ObjectManager.Player.IsZombie;
        }

        private static void PassiveForm()
        {
            if (Player.IsZombie)
            {
                var Target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);

                if (Target != null)
                {
                    Combo();
                }
                else
                {
                    LaneClear();
                }
            }

        }

    private static void Ping(Vector2 position)
        {
            if (LeagueSharp.Common.Utils.TickCount - LastPingT < 30 * 1000)
            {
                return;
            }

            LastPingT = LeagueSharp.Common.Utils.TickCount;
            PingLocation = position;
            SimplePing();

            Utility.DelayAction.Add(150, SimplePing);
            Utility.DelayAction.Add(300, SimplePing);
            Utility.DelayAction.Add(400, SimplePing);
            Utility.DelayAction.Add(800, SimplePing);
        }
        private static void SimplePing()
        {
            Game.ShowPing(PingCategory.Fallback, PingLocation, true);
        }

        private static void Combo()
        {
            var qTarget = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            var wTarget = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Magical);
            var eTarget = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
            var UseQ = _menu.Item("useQ").GetValue<bool>();
            var UseW = _menu.Item("useW").GetValue<bool>();
            var UseE = _menu.Item("useE").GetValue<bool>();
            if (qTarget != null &&  UseQ && Q.IsReady() && qTarget.IsValidTarget())
            {
               var HC = HitChance.VeryHigh;
                switch (_menu.Item("Hitchance").GetValue<StringList>().SelectedIndex)
                {
                    case 0: //Low
                        HC = HitChance.Low;
                        break;
                    case 1: //Medium
                        HC = HitChance.Medium;
                        break;
                    case 2: //High
                        HC = HitChance.High;
                        break;
                    case 3: //Very High
                        HC = HitChance.VeryHigh;
                        break;
                    case 4: //impossable
                        HC = HitChance.Impossible;
                        break;
                }
                Q.CastIfHitchanceEquals(qTarget, HC, true);
            }
            if (eTarget != null && UseE && E.IsReady() && ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E).ToggleState == 1)
            {
                if (ObjectManager.Player.Distance(eTarget.ServerPosition) <= E.Range)
                {
                    _comboE = true;
                    E.Cast();
                }
                }
                else if (eTarget == null && ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E).ToggleState != 1)
                {
                E.Cast();
                }

            if (wTarget != null && UseW  && W.IsReady() && wTarget.IsValidTarget())
            {
                W.Cast(wTarget, false, true);
            }

            if (IgniteSlot != SpellSlot.Unknown &&
                ObjectManager.Player.Spellbook.CanUseSpell(IgniteSlot) == SpellState.Ready &&
                ObjectManager.Player.Distance(qTarget.ServerPosition) < 600 &&
                Player.GetSummonerSpellDamage(qTarget, Damage.SummonerSpell.Ignite) > qTarget.Health)
            {
                ObjectManager.Player.Spellbook.CastSpell(IgniteSlot, qTarget);
            }
        }

        private static void Harass()
        {
            var qTarget = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            var eTarget = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
            var UseQ = _menu.Item("useQHarass").GetValue<bool>();
            var UseE = _menu.Item("useEHarass").GetValue<bool>();

            var HC = HitChance.VeryHigh;
            if (qTarget != null && UseQ && Q.IsReady())
            {
                switch (_menu.Item("Hitchance").GetValue<StringList>().SelectedIndex)
                {
                    case 0: //Low
                        HC = HitChance.Low;
                        break;
                    case 1: //Medium
                        HC = HitChance.Medium;
                        break;
                    case 2: //High
                        HC = HitChance.High;
                        break;
                    case 3: //Very High
                        HC = HitChance.VeryHigh;
                        break;
                    case 4: //impossable
                        HC = HitChance.Impossible;
                        break;
                }
                Q.CastIfHitchanceEquals(qTarget, HC, true);
            }
            if (eTarget != null && UseE && E.IsReady() && ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E).ToggleState == 1)
            {
                E.Cast();
            }
            else if (eTarget == null && ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E).ToggleState != 1)
            {
                E.Cast();
            }
        }

    }
}
