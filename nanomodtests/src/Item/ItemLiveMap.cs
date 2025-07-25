using nanomodtests.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Common;
using Vintagestory.GameContent;
using static System.Net.Mime.MediaTypeNames;

namespace nanomodtests.Content
{
    public class ItemLiveMap : Item
    {
        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            var meshRef = GetMeshRef(capi);
            if (meshRef == null)
            {
                base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
                return;
            }

            renderinfo.ModelRef = meshRef;
        }

        public MultiTextureMeshRef GetMeshRef(ICoreClientAPI capi)
        {
            MultiTextureMeshRef mapMeshRef = ObjectCacheUtil.TryGet<MultiTextureMeshRef>(capi, "livemapmeshref");
            MeshData mesh = GenMesh(capi);
            if (mesh == null) return null;
            // return capi.Render.UploadMultiTextureMesh(mesh);
            if (mapMeshRef != null)
            {
                mapMeshRef.Dispose();
            }
            mapMeshRef = capi.Render.UploadMultiTextureMesh(mesh);
            ObjectCacheUtil.Delete(capi, "livemapmeshref");
            ObjectCacheUtil.GetOrCreate(capi, "livemapmeshref", () => { return mapMeshRef; });

            return mapMeshRef;
        }

        public MeshData GenMesh(ICoreClientAPI capi)
        {
            // ContainedTextureSource texSource = capi.Tesselator.GetTextureSource(this) as ContainedTextureSource;
            SimpleContainedTextureSource texSource = new SimpleContainedTextureSource(capi, capi.ItemTextureAtlas, "livemap");
            Textures.Foreach(t => texSource.AddTexture(t.Key, t.Value.Base));
            if (texSource["map-treasures"] == null) return null;

            TextureAtlasPosition pos = new TextureAtlasPosition();
            pos.x1 = 0;
            pos.y1 = 0;
            pos.x2 = 1;
            pos.y2 = 1;

            var mapManager = capi.ModLoader.GetModSystem<WorldMapManager>();
            var colorLayer = mapManager.MapLayers.First(l => l is ChunkMapLayer) as ChunkMapLayer;
            if (colorLayer == null) return null;

            try
            {
                Type mapLayerType = typeof(ChunkMapLayer);
                var mapData = mapLayerType
                    .GetField("loadedMapData", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(colorLayer) as ConcurrentDictionary<FastVec2i, MultiChunkMapComponent>;

                var player = capi.World.Player.Entity.Pos;
                FastVec2i mapPos = new FastVec2i((int)player.X / GlobalConstants.ChunkSize, (int)player.Z / GlobalConstants.ChunkSize);
                mapPos.X /= 3;
                mapPos.Y /= 3;
                mapData.TryGetValue(mapPos, out var mapComponent);
                if (mapComponent != null)
                {
                    int replacementId = mapComponent.Texture.TextureId;
                    pos.atlasTextureId = replacementId;
                    texSource.AddTexture("map-treasures", pos);
                }
            }
            catch
            {
            }
            
            capi.Tesselator.TesselateItem(this, out var mesh, texSource);

            return mesh;
        }
    }
}
