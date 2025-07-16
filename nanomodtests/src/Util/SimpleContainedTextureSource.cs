using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace nanomodtests.Util
{
    // Bootleg implementation ITexPositionSource that's a tad bit more lightweight than ContainedTextureSource
    public class SimpleContainedTextureSource : ITexPositionSource
    {
        private ITextureAtlasAPI atlas;
        private Dictionary<string, TextureAtlasPosition> positions = new Dictionary<string, TextureAtlasPosition>();

        public SimpleContainedTextureSource(ITextureAtlasAPI atlas)
        {
            this.atlas = atlas;
        }

        public void AddTexture(string name, TextureAtlasPosition position)
        {
            positions.Add(name, position);
        }

        public TextureAtlasPosition this[string index] => positions[index];

        public Size2i AtlasSize => atlas.Size;
    }
}
