using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace nanomodtests.Content
{
    public class ItemTuningFork : Item
    {
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            AddAllTypesToCreativeInventory();
        }

        protected virtual void AddAllTypesToCreativeInventory()
        {
            List<JsonItemStack> stacks = new List<JsonItemStack>();
            Dictionary<string, string[]> vg = this.Attributes["variantGroups"].AsObject<Dictionary<string, string[]>>();
            JsonItemStack stack;
            foreach (var handleMaterial in vg["handle"])
            {
                foreach (var tipMaterial in vg["tip"])
                {
                    stack = new JsonItemStack()
                    {
                        Code = Code,
                        Type = EnumItemClass.Item,
                        Attributes = new JsonObject(JToken.Parse($"{{materials: {{handle: \"{handleMaterial}\", tip: \"{tipMaterial}\"}}}}")),
                    };
                    stack.Resolve(api.World, "tuning fork", true);
                    stacks.Add(stack);
                }
            }

            CreativeInventoryStacks = new CreativeTabAndStackList[]
            {
                new CreativeTabAndStackList()
                {
                    Stacks = stacks.ToArray(),
                    Tabs = new string[] { "general", "items", "nanomodtests" }
                }
            };
        }
    }
}
