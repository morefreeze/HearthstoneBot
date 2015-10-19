using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System;
using UnityEngine;

using LuaInterface;

namespace HearthstoneBot
{
    public class API
    {
        private Lua lua;

        public void __csharp_print_to_log(string str)
        {
            Log.log(str, "LuaOutput.log");
        }

        public API()
        {   
            Log.log("API constructed");

            try
            {
                lua = new Lua();

                Log.log("Setting script path...");
                string lua_script_path = Plugin.getBothPath() + "LuaScripts/";
                lua["script_path"] = lua_script_path;
                Log.log("Done");
                
                // Print to screen
                lua.RegisterFunction("__csharp_print_to_log", this, typeof(API).GetMethod("__csharp_print_to_log"));

                // Game functions
                // Query for cards
                lua.RegisterFunction("__csharp_cards", this, typeof(API).GetMethod("getCards"));
                lua.RegisterFunction("__csharp_card", this, typeof(API).GetMethod("getCard"));
                // Query the number of crystals
                lua.RegisterFunction("__csharp_crystals", this, typeof(API).GetMethod("getCrystals"));

                // Query about entities
                lua.RegisterFunction("__csharp_entity_bool", this, typeof(API).GetMethod("getEntityBool"));
                lua.RegisterFunction("__csharp_entity_value", this, typeof(API).GetMethod("getEntityValue"));

                // Utility functions
                lua.RegisterFunction("__csharp_drop_card", this, typeof(API).GetMethod("drop_card"));
                lua.RegisterFunction("__csharp_convert_to_entity", this, typeof(API).GetMethod("__csharp_convert_to_entity"));
                lua.RegisterFunction("__csharp_do_attack", this, typeof(API).GetMethod("attack"));
                
                Log.log("Loading Main.lua...");
                lua.DoFile(lua_script_path + "Main.lua");
                Log.log("Done");
            }
            catch(LuaException e)
            {
                Log.error("EXCEPTION");
                Log.error(e.ToString());
                Log.error(e.Message);
            }
            catch(Exception e)
            {
                Log.error(e.ToString());
            }
            Log.log("Scripts loaded constructed");
        }

        private LuaTable CreateTable()
        {
            return (LuaTable) lua.DoString("return {}")[0];
        }

        private LuaTable CardListToTable(List<Card> list)
        {
            LuaTable tab = CreateTable();
            int i = 1;
            foreach(Card current in list)
            {
                tab[i] = current;
                i++;
            }
            return tab;
        }

        private List<Card> TableToCardList(LuaTable tab)
        {
            List<Card> list = new List<Card>();
            
            for(int i = 1; ; i++)
            {
                Card c = tab[i] as Card;
                if(c == null)
                {
                    break;
                }
                list.Add(c);
            }

            return list;
        }

        public LuaTable getCards(string where)
        {
            List<Card> list = null;
            switch(where)
            {
                case "HAND":
                    list = getOurPlayer().GetHandZone().GetCards().ToList<Card>();
                    break;

                case "OUR_BATTLEFIELD":
                    list = getOurPlayer().GetBattlefieldZone().GetCards().ToList<Card>();
                    break;

                case "ENEMY_BATTLEFIELD":
                    list = getEnemyPlayer().GetBattlefieldZone().GetCards().ToList<Card>();
                    break;

                default:
                    Log.error("getCards: Unknown area requested = " + where);
                    // Return an empty table
                    return CreateTable();
            }
            return CardListToTable(list);
        }

        public Card getCard(string which)
        {
            switch(which)
            {
                case "ENEMY_HERO":
                    return getEnemyPlayer().GetHeroCard();

                case "OUR_HERO":
                    return getOurPlayer().GetHeroCard();

                case "HERO_POWER":
                    return getOurPlayer().GetHeroPowerCard();

                default:
                    Log.error("getCard: Unknown card requested = " + which);
                    // Return nothing
                    return null;
            }
        }

        public int getCrystals(string whos)
        {
            switch(whos)
            {
                case "ENEMY_HERO":
                    return getEnemyPlayer().GetNumAvailableResources();

                case "OUR_HERO":
                    return getOurPlayer().GetNumAvailableResources();

                default:
                    Log.error("getCrystals: Unknown who requested = " + whos);
                    // Return nothing
                    return 0;
            }
        }

