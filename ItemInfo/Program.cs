using BepInEx;
using HarmonyLib;

namespace ItemInfo
{
    [BepInPlugin("com.Amanda.ItemInfo", "ItemInfo", "1.0.0")]
    public class ItemInfoPlugin : BaseUnityPlugin
    {
        private void Awake()
        {
            new ItemPatch().Enable();
            new AmmoPatch().Enable();
            new GrenadePatch().Enable();
            new SecureContainerPatch().Enable();
        }
    }
}
