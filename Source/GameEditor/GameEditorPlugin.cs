using FlaxEditor;

namespace Game.Editor
{
    public class GameEditorPlugin : EditorPlugin
    {
        private ExpressionGraphProxy _expressionGraphProxy;
        public override void InitializeEditor()
        {
            base.InitializeEditor();
            _expressionGraphProxy = new ExpressionGraphProxy();
            
            Editor.ContentDatabase.AddProxy(_expressionGraphProxy);
            Editor.ContentDatabase.Rebuild();
        }

        public override void DeinitializeEditor()
        {
            Editor.ContentDatabase.RemoveProxy(_expressionGraphProxy);
            
            base.DeinitializeEditor();
        }
    }
}
