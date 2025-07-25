using nanomodtests.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;

namespace nanomodtests.Content
{
    public class PropertyTextureMapping
    {
        // PropertyName is the path to the attribute we base this texture on
        public string PropertyName { get; set; }
        // 
        public string TextureName { get; set; }
        public Dictionary<string, AssetLocation> Mappings { get; set; }
    }

    public struct AttributeAndValue
    {
        public string Attribute { get; set; }
        public string Value { get; set; }
    }

    public class CollectibleTexturesFromAttributes : CollectibleBehavior// , IContainedMeshSource
    {
        private Dictionary<string, PropertyTextureMapping> _textureMappings;
        private ICoreClientAPI _capi;

        public CollectibleTexturesFromAttributes(CollectibleObject collObj) : base(collObj) { }

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
            var mappings = properties["TextureMappings"].AsObject<List<PropertyTextureMapping>>();
            _textureMappings = mappings.ToDictionary(m =>
            {
                var splits = m.PropertyName.Split('/');
                return splits[splits.Length - 1];
            });
        }

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            if (api.Side == EnumAppSide.Client)
            {
                _capi = api as ICoreClientAPI;
            }
        }

        public override void OnUnloaded(ICoreAPI api)
        {
            base.OnUnloaded(api);
            if (api.Side == EnumAppSide.Client)
            {
                // still need to dispose of all meshes in the cache...
                // we'll worry about that later!
            }
        }

        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
            if (itemstack.Class == EnumItemClass.Block) return;

            var meshRef = GetMeshRef(capi, itemstack);
            if (meshRef != null)
            {
                renderinfo.ModelRef = meshRef;
            }
        }

        public virtual MultiTextureMeshRef GetMeshRef(ICoreClientAPI capi, ItemStack stack)
        {
            if (_textureMappings == null) return null;
            // temporarily storing mesh key in itemstack could slightly reduce
            // processing time on re-render maybe kind of?
            string meshKey = stack.TempAttributes.GetString("texturedMeshKey");
            List<AttributeAndValue> mappings = null;
            if (meshKey == null)
            {
                mappings = GetMappedAttributesAndValues(stack.Attributes);
                foreach (var mapping in mappings)
                {
                    meshKey += mapping.Value;
                }

                stack.TempAttributes.SetString("texturedMeshKey", meshKey);
            }

            // pull our dict from global cache
            // learned this trick from base game code
            Dictionary<string, MultiTextureMeshRef> refs = ObjectCacheUtil.GetOrCreate(capi, stack.Collectible.Code, () =>
            {
                return new Dictionary<string, MultiTextureMeshRef>();
            });

            MultiTextureMeshRef outMesh;
            if (!refs.TryGetValue(meshKey, out outMesh))
            {
                if (mappings == null)
                {
                    mappings = GetMappedAttributesAndValues(stack.Attributes);
                }
                var mesh = GenMesh(capi, stack, mappings);
                outMesh = capi.Render.UploadMultiTextureMesh(mesh);
                refs.Add(meshKey, outMesh);
            }

            return outMesh;
        }

        // Does NOT work at the moment, no idea why
        /*// VERY bootleg and quick implementation of IContainedMeshSource, since Dana asked
        public MeshData GenMesh(ItemStack stack, ITextureAtlasAPI atlas, BlockPos pos)
        {
            var mappings = GetMappedAttributesAndValues(stack.Attributes);
            return GenMesh(_capi, stack, mappings);
        }

        public string GetMeshCacheKey(ItemStack stack)
        {
            string meshKey = string.Empty;
            var mappings = GetMappedAttributesAndValues(stack.Attributes);

            foreach (var mapping in mappings)
            {
                meshKey += mapping.Value;
            }

            return meshKey;
        }*/

        public virtual MeshData GenMesh(ICoreClientAPI capi, ItemStack stack, List<AttributeAndValue> values)
        {
            MeshData mesh;
            ITextureAtlasAPI atlas = stack.Class == EnumItemClass.Item ? capi.ItemTextureAtlas : capi.BlockTextureAtlas;

            SimpleContainedTextureSource textureMapping = new SimpleContainedTextureSource(capi, atlas);
            PropertyTextureMapping mapping;
            AssetLocation textureLocation;
            TextureAtlasPosition atlasPos;
            for (int i = 0; i < values.Count; i++)
            {
                _textureMappings.TryGetValue(values[i].Attribute, out mapping);
                if (mapping == null) continue;

                if (!mapping.Mappings.TryGetValue(values[i].Value, out textureLocation))
                {
                    textureLocation = new AssetLocation("game:unknown");
                }

                capi.ItemTextureAtlas.GetOrInsertTexture(textureLocation, out _, out atlasPos);

                textureMapping.AddTexture(mapping.TextureName, atlasPos);
            }

            // Better make sure all mappings are correct and such in the JSON
            // or this WILL crash
            capi.Tesselator.TesselateItem(stack.Item, out mesh, textureMapping);

            return mesh;
        }

        private List<AttributeAndValue> GetMappedAttributesAndValues(ITreeAttribute attributes)
        {
            List<AttributeAndValue> mappings = new List<AttributeAndValue>();

            ITreeAttribute subTree;
            string[] propertyParts;
            string truePropertyName;

            // Very trash way to get the attribute value from a path
            // maybe there's a way to get Newtonsoft to do it for us?
            // time crunch so no time to figure it out
            foreach (var mapping in _textureMappings.Values)
            {
                subTree = attributes;

                propertyParts = mapping.PropertyName.Split('/');
                for (int i = 0; i < (propertyParts.Length - 1); i++)
                {
                    subTree = subTree.GetTreeAttribute(propertyParts[i]);
                    if (subTree == null) break;
                }

                truePropertyName = propertyParts[propertyParts.Length - 1];
                if (subTree != null)
                {
                    mappings.Add(new AttributeAndValue
                    {
                        Attribute = truePropertyName,
                        // adding a default value in case of missing is important,
                        // as the values are used to create the dictionary key,
                        // missing or empty values would result in potential duplicate keys
                        Value = subTree.GetString(truePropertyName, "null")
                    });
                } else
                {
                    mappings.Add(new AttributeAndValue
                    {
                        Attribute = truePropertyName,
                        Value = "null"
                    });
                }
            }

            return mappings;
        }
    }
}