        public bool getEntityBool(Entity entity, string which)
        {
            if(entity == null)
            {
                Log.error("getEntityBool called with entity = null");
                return false;
            }
            if(which == null)
            {
                Log.error("getEntityBool called with which = null");
                return false;
            }

            // GetOriginalCharge(), HasCharge(), HasBattlecry(), CanBeTargetedByAbilities(), CanBeTargetedByHeroPowers(),
            // IsImmune(), IsPoisonous(), IsEnraged(), IsFreeze(), IsFrozen(), IsAsleep(), IsStealthed(), HasTaunt(),
            // HasDivineShield(), IsHero(), IsHeroPower(), IsMinion(), IsSpell(), IsAbility(), IsWeapon(), IsElite(),
            // IsExhausted(), IsSecret(), CanAttack(), CanBeAttacked(), CanBeTargetedByOpponents(), HasSpellPower(),
            // IsAffectedBySpellPower(), HasSpellPowerDouble(), IsDamaged(), HasWindfury(), HasCombo(), HasRecall(),
            // HasDeathrattle(), IsSilenced(), CanBeDamaged()
            MethodInfo dynMethod = entity.GetType().GetMethod(which, BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.Public);
            /*
            if(dynMethod == null)
            {
                Log.error("getEntityBool: Unknown function requested = " + which);
                return false;
            }
            */
            try
            {
                return (bool) dynMethod.Invoke(entity, new object[]{});
            }
            catch(Exception e)
            {
                Log.error("Exception in getEntityBool:");
                Log.error(e.ToString());
                return false;
            }
        }

        public int getEntityValue(Entity entity, string which)
        {
            if(entity == null)
            {
                Log.error("getEntityValue called with entity = null");
                return -1;
            }
            if(which == null)
            {
                Log.error("getEntityValue called with which = null");
                return -1;
            }
            // GetOriginalCost(), GetOriginalATK(), GetOriginalHealth(), GetOriginalDurability(),
            // GetDamage(), GetNumTurnsInPlay(), GetNumAttacksThisTurn(), GetSpellPower(),
            // GetCost(), GetATK(), GetDurability(), GetZonePosition(), GetArmor(), GetFatigue(),
            // GetHealth(), GetRemainingHP()
            MethodInfo dynMethod = entity.GetType().GetMethod(which, BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.Public);
            /* // TODO: Figure out why mono cannot generate code correctly
            if(dynMethod == null)
            {
                Log.error("getEntityValue: Unknown function requested = " + which);
                return 0;
            }
            */
            try
            {
                return (int) dynMethod.Invoke(entity, new object[]{});
            }
            catch(Exception e)
            {
                Log.error("Exception in getEntityValue:");
                Log.error(e.ToString());
                return -1;
            }
        }

        public bool was_critical_pause_requested()
        {
            bool critical_puase_requested = (bool) lua["__critical_pause"];
            lua["__critical_pause"] = false;
            return critical_puase_requested;
        }

        public bool was_end_turn_requested()
        {
            bool end_turn_requested = (bool) lua["__end_turn"];
            lua["__end_turn"] = false;
            return end_turn_requested;
        }

        public void run()
        {
            try
            {
                LuaFunction f = lua.GetFunction("turn_start");
                if(f == null)
                {
                    Log.error("Lua function not found!");
                    return;
                }
                object[] args = f.Call();
                string error = (string) args[0];
                if(error != null)
                {
                    Log.error("Internal Lua Exception");
                    Log.error(error);
                }
            }
            catch(LuaException e)
            {
                Log.error("EXCEPTION");
                Log.error(e.ToString());
                Log.error(e.Message);
            }
            catch(Exception e)
            {
                Log.error(e.ToString());
            }
        }

        public List<Card> mulligan(List<Card> cards)
        {
            try
            {
                LuaFunction f = lua.GetFunction("mulligan");
                if(f == null)
                {
                    Log.log("Lua function not found!");
                    return null;
                }
                LuaTable argument = CardListToTable(cards);
                object[] args = f.Call(argument);
                LuaTable replace = args[0] as LuaTable;
                if(replace != null)
                {
                    List<Card> replace_list = TableToCardList(replace);
                    return replace_list;
                }
                Log.log("NO VALID RETURN TYPE");
            }
            catch(LuaException e)
            {
                Log.error("EXCEPTION");
                Log.error(e.ToString());
                Log.error(e.Message);
            }
            catch(Exception e)
            {
                Log.error(e.ToString());
            }
            return null;
        }

        public Entity __csharp_convert_to_entity(Card c)
        {
            return c.GetEntity();
        }

        public static Player getOurPlayer()
        {
            return GameState.Get().GetFriendlySidePlayer();
        }

        public static Player getEnemyPlayer()
        {
            return GameState.Get().GetFirstOpponentPlayer(getOurPlayer());
        }

		public void attack(Card c)
		{
			Log.log("Attack: " + c.GetEntity().GetName());
            
            PrivateHacker.HandleClickOnCardInBattlefield(c);
		}

        public bool drop_held_card(int requested_zone_position = 1) 
        {
            try
            {
                return drop_held_card_worker(requested_zone_position);
            }
            catch(Exception e)
            {
                Log.error("Exception within drop_held_card_worker");
                Log.error(e.ToString());
            }
            return false;
        }

