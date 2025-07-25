using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace nanomodtests.Util
{
    // Bootleg implementation ITexPositionSource that's a tad bit more lightweight than ContainedTextureSource
    public class SimpleContainedTextureSource : ITexPositionSource
    {
        private ITextureAtlasAPI _atlas;
        private ICoreClientAPI _capi;
        private string _sourceForLogging;
        private Dictionary<string, TextureAtlasPosition> positions = new Dictionary<string, TextureAtlasPosition>();

        public SimpleContainedTextureSource(ICoreClientAPI capi, ITextureAtlasAPI atlas, string sourceForLogging = "simpletexsource")
        {
            _capi = capi;
            _atlas = atlas;
        }

        public void AddTexture(string name, TextureAtlasPosition position)
        {
            if (positions.ContainsKey(name))
            {
                positions[name] = position;
            }
            else
            {
                positions.Add(name, position);
            }
        }

        public void AddTexture(string name, AssetLocation location)
        {
            if (positions.ContainsKey(name))
            {
                positions[name] = getOrCreateTexPos(location);
            }
            else
            {
                positions.Add(name, getOrCreateTexPos(location));
            }
        }

        public TextureAtlasPosition this[string index] => positions[index];

        public Size2i AtlasSize => _atlas.Size;

        protected TextureAtlasPosition getOrCreateTexPos(AssetLocation texturePath)
        {
            TextureAtlasPosition texpos = _atlas[texturePath];
            if (texpos == null)
            {
                IAsset texAsset = _capi.Assets.TryGet(texturePath.Clone().WithPathPrefixOnce("textures/").WithPathAppendixOnce(".png"), true);
                if (texAsset != null)
                {
                    int num;
                    _atlas.GetOrInsertTexture(texturePath, out num, out texpos, () => texAsset.ToBitmap(_capi), 0f);
                    if (texpos == null)
                    {
                        _capi.World.Logger.Error("{0}, require texture {1} which exists, but unable to upload it or allocate space", new object[] { _sourceForLogging, texturePath });
                        texpos = _atlas.UnknownTexturePosition;
                    }
                }
                else
                {
                    _capi.World.Logger.Error("{0}, require texture {1}, but no such texture found.", new object[] { _sourceForLogging, texturePath });
                    texpos = _atlas.UnknownTexturePosition;
                }
            }
            return texpos;
        }
    }
}
