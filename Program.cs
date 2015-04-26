using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp.Common.Data;
using LeagueSharp;
using LeagueSharp.Common;

namespace Kalista
{
  class Program
	{   
	 const string ChampionName = "Kalista";
        static Obj_AI_Hero Player;
        static Orbwalking.Orbwalker Orbwalker;
        static Menu Config;
        static Spell Q, W, E, R;
        
       public static void Main(string[] args)
	{
	      CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
	}
       public static void Game_OnGameLoad(EventArgs args)
	{
		Player = ObjectManager.Player;
            if (Player.BaseSkinName != ChampionName) return;
            //spell
            Q = new Spell(SpellSlot.Q, 1150);
            Q.SetSkillshot(0.25f, 35, 1700, true, SkillshotType.SkillshotLine);
            W = new Spell(SpellSlot.W, 5200);
            E = new Spell(SpellSlot.E, 1000);
            R = new Spell(SpellSlot.R, 1200);
            
            //menu
            Config = new Menu(ChampionName, ChampionName, true);
            //ts
            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);
            //orbwalk
             var orbwalkermenu = new Menu("Orbwalker", "Orbwalker");
             Orbwalker = new Orbwalking.Orbwalker(orbwalkermenu);
             Config.AddSubMenu(orbwalkermenu);
            //combomenu
             var combomenu = new Menu("Combo", "Combo");
            {
                combomenu.AddItem(new MenuItem("cq", "Use Q").SetValue(true));            
                combomenu.AddItem(new MenuItem("ce", "Use E").SetValue(true));               
            }
             Config.AddSubMenu(combomenu);
            //Harrassmenu
            var harrassmenu = new Menu("Harrass", "Harrass");
            {
                harrassmenu.AddItem(new MenuItem("hq", "UseQ").SetValue(true));
                harrassmenu.AddItem(new MenuItem("harrasmana", "Harrass if Mana >").SetValue(new Slider(60, 100, 0)));
            }
            Config.AddSubMenu(harrassmenu);
            var farmmenu =new Menu("Farm", "Farm");
            {
            	farmmenu.AddItem(new MenuItem("fe", "Use E LaneClear").SetValue(true));
				farmmenu.AddItem(new MenuItem("elaneclear", "E if kill x minions ").SetValue(new Slider(2, 5, 1)));            	
            	farmmenu.AddItem(new MenuItem("fmana", "mana> ").SetValue(new Slider(50, 100, 1)));
            	farmmenu.AddItem(new MenuItem("jq", "UseQJungle").SetValue(false));
            	farmmenu.AddItem(new MenuItem("je", "UseEJungle").SetValue(true));
             }
            Config.AddSubMenu(farmmenu);            
            //Miscmenu
            var miscmenu = new Menu("Misc", "Misc");
            {               
                miscmenu.AddItem(new MenuItem("rsave","Use R to save soul").SetValue(true));
                miscmenu.AddItem(new MenuItem("mobsteal", "Steal Mods").SetValue(true));
                miscmenu.AddItem(new MenuItem("edamereduce", "E dame ruduce").SetValue(new Slider(0, 100, 0)));
                miscmenu.AddItem(new MenuItem("lasthitassist", "Use E To Last Hit").SetValue(true));
            }
            Config.AddSubMenu(miscmenu);            
             var ksmenu = new Menu("KillSteal", "KillSteal");
            {
             ksmenu.AddItem(new MenuItem("ks", "KillSteal").SetValue(true));
             ksmenu.AddItem(new MenuItem("qks", "Use Q").SetValue(true));                        
             ksmenu.AddItem(new MenuItem("eks", "Use E").SetValue(true));
             
            }
            
            Config.AddItem(new MenuItem("debug", "Debug").SetValue(false));
            Config.AddSubMenu(ksmenu);           
            Config.AddToMainMenu();
            Game.OnUpdate += Game_OnUpdate;
            Game.PrintChat("DonguKalista by dongu54321 Loaded");
            Orbwalking.OnNonKillableMinion += Orbwalking_OnNonKillableMinion;
            Obj_AI_Hero.OnProcessSpellCast += Obj_AI_Hero_OnProcessSpellCast;            
            Utility.HpBarDamageIndicator.DamageToUnit = GetEDamage;
            Utility.HpBarDamageIndicator.Enabled = true;          
	}
       static void Orbwalking_OnNonKillableMinion(AttackableUnit minion)
        {
            if (!Config.Item("lasthitassist").GetValue<bool>())
                return;

            if (E.CanCast((Obj_AI_Base)minion) && minion.Health +10 <= GetEDamage((Obj_AI_Base)minion))
            {E.Cast();debug("E last hit helper");}
        }
	  
