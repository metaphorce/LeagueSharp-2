﻿using System;
using System.Drawing;
using System.Linq;
using BrianSharp.Common;
using LeagueSharp;
using LeagueSharp.Common;
using Orbwalk = BrianSharp.Common.Orbwalker;

namespace BrianSharp.Plugin
{
    internal class Maokai : Helper
    {
        public Maokai()
        {
            Q = new Spell(SpellSlot.Q, 600, TargetSelector.DamageType.Magical);
            W = new Spell(SpellSlot.W, 525, TargetSelector.DamageType.Magical);
            E = new Spell(SpellSlot.E, 1100, TargetSelector.DamageType.Magical);
            R = new Spell(SpellSlot.R, 475, TargetSelector.DamageType.Magical);
            Q.SetSkillshot(0.3333f, 110, 1100, false, SkillshotType.SkillshotLine);
            E.SetSkillshot(0.25f, 225, 1750, false, SkillshotType.SkillshotCircle);

            var champMenu = new Menu("Plugin", Player.ChampionName + "_Plugin");
            {
                var comboMenu = new Menu("Combo", "Combo");
                {
                    AddItem(comboMenu, "Q", "Use Q");
                    AddItem(comboMenu, "W", "Use W");
                    AddItem(comboMenu, "E", "Use E");
                    AddItem(comboMenu, "R", "Use R");
                    AddItem(comboMenu, "RHpU", "-> If Enemy Hp Under", 60);
                    AddItem(comboMenu, "RCountA", "-> If Enemy Above", 2, 1, 5);
                    AddItem(comboMenu, "RKill", "-> Cancel When Killable");
                    AddItem(comboMenu, "RKillCountA", "--> If Can Kill Above", 2, 1, 5);
                    AddItem(comboMenu, "RMpU", "-> Cancel If Mp Under", 20);
                    champMenu.AddSubMenu(comboMenu);
                }
                var harassMenu = new Menu("Harass", "Harass");
                {
                    AddItem(harassMenu, "AutoQ", "Auto Q", "H", KeyBindType.Toggle);
                    AddItem(harassMenu, "AutoQMpA", "-> If Mp Above", 50);
                    AddItem(harassMenu, "Q", "Use Q");
                    AddItem(harassMenu, "W", "Use W");
                    AddItem(harassMenu, "WHpA", "-> If Hp Above", 20);
                    AddItem(harassMenu, "E", "Use E");
                    champMenu.AddSubMenu(harassMenu);
                }
                var clearMenu = new Menu("Clear", "Clear");
                {
                    AddSmiteMobMenu(clearMenu);
                    AddItem(clearMenu, "Q", "Use Q");
                    AddItem(clearMenu, "W", "Use W");
                    AddItem(clearMenu, "E", "Use E");
                    champMenu.AddSubMenu(clearMenu);
                }
                var fleeMenu = new Menu("Flee", "Flee");
                {
                    AddItem(fleeMenu, "W", "Use W");
                    AddItem(fleeMenu, "Q", "Use Q To Slow Enemy");
                    champMenu.AddSubMenu(fleeMenu);
                }
                var miscMenu = new Menu("Misc", "Misc");
                {
                    var killStealMenu = new Menu("Kill Steal", "KillSteal");
                    {
                        AddItem(killStealMenu, "Q", "Use Q");
                        AddItem(killStealMenu, "W", "Use W");
                        AddItem(killStealMenu, "Ignite", "Use Ignite");
                        AddItem(killStealMenu, "Smite", "Use Smite");
                        miscMenu.AddSubMenu(killStealMenu);
                    }
                    var antiGapMenu = new Menu("Anti Gap Closer", "AntiGap");
                    {
                        AddItem(antiGapMenu, "Q", "Use Q");
                        AddItem(antiGapMenu, "QSlow", "-> Slow If Cant Knockback (Skillshot)");
                        foreach (var spell in
                            AntiGapcloser.Spells.Where(
                                i => HeroManager.Enemies.Any(a => i.ChampionName == a.ChampionName)))
                        {
                            AddItem(
                                antiGapMenu, spell.ChampionName + "_" + spell.Slot,
                                "-> Skill " + spell.Slot + " Of " + spell.ChampionName);
                        }
                        miscMenu.AddSubMenu(antiGapMenu);
                    }
                    var interruptMenu = new Menu("Interrupt", "Interrupt");
                    {
                        AddItem(interruptMenu, "Q", "Use Q");
                        foreach (var spell in
                            Interrupter.Spells.Where(
                                i => HeroManager.Enemies.Any(a => i.ChampionName == a.ChampionName)))
                        {
                            AddItem(
                                interruptMenu, spell.ChampionName + "_" + spell.Slot,
                                "-> Skill " + spell.Slot + " Of " + spell.ChampionName);
                        }
                        miscMenu.AddSubMenu(interruptMenu);
                    }
                    AddItem(miscMenu, "Gank", "Gank", "Z");
                    AddItem(miscMenu, "WTower", "Auto W If Enemy Under Tower");
                    champMenu.AddSubMenu(miscMenu);
                }
                var drawMenu = new Menu("Draw", "Draw");
                {
                    AddItem(drawMenu, "Q", "Q Range", false);
                    AddItem(drawMenu, "W", "W Range", false);
                    AddItem(drawMenu, "E", "E Range", false);
                    AddItem(drawMenu, "R", "R Range", false);
                    champMenu.AddSubMenu(drawMenu);
                }
                MainMenu.AddSubMenu(champMenu);
            }
            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += OnDraw;
            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
            Interrupter.OnPossibleToInterrupt += OnPossibleToInterrupt;
        }

