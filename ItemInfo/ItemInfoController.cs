using Aki.Common.Http;
using Aki.Common.Utils;
using EFT.InventoryLogic;
using System;
using System.Collections.Generic;
using System.Timers;
using UnityEngine;
using System.Reflection;
using ItemAttribute = GClass2197;
using System.Net;

namespace ItemInfo
{
    public class ItemInfoClass
    {
        public static void AddItemInfo<T>(ref T __instance, string id, ItemTemplate template) where T : Item
        {
            var atts = new List<ItemAttribute>();
            atts.AddRange(__instance.Attributes);
            __instance.Attributes = atts;
            ItemAttribute attr = new ItemAttribute(EItemAttributeId.MoneySum)
            {
                StringValue = new Func<string>(__instance.traderPrice),
                FullStringValue = new Func<string>(__instance.traderName),
                Name = "TRADER ",
                DisplayType = new Func<EItemAttributeDisplayType>(() => EItemAttributeDisplayType.Compact)
            };
            ItemAttribute attr2 = new ItemAttribute(EItemAttributeId.MoneySum)
            {
                StringValue = new Func<string>(__instance.fleaPrice),
                Name = "FLEA",
                DisplayType = new Func<EItemAttributeDisplayType>(__instance.fleaPriceDisplayType)
            };
            ItemAttribute attr3 = new ItemAttribute(EItemAttributeId.Resource)
            {
                StringValue = new Func<string>(__instance.neededInfo),
                FullStringValue = new Func<string>(__instance.neededInfoTooltip),
                Name = "NEEDED",
                DisplayType = new Func<EItemAttributeDisplayType>(__instance.neededDisplayType)
            };
            ItemAttribute attr4 = new ItemAttribute(EItemAttributeId.ContainerSize)
            {
                StringValue = new Func<string>(__instance.stashInfo),
                FullStringValue = new Func<string>(__instance.stashInfoTooltip),
                Name = "STASH",
                DisplayType = new Func<EItemAttributeDisplayType>(__instance.stashDisplayType)
            };
            __instance.Attributes.Add(attr);
            __instance.Attributes.Add(attr2);
            __instance.Attributes.Add(attr3);
            __instance.Attributes.Add(attr4);
        }
    }

