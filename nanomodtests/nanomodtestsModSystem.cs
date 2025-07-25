using nanomodtests.Content;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace nanomodtests
{
    public class nanomodtestsModSystem : ModSystem
    {

        // Called on server and client
        // Useful for registering block/entity classes on both sides
        public override void Start(ICoreAPI api)
        {
            Mod.Logger.Notification("Hello from template mod: " + api.Side);
            RegisterCollectibleBehaviors(api);
            RegisterItems(api);
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            Mod.Logger.Notification("Hello from template mod server side: " + Lang.Get("nanomodtests:hello"));
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            Mod.Logger.Notification("Hello from template mod client side: " + Lang.Get("nanomodtests:hello"));
        }

        private void RegisterItems(ICoreAPI api)
        {
            api.RegisterItemClass("ItemTuningFork", typeof(ItemTuningFork));
            api.RegisterItemClass("ItemLiveMap", typeof(ItemLiveMap));
        }

        private void RegisterCollectibleBehaviors(ICoreAPI api)
        {
            api.RegisterCollectibleBehaviorClass("TexturesFromAttributes", typeof(CollectibleTexturesFromAttributes));
        }

    }
}