      static void Obj_AI_Hero_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe && args.SData.Name == "KalistaExpungeWrapper")
                    Utility.DelayAction.Add(200, Orbwalking.ResetAutoAttackTimer);            
        }
		
          
     static void Game_OnUpdate(EventArgs args)
	  {      
			KillSteal();
            switch (Orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo: 
            		Combo();
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    
                    Harass();
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    
                    Laneclear();
                    JungleClear();
                    break;
            }         
            MobSteal();            
	}
	 //Combo
	static void Combo()
        { 				
		var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
		    useItems();
            if (Config.Item("cq").GetValue<bool>())
            {   
            	var pre = Q.GetPrediction(target);
            	if (Q.IsReady() && !Player.IsDashing() &&  pre.Hitchance >= HitChance.VeryHigh )
            		Q.Cast(pre.CastPosition, true);
            }
            if (Config.Item("ce").GetValue<bool>()  )            	
            { 
                if (!target.HasBuffOfType(BuffType.Invulnerability) && !target.HasBuff("shenstandunitedshield", true))
                    {
                	if ((target.Health  + target.HPRegenRate/2) <= GetEDamage(target) && E.IsReady() && target.IsValidTarget(E.Range))
                	{debug( GetEDamage(target)+"damage Ecombo kill target: "+ target.Health+"/"+target.MaxHealth);E.Cast();}
                    }
                else if (!Orbwalker.InAutoAttackRange(target) && target.HasBuff("KalistaExpungeMarker"))
                {
                	var combomionion = MinionManager.GetMinions(Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.Enemy).Where(x => x.Health <= GetEDamage(x) && E.IsReady() && E.IsInRange(x));
                	if (E.IsReady()) E.Cast();debug("combo E target out of range");
                }
            }            
        }
        //Harrass
		 static void Harass()
        {
	    if (Player.ManaPercent <  Config.Item("harrasmana").GetValue<Slider>().Value)
                return;

            if (Config.Item("hq").GetValue<bool>())
            {
                var Qtarget = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical, true);
                if (Q.CanCast(Qtarget)  && !Player.IsDashing() && !Player.IsWindingUp)
                	Q.CastIfHitchanceEquals(Qtarget, HitChance.VeryHigh);
            }
        }
 	static void Laneclear()
        {  
            if (Player.ManaPercent < Config.Item("fmana").GetValue<Slider>().Value)
                return;
            var Minions = MinionManager.GetMinions(Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.Enemy);
            if (Minions.Count <= 0)
                return;
            if (Config.Item("fe").GetValue<bool>())
            {   
            	var z = Config.Item("elaneclear").GetValue<Slider>().Value;
                var minionkillcount = 0;
                foreach (var Minion in Minions.Where(x => E.CanCast(x) && x.Health <= GetEDamage(x))){minionkillcount++;}
                if (minionkillcount >= z)
                {E.Cast();debug("E LaneClear");}
                if (Player.ManaPercent <  Config.Item("harrasmana").GetValue<Slider>().Value) return;
                var t = TargetSelector.GetTarget(E.Range,TargetSelector.DamageType.Physical,true);
                if (t.HasBuff("KalistaExpungeMarker") && minionkillcount >= 1) {E.Cast(); debug("Harras e with minion kill");}
                if (Q.CanCast(t)  && !Player.IsDashing() && !Player.IsWindingUp)
                	Q.CastIfHitchanceEquals(t, HitChance.VeryHigh);
            }
            
            
            
        }
       static void JungleClear()
       {  
	   var mobs = MinionManager.GetMinions(Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.Health);            
            if (mobs.Count == 0)
                return;
            var mob = mobs.First();            
            if ( Config.Item("jq").GetValue<bool>()&& Q.IsReady() && mob.IsValidTarget(Q.Range))
            {
                Q.Cast(mob);
            }

            if ( E.IsReady() && GetEDamage(mob)>mob.Health + mob.HPRegenRate/2)
            { if (E.CanCast(mob))
            	{E.Cast();debug("E JungleClear");}
            }
        
	}
        static void MobSteal()
        {   
        	var a = Config.Item("edamereduce").GetValue<Slider>().Value;
        	if (Config.Item("mobsteal").GetValue<bool>() && 
        		
        	    (ObjectManager.Get<Obj_AI_Minion>().Any(m => m.IsValidTarget(E.Range) && (m.BaseSkinName.Contains("MinionSiege") || m.BaseSkinName.Contains("Dragon") || m.BaseSkinName.Contains("Baron")|| m.BaseSkinName.Contains("SRU_Blue")|| m.BaseSkinName.Contains("SRU_Red")) && m.Health+10 +a<GetEDamage(m))))
        	    {   if (E.IsReady())
        		     {E.Cast();debug("E MobSteal Big Monster And MinionSiege");}
                   
                }
        }
        static void useItems()
        {   var target = TargetSelector.GetTarget(450,TargetSelector.DamageType.Physical,true);
        	if (target != null && target.Type == ObjectManager.Player.Type &&
                    target.ServerPosition.Distance(ObjectManager.Player.ServerPosition) < 450)
                {
                    var hasCutGlass = Items.HasItem(3144);
                    var hasBotrk = Items.HasItem(3153);

                    if (hasBotrk || hasCutGlass)
                    {
                        var itemId = hasCutGlass ? 3144 : 3153;
                        var damage = ObjectManager.Player.GetItemDamage(target, Damage.DamageItems.Botrk);
                        if (hasCutGlass || ObjectManager.Player.Health + damage < ObjectManager.Player.MaxHealth)
                            Items.UseItem(itemId, target);
                    }
                }
            var ghost = ItemData.Youmuus_Ghostblade.GetItem();           
    

            if (ghost.IsReady() && ghost.IsOwned(Player) && target.IsValidTarget(Q.Range))
            {
            	ghost.Cast();            
            }
           
        }
        static void KillSteal()
        {
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsValidTarget(Q.Range)))        			                    
        	{   
				            	
            	var qdamage = Player.GetSpellDamage(enemy, SpellSlot.Q);
            	 var edamage = GetEDamage(enemy);
            	if  (!Config.Item("ks").GetValue<bool>() || (enemy.IsInvulnerable)||enemy.HasBuff("deathdefiedbuff"))
        			return;	 
                //E
               if ( E.IsReady() &&  Config.Item("eks").GetValue<bool>() && edamage >= (enemy.Health+enemy.HPRegenRate/3) && !enemy.HasBuffOfType(BuffType.Invulnerability)  && !enemy.HasBuff("shenstandunitedshield", true))
                {
                	if ( enemy.IsValidTarget(E.Range) )
                	    
                	  {debug(GetEDamage(enemy) +"damage Eks kill target:"+ enemy.Health+" Hp,enemy Armor:"+enemy.Armor);E.Cast();}
                	else if ( Player.Distance(enemy,true) > E.Range )
                	{                                 
                		var t = TargetSelector.GetTarget(E.Range,TargetSelector.DamageType.Physical);                
                		if (t.IsValidTarget(E.Range) &&  t.HasBuff("KalistaExpungeMarker") &&  E.IsReady() && E.IsInRange(t)) {E.Cast();break;}
                		 var Minions = MinionManager.GetMinions(Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.Enemy);
                		 foreach (var Minion in Minions.Where(x => E.CanCast(x) && x.HasBuff("KalistaExpungeMarker"))){E.Cast(); debug("E Ks enemy out of E.Range");}
                		                		
                	}
                }
                //Q
				if ( Config.Item("qks").GetValue<bool>() && Q.IsReady())
				{
					if (  qdamage > enemy.Health+enemy.HPRegenRate/2)
					{Q.CastIfHitchanceEquals(enemy, HitChance.High);debug("Q ks");}
				}               
          	}
        }
        public static float GetEDamage(Obj_AI_Base target)
        {  var buff = target.Buffs.Find(b => b.Caster.IsMe && b.IsValidBuff() && b.DisplayName == "KalistaExpungeMarker");;
        	if (buff!=null && E.IsReady())
        	{
            var a = Config.Item("edamereduce").GetValue<Slider>().Value;        		
            double armorPenPercent = Player.PercentArmorPenetrationMod;
            double armorPenFlat = Player.FlatArmorPenetrationMod;
            double k;
            double damage = 0f;
			var armor = target.Armor; 
			if (armor < 0) {k = 2 - 100 / (100 - armor);}
            else if ((target.Armor * armorPenPercent) - armorPenFlat < 0) k = 1;
            else  {k = 100 / (100 + (target.Armor * armorPenPercent) - armorPenFlat);}
            if (Player.Masteries.Any(m => m.Page == MasteryPage.Offense && m.Id == 65 && m.Points == 1)) k= k*1.015;
            if (Player.Masteries.Any(m => m.Page == MasteryPage.Offense && m.Id == 146 && m.Points == 1)) k=k*1.03;                         
            		
			damage += new double[] {20, 30, 40, 50, 60}[E.Level -1] + Player.TotalAttackDamage*0.6f + (  new double[] {10, 14, 19, 25, 32 }[E.Level -1]+ new double[]{0.2f, 0.225f, 0.25f, 0.275f, 0.3f }[E.Level-1] *  Player.TotalAttackDamage) * (buff.Count-1);          
			return (float) (damage*k-a);
        	}
        	return 1;
        }
          
        public static void debug(string msg)
        {
            if (Config.Item("debug").GetValue<bool>())
                Game.PrintChat(msg);
        }              
               
    }	  
	
}	
