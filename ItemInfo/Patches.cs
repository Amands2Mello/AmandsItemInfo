using System;
using System.Reflection;
using Aki.Reflection.Patching;
using EFT.InventoryLogic;
using Ammo = BulletClass;
using Grenade = GClass2174;
using GrenadeTemplate = GClass2067;
using SecureContainer = GClass2120;
using SecureContainerTemplate = GClass2028;

namespace ItemInfo
{
    public class ItemPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Item).GetConstructor(new Type[] { typeof(string), typeof(ItemTemplate) });
        }

        [PatchPostfix]
        private static void PatchPostFix(ref Item __instance, string id, ItemTemplate template)
        {
            ItemInfoClass.AddItemInfo(ref __instance, id, template);
        }
    }

    public class AmmoPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Ammo).GetConstructor(new Type[] { typeof(string), typeof(AmmoTemplate) });
        }

        [PatchPostfix]
        private static void PatchPostFix(ref Ammo __instance, string id, AmmoTemplate template)
        {
            ItemInfoClass.AddItemInfo(ref __instance, id, template);
        }
    }

    public class GrenadePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Grenade).GetConstructor(new Type[] { typeof(string), typeof(GrenadeTemplate) });
        }

        [PatchPostfix]
        private static void PatchPostFix(ref Grenade __instance, string id, GrenadeTemplate template)
        {
            ItemInfoClass.AddItemInfo(ref __instance, id, template);
        }
    }

    public class SecureContainerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(SecureContainer).GetConstructor(new Type[] { typeof(string), typeof(SecureContainerTemplate) });
        }

        [PatchPostfix]
        private static void PatchPostFix(ref SecureContainer __instance, string id, SecureContainerTemplate template)
        {
            ItemInfoClass.AddItemInfo(ref __instance, id, template);
        }
    }
}