        public bool drop_held_card_worker(int requested_zone_position)
        {
            PegCursor.Get().SetMode(PegCursor.Mode.STOPDRAG);

            InputManager input_man = InputManager.Get();
            if (input_man.GetHeldCard() == null)
            {
                Log.log("Nothing held, when trying to drop");
                return false;
            }
            Card component = input_man.GetHeldCard();

            ZonePlay m_myPlayZone = PrivateHacker.get_m_myPlayZone();
            ZoneHand m_myHandZone = PrivateHacker.get_m_myHandZone();
            
            component.SetDoNotSort(false);
            //iTween.Stop(input_man.heldObject);
            Entity entity = component.GetEntity();
            component.NotifyLeftPlayfield();
            GameState.Get().GetGameEntity().NotifyOfCardDropped(entity);
            //m_myPlayZone.UnHighlightBattlefield();
            DragCardSoundEffects component2 = component.GetComponent<DragCardSoundEffects>();
            if (component2)
            {
                component2.Disable();
            }
            //UnityEngine.Object.Destroy(input_man.heldObject.GetComponent<DragRotator>());
            //input_man.heldObject = null;
            ProjectedShadow componentInChildren = component.GetActor().GetComponentInChildren<ProjectedShadow>();
            if (componentInChildren != null)
            {
                componentInChildren.DisableShadow();
            }
            
            // Check that the card is on the hand
            Zone card_zone = component.GetZone();
            if ((card_zone == null) || card_zone.m_ServerTag != TAG_ZONE.HAND)
            {
                return false;
            }
            
            bool does_target = false;

            bool is_minion = entity.IsMinion();
            bool is_weapon = entity.IsWeapon();

            if (is_minion || is_weapon)
            {
                Zone zone = (!is_weapon) ? (Zone) m_myPlayZone : (Zone) PrivateHacker.get_m_myWeaponZone();
                if (zone)
                {
                    GameState gameState = GameState.Get();
                    int card_position = Network.NoPosition;
                    if (is_minion)
                    {
                        card_position = ZoneMgr.Get().PredictZonePosition(zone, requested_zone_position);
                        gameState.SetSelectedOptionPosition(card_position);
                    }
                    if (input_man.DoNetworkResponse(entity))
                    {
                        if (is_weapon)
                        {
                            PrivateHacker.set_m_lastZoneChangeList(ZoneMgr.Get().AddLocalZoneChange(component, zone, zone.GetLastPos()));
                        }
                        else
                        {
                            PrivateHacker.set_m_lastZoneChangeList(ZoneMgr.Get().AddPredictedLocalZoneChange(component, zone, requested_zone_position, card_position));
                        }
                        PrivateHacker.ForceManaUpdate(entity);
                        if (is_minion && gameState.EntityHasTargets(entity))
                        {
                            does_target = true;
                            if (TargetReticleManager.Get())
                            {
                                bool showArrow = true;
                                TargetReticleManager.Get().CreateFriendlyTargetArrow(entity, entity, true, showArrow, null);
                            }
                            PrivateHacker.set_m_battlecrySourceCard(component);
                        }
                    }
                    else
                    {
                        gameState.SetSelectedOptionPosition(Network.NoPosition);
                    }
                }
            }
            // Spell support
            else
            {
                if (entity.IsSpell())
                {
                    if (GameState.Get().EntityHasTargets(entity))
                    {
                        //input_man.heldObject = null;
                        RemoteActionHandler.Get().NotifyOpponentOfCardDropped();
                        m_myHandZone.UpdateLayout(-1, true);
                        m_myPlayZone.SortWithSpotForHeldCard(-1);

                        return true;
                    }
                    if (!GameState.Get().HasResponse(entity))
                    {
                        PlayErrors.DisplayPlayError(PlayErrors.GetPlayEntityError(entity), entity);
                    }
                    else
                    {
                        input_man.DoNetworkResponse(entity);
                        if (entity.IsSecret())
                        {
                            ZoneSecret m_mySecretZone = PrivateHacker.get_m_mySecretZone();
                            PrivateHacker.set_m_lastZoneChangeList(ZoneMgr.Get().AddLocalZoneChange(component, m_mySecretZone, m_mySecretZone.GetLastPos()));
                        }
                        else
                        {
                            PrivateHacker.set_m_lastZoneChangeList(ZoneMgr.Get().AddLocalZoneChange(component, TAG_ZONE.PLAY));
                        }
                        PrivateHacker.ForceManaUpdate(entity);
                        PrivateHacker.PlayPowerUpSpell(component);
                        PrivateHacker.PlayPlaySpell(component);
                    }
                }
            }
            m_myHandZone.UpdateLayout(-1, true);
            m_myPlayZone.SortWithSpotForHeldCard(-1);
            if (does_target)
            {
                if (RemoteActionHandler.Get())
                {
                    RemoteActionHandler.Get().NotifyOpponentOfTargetModeBegin(component);
                }
            }
            else
            {
                if (GameState.Get().GetResponseMode() != GameState.ResponseMode.SUB_OPTION)
                {
                    RemoteActionHandler.Get().NotifyOpponentOfCardDropped();
                }
            }
            return true;
        }

		public bool drop_card(Card c, bool pickup)
		{
			Log.log("Dropped card: " + c.GetEntity().GetName());

            if (pickup)
            {
                PrivateHacker.GrabCard(c);
            }
            else
            {
                return drop_held_card();
            }
            return false;
		}
    }
}