        private void OnUpdate(EventArgs args)
        {
            if (Player.IsDead || MenuGUI.IsChatOpen || Player.IsRecalling())
            {
                return;
            }
            switch (Orbwalk.CurrentMode)
            {
                case Orbwalker.Mode.Combo:
                    Fight("Combo");
                    break;
                case Orbwalker.Mode.Harass:
                    Fight("Harass");
                    break;
                case Orbwalker.Mode.Clear:
                    Clear();
                    break;
                case Orbwalker.Mode.Flee:
                    Flee();
                    break;
            }
            if (GetValue<KeyBind>("Misc", "Gank").Active)
            {
                Fight("Gank");
            }
            AutoQ();
            KillSteal();
            AutoWUnderTower();
        }

        private void OnDraw(EventArgs args)
        {
            if (Player.IsDead)
            {
                return;
            }
            if (GetValue<bool>("Draw", "Q") && Q.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, Q.Range, Q.IsReady() ? Color.Green : Color.Red);
            }
            if (GetValue<bool>("Draw", "W") && W.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, W.Range, W.IsReady() ? Color.Green : Color.Red);
            }
            if (GetValue<bool>("Draw", "E") && E.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, E.Range, E.IsReady() ? Color.Green : Color.Red);
            }
            if (GetValue<bool>("Draw", "R") && R.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, R.Range, R.IsReady() ? Color.Green : Color.Red);
            }
        }

        private void OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (Player.IsDead || !GetValue<bool>("AntiGap", "Q") ||
                !GetValue<bool>("AntiGap", gapcloser.Sender.ChampionName + "_" + gapcloser.Slot) ||
                !Q.CanCast(gapcloser.Sender))
            {
                return;
            }
            if (Player.Distance(gapcloser.Sender) <= 100)
            {
                Q.Cast(gapcloser.Sender.ServerPosition, PacketCast);
            }
            else if (GetValue<bool>("AntiGap", "QSlow") && gapcloser.SkillType == GapcloserType.Skillshot &&
                     Player.Distance(gapcloser.End) > 100)
            {
                Q.CastIfHitchanceEquals(gapcloser.Sender, HitChance.High, PacketCast);
            }
        }

        private void OnPossibleToInterrupt(Obj_AI_Hero unit, InterruptableSpell spell)
        {
            if (Player.IsDead || !GetValue<bool>("Interrupt", "Q") ||
                !GetValue<bool>("Interrupt", unit.ChampionName + "_" + spell.Slot) || !Q.IsReady())
            {
                return;
            }
            if (Player.Distance(unit) > 100 && W.CanCast(unit) &&
                Player.Mana >= Q.Instance.ManaCost + W.Instance.ManaCost && W.CastOnUnit(unit, PacketCast))
            {
                return;
            }
            if (Player.Distance(unit) <= 100)
            {
                Q.Cast(unit.ServerPosition, PacketCast);
            }
        }

        private void Fight(string mode)
        {
            if (mode == "Combo" && GetValue<bool>(mode, "R") && R.IsReady())
            {
                var obj = HeroManager.Enemies.Where(i => i.IsValidTarget(R.Range)).ToList();
                if (!Player.HasBuff("MaokaiDrain"))
                {
                    if (Player.ManaPercent >= GetValue<Slider>(mode, "RMpU").Value &&
                        (GetValue<Slider>(mode, "RCountA").Value > 1
                            ? (obj.Count > 1 &&
                               obj.Any(i => i.HealthPercentage() < GetValue<Slider>(mode, "RHpU").Value)) ||
                              obj.Count >= GetValue<Slider>(mode, "RCountA").Value
                            : obj.Any(i => i.HealthPercentage() < GetValue<Slider>(mode, "RHpU").Value)) &&
                        R.Cast(PacketCast))
                    {
                        return;
                    }
                }
                else
                {
                    if (GetValue<bool>(mode, "RKill") &&
                        obj.Count(i => CanKill(i, GetRDmg(i))) >= GetValue<Slider>(mode, "RKillCountA").Value &&
                        R.Cast(PacketCast))
                    {
                        return;
                    }
                    if (Player.ManaPercent < GetValue<Slider>(mode, "RMpU").Value && R.Cast(PacketCast))
                    {
                        return;
                    }
                }
            }
            if (mode == "Gank")
            {
                var target = W.GetTarget(100);
                CustomOrbwalk(target);
                if (target == null)
                {
                    return;
                }
                if (W.IsReady())
                {
                    if (E.IsReady() && E.CastIfWillHit(target, -1, PacketCast))
                    {
                        return;
                    }
                    W.CastOnUnit(target, PacketCast);
                }
                else if (Utils.TickCount - W.LastCastAttemptT <= 1500 && Q.IsReady())
                {
                    Q.CastIfHitchanceEquals(target, HitChance.High, PacketCast);
                }
            }
            else
            {
                if (GetValue<bool>(mode, "E") && E.CastOnBestTarget(E.Width, PacketCast, true).IsCasted())
                {
                    return;
                }
                if (GetValue<bool>(mode, "W") &&
                    (mode == "Combo" || Player.HealthPercentage() >= GetValue<Slider>(mode, "WHpA").Value) &&
                    W.CastOnBestTarget(0, PacketCast).IsCasted())
                {
                    return;
                }
                if (GetValue<bool>(mode, "Q") && Q.IsReady())
                {
                    Q.CastOnBestTarget(0, PacketCast);
                }
            }
        }

        private void Clear()
        {
            SmiteMob();
            var minionObj = MinionManager.GetMinions(
                E.Range + E.Width, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
            if (!minionObj.Any())
            {
                return;
            }
            if (GetValue<bool>("Clear", "E") && E.IsReady() &&
                (minionObj.Count > 2 || minionObj.Any(i => i.MaxHealth >= 1200)))
            {
                var pos = E.GetCircularFarmLocation(minionObj);
                if (pos.MinionsHit > 0 && E.Cast(pos.Position, PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>("Clear", "Q") && Q.IsReady())
            {
                var pos = Q.GetLineFarmLocation(minionObj.Where(i => Q.IsInRange(i)).ToList());
                if (pos.MinionsHit > 0 && Q.Cast(pos.Position, PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>("Clear", "W") && W.IsReady())
            {
                var obj = minionObj.Where(i => W.IsInRange(i)).FirstOrDefault(i => i.MaxHealth >= 1200);
                if (obj == null && !minionObj.Any(i => Orbwalk.InAutoAttackRange(i, 40)))
                {
                    obj = minionObj.Where(i => W.IsInRange(i)).MinOrDefault(i => i.Health);
                }
                if (obj != null)
                {
                    W.CastOnUnit(obj, PacketCast);
                }
            }
        }

        private void Flee()
        {
            if (GetValue<bool>("Flee", "W") && W.IsReady())
            {
                var obj =
                    HeroManager.Enemies.Where(i => i.IsValidTarget(W.Range) && i.Distance(Game.CursorPos) < 200)
                        .MinOrDefault(i => i.Distance(Game.CursorPos)) ??
                    MinionManager.GetMinions(W.Range, MinionTypes.All, MinionTeam.NotAlly)
                        .Where(i => i.Distance(Game.CursorPos) < 200)
                        .MinOrDefault(i => i.Distance(Game.CursorPos));
                if (obj != null && W.CastOnUnit(obj, PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>("Flee", "Q") && Q.IsReady())
            {
                Q.CastOnBestTarget(0, PacketCast);
            }
        }

        private void AutoQ()
        {
            if (!GetValue<KeyBind>("Harass", "AutoQ").Active ||
                Player.ManaPercent < GetValue<Slider>("Harass", "AutoQMpA").Value || !Q.IsReady())
            {
                return;
            }
            Q.CastOnBestTarget(0, PacketCast);
        }

        private void KillSteal()
        {
            if (GetValue<bool>("KillSteal", "Ignite") && Ignite.IsReady())
            {
                var target = TargetSelector.GetTarget(600, TargetSelector.DamageType.True);
                if (target != null && CastIgnite(target))
                {
                    return;
                }
            }
            if (GetValue<bool>("KillSteal", "Smite") &&
                (CurrentSmiteType == SmiteType.Blue || CurrentSmiteType == SmiteType.Red))
            {
                var target = TargetSelector.GetTarget(760, TargetSelector.DamageType.True);
                if (target != null && CastSmite(target))
                {
                    return;
                }
            }
            if (GetValue<bool>("KillSteal", "Q") && Q.IsReady())
            {
                var target = Q.GetTarget();
                if (target != null && Q.IsKillable(target) &&
                    Q.CastIfHitchanceEquals(target, HitChance.High, PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>("KillSteal", "W") && W.IsReady())
            {
                var target = W.GetTarget();
                if (target != null && W.IsKillable(target))
                {
                    W.CastOnUnit(target, PacketCast);
                }
            }
        }

        private void AutoWUnderTower()
        {
            if (!GetValue<bool>("Misc", "WTower") || !W.IsReady())
            {
                return;
            }
            var target = HeroManager.Enemies.Where(i => i.IsValidTarget(W.Range)).MinOrDefault(i => i.Distance(Player));
            var tower =
                ObjectManager.Get<Obj_AI_Turret>()
                    .FirstOrDefault(i => i.IsAlly && !i.IsDead && i.Distance(Player) <= 850);
            if (target != null && tower != null && target.Distance(tower) <= 850)
            {
                W.CastOnUnit(target, PacketCast);
            }
        }

        private double GetRDmg(Obj_AI_Hero target)
        {
            return Player.CalcDamage(
                target, Damage.DamageType.Magical,
                new double[] { 100, 150, 200 }[R.Level - 1] + 0.5 * Player.FlatMagicDamageMod + R.Instance.Ammo);
        }
    }
}