    public static class InfoExtension
    {
        static public Dictionary<string, JsonClass> dict = new Dictionary<string, JsonClass>();
        static object traderPricelockObject = new object();
        static object traderNamelockObject = new object();
        static object fleaPricelockObject = new object();
        static object fleaPriceDisplayTypelockObject = new object();
        static object neededInfolockObject = new object();
        static object neededInfoTooltiplockObject = new object();
        static object neededDisplayTypelockObject = new object();
        static object stashInfolockObject = new object();
        static object stashInfoTooltiplockObject = new object();
        static object stashDisplayTypelockObject = new object();
        static string _id;
        static JsonClass jsonClass;
        public static string traderPrice(this Item item)
        {
            bool lockWasTaken = false;
            try
            {
                System.Threading.Monitor.Enter(traderPricelockObject, ref lockWasTaken);
                if (item != null && item != null && item?.Template?._id != _id)
                {
                    _id = item?.Template?._id;
                    jsonClass = null;
                    var json = RequestHandler.GetJson($"/amanda/iteminfo/{_id}");
                    if (json != null && !json.Contains("null") && !json.Contains("undefined") && !json.Contains("Infinity") && !json.Contains("-Infinity") && !json.Contains("NaN") && !json.Contains("UNHANDLED"))
                    {
                        jsonClass = Json.Deserialize<JsonClass>(json);
                    }
                }
            }
            catch (WebException)
            {
                return "WebException";
            }
            finally
            {
                if (lockWasTaken) System.Threading.Monitor.Exit(traderPricelockObject);
            }
            double trader = 0;
            double durability = 1;
            double multiplier = 1;
            if (jsonClass != null && item != null)
            {
                trader = jsonClass.trader;
                durability = jsonClass.durability;
                multiplier = jsonClass.multiplier;
                var medKit = item.GetItemComponent<MedKitComponent>();
                if (medKit != null && medKit.HpResource != 0 && medKit.MaxHpResource != 0)
                {
                    trader *= medKit.HpResource / medKit.MaxHpResource;
                }

                var repair = item.GetItemComponent<RepairableComponent>();
                if (repair != null)
                {
                    if (repair.Durability > 0)
                    {
                        trader *= repair.Durability / durability;
                    }
                    else
                    {
                        trader = 1;
                    }
                }

                var dogtag = item.GetItemComponent<DogtagComponent>();
                if (dogtag != null && dogtag.Level != 0)
                {
                    trader *= dogtag.Level;
                }

                var repairKit = item.GetItemComponent<RepairKitComponent>();
                if (repairKit != null)
                {
                    if (repairKit.Resource > 0)
                    {
                        trader *= repairKit.Resource / durability;
                    }
                    else
                    {
                        trader = 1;
                    }
                }

                var resource = item.GetItemComponent<ResourceComponent>();
                if (resource != null && resource.Value != 0 && resource.MaxResource != 0)
                {
                    trader *= resource.Value / resource.MaxResource;
                }

                var foodDrink = item.GetItemComponent<FoodDrinkComponent>();
                if (foodDrink != null && foodDrink.HpPercent != 0)
                {
                    GInterface208 ginterface208_0 = (GInterface208)foodDrink.GetType().GetField("ginterface208_0", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(foodDrink);

                    trader *= foodDrink.HpPercent / ginterface208_0.MaxResource;
                }

                var keys = item.GetItemComponent<KeyComponent>();
                if (keys != null)
                {
                    GInterface212 ginterface212_0 = (GInterface212)keys.GetType().GetField("Template", BindingFlags.Public | BindingFlags.Instance).GetValue(keys);

                    if (keys.NumberOfUsages > 0)
                    {
                        double totalMinusUsed = Convert.ToDouble(ginterface212_0.MaximumNumberOfUsage - keys.NumberOfUsages);
                        double multi = totalMinusUsed / ginterface212_0.MaximumNumberOfUsage;

                        trader *= multi;
                    }
                }

                var sideEffect = item.GetItemComponent<SideEffectComponent>();
                if (sideEffect != null && sideEffect.Value != 0)
                {
                    trader *= sideEffect.Value / sideEffect.MaxResource;
                }
            }
            return Math.Round(trader*multiplier).ToString();
        }
        public static string traderName(this Item item)
        {
            bool lockWasTaken = false;
            try
            {
                System.Threading.Monitor.Enter(traderNamelockObject, ref lockWasTaken);
                if (item != null && item != null && item?.Template?._id != _id)
                {
                    _id = item?.Template?._id;
                    jsonClass = null;
                    var json = RequestHandler.GetJson($"/amanda/iteminfo/{_id}");
                    if (json != null && !json.Contains("null") && !json.Contains("undefined") && !json.Contains("Infinity") && !json.Contains("-Infinity") && !json.Contains("NaN") && !json.Contains("UNHANDLED"))
                    {
                        jsonClass = Json.Deserialize<JsonClass>(json);
                    }
                }
            }
            catch (WebException)
            {
                return "WebException";
            }
            finally
            {
                if (lockWasTaken) System.Threading.Monitor.Exit(traderNamelockObject);
            }
            string name = "";
            if (jsonClass != null)
            {
                name = jsonClass.name;
            }
            return name;
        }
        public static string fleaPrice(this Item item)
        {
            bool lockWasTaken = false;
            try
            {
                System.Threading.Monitor.Enter(fleaPricelockObject, ref lockWasTaken);
                if (item != null && item != null && item?.Template?._id != _id)
                {
                    _id = item?.Template?._id;
                    jsonClass = null;
                    var json = RequestHandler.GetJson($"/amanda/iteminfo/{_id}");
                    if (json != null && !json.Contains("null") && !json.Contains("undefined") && !json.Contains("Infinity") && !json.Contains("-Infinity") && !json.Contains("NaN") && !json.Contains("UNHANDLED"))
                    {
                        jsonClass = Json.Deserialize<JsonClass>(json);
                    }
                }
            }
            catch (WebException)
            {
                return "WebException";
            }
            finally
            {
                if (lockWasTaken) System.Threading.Monitor.Exit(fleaPricelockObject);
            }
            double flea = 0;
            if (jsonClass != null && item != null)
            {
                flea = jsonClass.flea;
                double durability = jsonClass.durability;
                var medKit = item.GetItemComponent<MedKitComponent>();
                if (medKit != null && medKit.HpResource != 0 && medKit.MaxHpResource != 0)
                {
                    flea *= medKit.HpResource / medKit.MaxHpResource;
                }

                var repair = item.GetItemComponent<RepairableComponent>();
                if (repair != null)
                {
                    if (repair.Durability > 0)
                    {
                        flea *= repair.Durability / durability;
                    }
                    else
                    {
                        flea = 1;
                    }
                }

                var dogtag = item.GetItemComponent<DogtagComponent>();
                if (dogtag != null && dogtag.Level != 0)
                {
                    flea *= dogtag.Level;
                }

                var repairKit = item.GetItemComponent<RepairKitComponent>();
                if (repairKit != null)
                {
                    if (repairKit.Resource > 0)
                    {
                        flea *= repairKit.Resource / durability;
                    }
                    else
                    {
                        flea = 1;
                    }
                }

                var resource = item.GetItemComponent<ResourceComponent>();
                if (resource != null && resource.Value != 0 && resource.MaxResource != 0)
                {
                    flea *= resource.Value / resource.MaxResource;
                }

                var foodDrink = item.GetItemComponent<FoodDrinkComponent>();
                if (foodDrink != null && foodDrink.HpPercent != 0)
                {
                    GInterface208 ginterface208_0 = (GInterface208)foodDrink.GetType().GetField("ginterface208_0", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(foodDrink);

                    flea *= foodDrink.HpPercent / ginterface208_0.MaxResource;
                }

                var keys = item.GetItemComponent<KeyComponent>();
                if (keys != null)
                {
                    GInterface212 ginterface212_0 = (GInterface212)keys.GetType().GetField("Template", BindingFlags.Public | BindingFlags.Instance).GetValue(keys);

                    if (keys.NumberOfUsages > 0)
                    {
                        double totalMinusUsed = Convert.ToDouble(ginterface212_0.MaximumNumberOfUsage - keys.NumberOfUsages);
                        double multi = totalMinusUsed / ginterface212_0.MaximumNumberOfUsage;

                        flea *= multi;
                    }
                }

                var sideEffect = item.GetItemComponent<SideEffectComponent>();
                if (sideEffect != null && sideEffect.Value != 0)
                {
                    flea *= sideEffect.Value / sideEffect.MaxResource;
                }
            }
            return Math.Round(flea).ToString();
        }
        public static EItemAttributeDisplayType fleaPriceDisplayType(this Item item)
        {
            bool lockWasTaken = false;
            try
            {
                System.Threading.Monitor.Enter(fleaPriceDisplayTypelockObject, ref lockWasTaken);
                if (item != null && item != null && item?.Template?._id != _id)
                {
                    _id = item?.Template?._id;
                    jsonClass = null;
                    var json = RequestHandler.GetJson($"/amanda/iteminfo/{_id}");
                    if (json != null && !json.Contains("null") && !json.Contains("undefined") && !json.Contains("Infinity") && !json.Contains("-Infinity") && !json.Contains("NaN") && !json.Contains("UNHANDLED"))
                    {
                        jsonClass = Json.Deserialize<JsonClass>(json);
                    }
                }
            }
            catch (WebException)
            {
                return EItemAttributeDisplayType.Special;
            }
            finally
            {
                if (lockWasTaken) System.Threading.Monitor.Exit(fleaPriceDisplayTypelockObject);
            }
            if (jsonClass != null)
            {
                if (jsonClass.cansellonflea)
                {
                    return EItemAttributeDisplayType.Compact;
                }
                else
                {
                    return EItemAttributeDisplayType.Special;
                }
            }
            else
            {
                return EItemAttributeDisplayType.Special;
            }
        }
        public static string neededInfo(this Item item)
        {
            bool lockWasTaken = false;
            try
            {
                System.Threading.Monitor.Enter(neededInfolockObject, ref lockWasTaken);
                if (item != null && item != null && item?.Template?._id != _id)
                {
                    _id = item?.Template?._id;
                    jsonClass = null;
                    var json = RequestHandler.GetJson($"/amanda/iteminfo/{_id}");
                    if (json != null && !json.Contains("null") && !json.Contains("undefined") && !json.Contains("Infinity") && !json.Contains("-Infinity") && !json.Contains("NaN") && !json.Contains("UNHANDLED"))
                    {
                        jsonClass = Json.Deserialize<JsonClass>(json);
                    }
                }
            }
            catch (WebException)
            {
                return "WebException";
            }
            finally
            {
                if (lockWasTaken) System.Threading.Monitor.Exit(neededInfolockObject);
            }
            double quest = 0;
            double hideout = 0;
            double wishlist = 0;
            double stashRaid = 0;
            double stash = 0;
            if (jsonClass != null)
            {
                quest = jsonClass.quest;
                hideout = jsonClass.hideout;
                wishlist = jsonClass.wishlist;
                stash = jsonClass.stash;
                stashRaid = jsonClass.stashRaid;
                if (Math.Round(quest) > 0 && Math.Round(hideout) == 0 && Math.Round(wishlist) == 0)
                {
                    return "Quest " + Math.Round(stashRaid).ToString() + "/" + Math.Round(quest).ToString();
                }
                if (Math.Round(hideout) > 0 && Math.Round(quest) == 0 && Math.Round(wishlist) == 0)
                {
                    return "Hideout " + Math.Round(stashRaid + stash).ToString() + "/" + Math.Round(hideout).ToString();
                }
                if (Math.Round(wishlist) > 0 && Math.Round(quest) == 0 && Math.Round(hideout) == 0)
                {
                    return "Wishlist " + Math.Round(stashRaid + stash).ToString() + "/" + Math.Round(wishlist).ToString();
                }
            }
            return Math.Round(stashRaid + stash).ToString() + "/" + Math.Round(quest + hideout + wishlist).ToString();
        }
        public static string neededInfoTooltip(this Item item)
        {
            bool lockWasTaken = false;
            try
            {
                System.Threading.Monitor.Enter(neededInfoTooltiplockObject, ref lockWasTaken);
                if (item != null && item != null && item?.Template?._id != _id)
                {
                    _id = item?.Template?._id;
                    jsonClass = null;
                    var json = RequestHandler.GetJson($"/amanda/iteminfo/{_id}");
                    if (json != null && !json.Contains("null") && !json.Contains("undefined") && !json.Contains("Infinity") && !json.Contains("-Infinity") && !json.Contains("NaN") && !json.Contains("UNHANDLED"))
                    {
                        jsonClass = Json.Deserialize<JsonClass>(json);
                    }
                }
            }
            catch (WebException)
            {
                return "WebException";
            }
            finally
            {
                if (lockWasTaken) System.Threading.Monitor.Exit(neededInfoTooltiplockObject);
            }
            string questtooltip = "";
            string hideouttooltip = "";
            string wishlisttooltip = "";
            if (jsonClass != null)
            {
                double quest = jsonClass.quest;
                double stashRaid = jsonClass.stashRaid;
                double stash = jsonClass.stash;
                double hideout = jsonClass.hideout;
                double wishlist = jsonClass.wishlist;
                if (quest > 0)
                {
                    if (Math.Round(stashRaid) >= Math.Round(quest))
                    {
                        if (hideout > 0)
                        {
                            questtooltip = "Quest " + quest + "/" + quest + "\n";
                            hideouttooltip = "Hideout " + (stash + (stashRaid - quest)) + "/" + hideout + "\n";
                        }
                        else
                        {
                            questtooltip = "Quest " + stashRaid + "/" + quest + "\n";
                        }
                    }
                    else
                    {
                        questtooltip = "Quest " + stashRaid + "/" + quest + "\n";
                        if (hideout > 0)
                        {
                            hideouttooltip = "Hideout " + stash + "/" + hideout + "\n";
                        }
                    }
                }
                else if (hideout > 0)
                {
                    hideouttooltip = "Hideout " + (stashRaid + stash) + "/" + hideout + "\n";
                }
                if (Math.Round(wishlist) > 0)
                {
                    wishlisttooltip = "Wishlist";
                }
            }
            return questtooltip + hideouttooltip + wishlisttooltip;
        }
        public static EItemAttributeDisplayType neededDisplayType(this Item item)
        {
            bool lockWasTaken = false;
            try
            {
                System.Threading.Monitor.Enter(neededDisplayTypelockObject, ref lockWasTaken);
                if (item != null && item != null && item?.Template?._id != _id)
                {
                    _id = item?.Template?._id;
                    jsonClass = null;
                    var json = RequestHandler.GetJson($"/amanda/iteminfo/{_id}");
                    if (json != null && !json.Contains("null") && !json.Contains("undefined") && !json.Contains("Infinity") && !json.Contains("-Infinity") && !json.Contains("NaN") && !json.Contains("UNHANDLED"))
                    {
                        jsonClass = Json.Deserialize<JsonClass>(json);
                    }
                }
            }
            catch (WebException)
            {
                return EItemAttributeDisplayType.Special;
            }
            finally
            {
                if (lockWasTaken) System.Threading.Monitor.Exit(neededDisplayTypelockObject);
            }
            if (jsonClass != null)
            {
                double quest = jsonClass.quest;
                double hideout = jsonClass.hideout;
                double wishlist = jsonClass.wishlist;
                if ((quest + hideout + wishlist) > 0)
                {
                    return EItemAttributeDisplayType.Compact;
                }
                else
                {
                    return EItemAttributeDisplayType.Special;
                }
            }
            return EItemAttributeDisplayType.Special;
        }
        public static string stashInfo(this Item item)
        {
            bool lockWasTaken = false;
            try
            {
                System.Threading.Monitor.Enter(stashInfolockObject, ref lockWasTaken);
                if (item != null && item != null && item?.Template?._id != _id)
                {
                    _id = item?.Template?._id;
                    jsonClass = null;
                    var json = RequestHandler.GetJson($"/amanda/iteminfo/{_id}");
                    if (json != null && !json.Contains("null") && !json.Contains("undefined") && !json.Contains("Infinity") && !json.Contains("-Infinity") && !json.Contains("NaN") && !json.Contains("UNHANDLED"))
                    {
                        jsonClass = Json.Deserialize<JsonClass>(json);
                    }
                }
            }
            catch (WebException)
            {
                return "WebException";
            }
            finally
            {
                if (lockWasTaken) System.Threading.Monitor.Exit(stashInfolockObject);
            }
            double stashRaid = 0;
            double stash = 0;
            if (jsonClass != null)
            {
                stashRaid = jsonClass.stashRaid;
                stash = jsonClass.stash;
            }
            return Math.Round(stashRaid + stash).ToString(); ;
        }
        public static string stashInfoTooltip(this Item item)
        {
            bool lockWasTaken = false;
            try
            {
                System.Threading.Monitor.Enter(stashInfoTooltiplockObject, ref lockWasTaken);
                if (item != null && item != null && item?.Template?._id != _id)
                {
                    _id = item?.Template?._id;
                    jsonClass = null;
                    var json = RequestHandler.GetJson($"/amanda/iteminfo/{_id}");
                    if (json != null && !json.Contains("null") && !json.Contains("undefined") && !json.Contains("Infinity") && !json.Contains("-Infinity") && !json.Contains("NaN") && !json.Contains("UNHANDLED"))
                    {
                        jsonClass = Json.Deserialize<JsonClass>(json);
                    }
                }
            }
            catch (WebException)
            {
                return "WebException";
            }
            finally
            {
                if (lockWasTaken) System.Threading.Monitor.Exit(stashInfoTooltiplockObject);
            }
            string stashRaidtooltip = "";
            string stashtooltip = "";
            if (jsonClass != null)
            {
                double stashRaid = jsonClass.stashRaid;
                double stash = jsonClass.stash;
                if (Math.Round(stashRaid) > 0)
                {
                    stashRaidtooltip = stashRaid + " Found in Raid" + "\n";
                }
                if (Math.Round(stash) > 0)
                {
                    stashtooltip = stash + " Non Found in Raid" + "\n";
                }
            }
            return stashRaidtooltip + stashtooltip;
        }
        public static EItemAttributeDisplayType stashDisplayType(this Item item)
        {
            bool lockWasTaken = false;
            try
            {
                System.Threading.Monitor.Enter(stashDisplayTypelockObject, ref lockWasTaken);
                if (item != null && item != null && item?.Template?._id != _id)
                {
                    _id = item?.Template?._id;
                    jsonClass = null;
                    var json = RequestHandler.GetJson($"/amanda/iteminfo/{_id}");
                    if (json != null && !json.Contains("null") && !json.Contains("undefined") && !json.Contains("Infinity") && !json.Contains("-Infinity") && !json.Contains("NaN") && !json.Contains("UNHANDLED"))
                    {
                        jsonClass = Json.Deserialize<JsonClass>(json);
                    }
                }
            }
            catch (WebException)
            {
                return EItemAttributeDisplayType.Special;
            }
            finally
            {
                if (lockWasTaken) System.Threading.Monitor.Exit(stashDisplayTypelockObject);
            }
            if (jsonClass != null)
            {
                double quest = jsonClass.quest;
                double hideout = jsonClass.hideout;
                double wishlist = jsonClass.wishlist;
                double stashRaid = jsonClass.stashRaid;
                double stash = jsonClass.stash;
                if ((Math.Round(quest) > 0 && Math.Round(hideout) == 0 && Math.Round(wishlist) == 0) && Math.Round(stash) > 0)
                {
                    return EItemAttributeDisplayType.Compact;
                }
                else if (Math.Round(quest + hideout + wishlist) == 0 && Math.Round(stashRaid + stash) > 0)
                {
                    return EItemAttributeDisplayType.Compact;
                }
                else
                {
                    return EItemAttributeDisplayType.Special;
                }
            }
            return EItemAttributeDisplayType.Special;
        }
    }
}