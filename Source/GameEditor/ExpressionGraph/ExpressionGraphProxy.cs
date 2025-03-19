using System;
using FlaxEditor.Content;
using FlaxEditor.Windows;
using FlaxEngine;

namespace Game.Editor
{
    [ContentContextMenu("New/ExpressionGraph")]
    public class ExpressionGraphProxy : JsonAssetProxy
    {
        public override string Name => "Expression Graph";

        public override Color AccentColor => Color.FromRGB(0x0F0371);

        public override bool CanCreate(ContentFolder targetLocation)
        {
            return targetLocation.CanHaveAssets;
        }

        public override string TypeName => typeof(ExpressionGraph).FullName;

        public override AssetItem ConstructItem(string path, string typeName, ref Guid id)
        {
            return new JsonAssetItem(path, id, typeName, SpriteHandle.Invalid);
        }

        public override void Create(string outputPath, object arg)
        {
            FlaxEditor.Editor.SaveJsonAsset(outputPath, new ExpressionGraph());
        }

        public override EditorWindow Open(FlaxEditor.Editor editor, ContentItem item)
        {
            return new ExpressionGraphWindow(editor, (JsonAssetItem) item);
        }
    }
}

