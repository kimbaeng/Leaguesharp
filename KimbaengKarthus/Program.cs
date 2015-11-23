using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace Kimbaeng_KarThus
{
    
    class Program
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


        static void Main(string[] args)
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
            HitchanceMenu.AddItem(new MenuItem("Hitchance", "Hitchance").SetValue(new StringList(new[] { "Low", "Medium", "High", "VeryHigh" })));

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

            var LastHitMenu = _menu.AddSubMenu(new Menu("LastHit", "LastHit"));
            //FreezeMenu.AddItem(new MenuItem("LastHit", "").SetValue(new KeyBind('Z', KeyBindType.Press)));
            LastHitMenu.AddItem(new MenuItem("useQLastHit", "LastHit With Q").SetValue(true));
            LastHitMenu.AddItem(new MenuItem("UseAALastHit", "LastHit With AA").SetValue(true));

            var MiscMenu = _menu.AddSubMenu(new Menu("Misc", "Misc"));
            MiscMenu.AddItem(new MenuItem("NotifyUlt", "Notify Ult Text").SetValue(true));
            MiscMenu.AddItem(new MenuItem("NotifyPing", "Notify Ult Ping").SetValue(true));
            MiscMenu.AddItem(new MenuItem("AutoQ", "AutoQ Immobile Enemmy").SetValue(true));

            var DrawMenu = _menu.AddSubMenu(new Menu("Draw", "drawing"));
            DrawMenu.AddItem(new MenuItem("noDraw", "Disable Drawing").SetValue(false));
            DrawMenu.AddItem(new MenuItem("drawDmg", "DrawR Damage").SetValue(true));
            DrawMenu.AddItem(new MenuItem("drawQ", "DrawQ").SetValue(new Circle(true, System.Drawing.Color.Goldenrod)));
            DrawMenu.AddItem(new MenuItem("drawW", "DrawW").SetValue(new Circle(false, System.Drawing.Color.Goldenrod)));
            DrawMenu.AddItem(new MenuItem("drawE", "DrawE").SetValue(new Circle(false, System.Drawing.Color.Goldenrod)));

            Drawing.OnDraw += Drawing_Ondraw;
            Game.OnUpdate += Game_OnUpdate;
            
            Game.PrintChat("Kimbaeng's Karthus <font color=\"#FF0000\">Loaded</font>");

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

            if (_menu.Item("AutoQ").GetValue<bool>())
            {
                AutoQ();
            }

            switch (_orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    _orbwalker.SetAttack(_menu.Item("comboAA").GetValue<bool>() || ObjectManager.Player.Mana < 100); //if no mana, allow auto attacks!
                    Combo();
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    _orbwalker.SetAttack(true);
                    Harass();
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    _orbwalker.SetAttack(true);
                    LaneClear();
                    break;
                case Orbwalking.OrbwalkingMode.LastHit:
                    _orbwalker.SetAttack(_menu.Item("UseAALastHit").GetValue<bool>());
                    LastHit();
                    break;
                default:
                    _orbwalker.SetAttack(false);
                    _orbwalker.SetMovement(true);
                    RegulateEState();
                    break;
            }
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
                if (Q.Instance.Level == 0) return;
                Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range, qValue.Color);
            }

            if (wValue.Active)
            {
                if (W.Instance.Level == 0) return;
                Render.Circle.DrawCircle(ObjectManager.Player.Position, W.Range, wValue.Color);
            }

            if (eValue.Active)
            {
                if (E.Instance.Level == 0) return;
                Render.Circle.DrawCircle(ObjectManager.Player.Position, E.Range, eValue.Color);
            }

			if (_menu.Item("DrawDmg").GetValue<bool>())
                {
                    foreach (var enemy in
                        ObjectManager.Get<Obj_AI_Hero>().Where(ene => !ene.IsDead && ene.IsEnemy && ene.IsVisible))
                    {
                        Hpi.unit = enemy;
                        var damage = R.GetDamage(enemy);
                        Hpi.drawDmg(damage , System.Drawing.Color.Goldenrod);
                    }
                }

        }

        //Trus Logic
        private static void AutoUlt()
        {
            if (R.Instance.Level == 0)
                return;

            //Drawing.DrawText(Drawing.WorldToScreen(Player.Position)[0] - 30, Drawing.WorldToScreen(Player.Position)[1] + 20, System.Drawing.Color.Gold, "Ult can kill: ");
            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(x => ObjectManager.Player.GetSpellDamage(x, SpellSlot.R) >= x.Health && x.IsValidTarget()))
            {
				Drawing.DrawText(Drawing.WorldToScreen(Player.Position)[0] - 30 , Drawing.WorldToScreen(Player.Position)[1]+ 20, System.Drawing.Color.Gold, "Ult can kill: "+ hero.ChampionName);
            }
        }

        private static void NotifyPing()
        {
            foreach (
                    var enemy in
                        HeroManager.Enemies.Where(
                            t =>
                                ObjectManager.Player.Spellbook.CanUseSpell(SpellSlot.R) == SpellState.Ready &&
                                t.IsValidTarget() && R.GetDamage(t) > t.Health &&
                                t.Distance(ObjectManager.Player.Position) > Q.Range))
            {
                Ping(enemy.Position.To2D());
            }
        }

        private static void AutoQ()
        {
            if (Q.IsReady())
                foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>())
                {
                    if (enemy.IsValidTarget(Q.Range))
                    {

                        var pred = Q.GetPrediction(enemy);
                        if (pred.Hitchance == HitChance.Immobile)
                        {
                            Q.Cast(enemy);
                        }
                    }
                }

        }

        //Trus Logic
        private static void Farm()
        {
            ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);

            var allMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range);
            if (Q.IsReady())
            {
                foreach (var minion in allMinions.Where(x => Player.GetSpellDamage(x, SpellSlot.Q) >= HealthPrediction.GetHealthPrediction(x, (int)(800))))
                {
                    FarmCast(minion);
                }
            }
        }

        private static void LastHit()
        {

            if (Q.IsReady())
            {
                var minioncout = Q.GetLineFarmLocation(MinionManager.GetMinions(Q.Range));
                if (minioncout.MinionsHit >= 4)
                {
                    var minions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All,MinionTeam.NotAlly);
                    minions.RemoveAll(x => x.MaxHealth <= 5); //filter wards the ghetto method lel

                    foreach (
                        var minion in
                            minions.Where(
                                x =>
                                    ObjectManager.Player.GetSpellDamage(x, SpellSlot.Q, 1) >=
                                    //FirstDamage = multitarget hit, differentiate! (check radius around mob predicted pos)
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

        }

        //Trus Logic
       private static void LaneClear()
        {
            var rangedMinions = MinionManager.GetMinions(
                ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.Ranged);
            var allMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range);
            if (Q.IsReady())
            {
                var rangedLocation = Q.GetCircularFarmLocation(rangedMinions);
                var location = Q.GetCircularFarmLocation(allMinions);

                var bLocation = (location.MinionsHit > rangedLocation.MinionsHit + 1) ? location : rangedLocation;

                if (bLocation.MinionsHit > 0)
                {
                    Q.Cast(bLocation.Position.To3D());
                }
            }
        }

//Trus Logic
        public static Vector3 FindHitPosition(PredictionOutput minion)
        {
            Console.WriteLine("Searching hit position");
            int multihit = 0;
            for (int i = -100; i < 100; i = i + 10)
            {
                for (int a = -100; a < 100; a = a + 10)
                {
                    Vector3 tempposition = new Vector3(minion.UnitPosition.X + i, minion.UnitPosition.Y + a, minion.UnitPosition.Z);
                    multihit = CheckMultiHit(tempposition);
                    if (multihit == 1)
                    {
                        return tempposition;
                    }
                }
            }
                return new Vector3(0,0,0);
        }
//Trus Logic
        static int CheckMultiHit(Vector3 minion)
        {
            var count = 0;
            var allMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range);
            foreach (Obj_AI_Base minionvar in allMinions.Where(x => Vector3.Distance(minion, Prediction.GetPrediction(x, 250f).UnitPosition) < 200))
            {
                    count++;               
            }
            return count;
        }
//Trus Logic
        private static void FarmCast(Obj_AI_Base minion)
        {
            Console.WriteLine("Starting farm check");
            var position = FindHitPosition(Prediction.GetPrediction(minion, 250f));
            if (!(position.X == 0 && position.Y == 0 && position.Z == 0))
            {
                Console.WriteLine("Cast Q: " + position.X + " : " + position.Y + " : " + position.Z);
                Q.Cast(position);
            }
        }

        private static void RegulateEState(bool ignoreTargetChecks = false)
        {
            if (!E.IsReady() || IsInPassiveForm() ||
                ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E).ToggleState != 2)
                return;
            var target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
            var minions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range, MinionTypes.All,
                MinionTeam.NotAlly);

            if (!ignoreTargetChecks && (target != null || (!_comboE && minions.Count != 0)))
                return;
            E.CastOnUnit(ObjectManager.Player);
            _comboE = false;
        }

        private static bool IsInPassiveForm()
        {
            return ObjectManager.Player.IsZombie; //!ObjectManager.Player.IsHPBarRendered;
        }
        //Beaving Logic

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
        //Beaving Logic
        private static void SimplePing()
        {
            Game.ShowPing(PingCategory.Fallback, PingLocation, true);
        }
        //Beaving Logic
        private float ComboDamage(Obj_AI_Hero t)
        {
            return R.GetDamage(t);
        }

        private static void Combo()
        {
			
            var qTarget = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            var wTarget = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Magical);
            var eTarget = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
            var UseQ = _menu.Item("useQ").GetValue<bool>();
            var UseW = _menu.Item("useW").GetValue<bool>();
            var UseE = _menu.Item("useE").GetValue<bool>();
            if (qTarget != null &&  UseQ && Q.IsReady())
            {
				var HC = HitChance.Medium;

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
                else
                {
                    RegulateEState();
                }
            }
            else if (eTarget == null && ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E).ToggleState != 1)
            {
                E.Cast();
            }

            if (wTarget != null && UseW  && W.IsReady())
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
                    case 0:
                        HC = HitChance.Low;
                        break;
                    case 1:
                        HC = HitChance.Medium;
                        break;
                    case 2:
                        HC = HitChance.High;
                        break;
                    case 3:
                        HC = HitChance.VeryHigh;
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
