using System;
using System.Collections.Generic;
using System.Linq;
using FlaxEditor.Content;
using FlaxEditor.GUI;
using FlaxEditor.Scripting;
using FlaxEditor.Surface;
using FlaxEditor.Windows.Assets;
using FlaxEngine;

namespace Game.Editor
{
    public class ExpressionGraphWindow : VisjectSurfaceWindow<JsonAsset, ExpressionGraphSurface, ExpressionGraphPreview>
    {
        private readonly ScriptType[] _newParameterTypes =
        [
            new ScriptType(typeof(float)),
            new ScriptType(typeof(Vector2)),
            new ScriptType(typeof(Vector3)),
            new ScriptType(typeof(Vector4))
        ];

        public override string SurfaceName => "Expression Graph";

        private readonly PropertiesProxy _properties;
        private ExpressionGraph _graph;
        
        private sealed class PropertiesProxy
        {
            [EditorOrder(1000), EditorDisplay("Parameters"), CustomEditor(typeof(ParametersEditor)), NoSerialize]
            public ExpressionGraphWindow Window { get; set; }
            
            [EditorOrder(20), EditorDisplay("General"), Tooltip("It's for demo purpose")]
            public int DemoInteger { get; set; }
            
            [HideInEditor, Serialize]
            public List<SurfaceParameter> Parameters 
            { 
                get => Window.VisjectSurface.Parameters;
                set => throw new Exception("No setter");
            }

            /// <summary>
            /// Gathers parameters from the specified window.
            /// </summary>
            /// <param name="window">The window.</param>
            public void OnLoad(ExpressionGraphWindow window)
            {
                Window = window;
            }

            /// <summary>
            /// Clears temporary data.
            /// </summary>
            public void OnClean()
            {
                Window = null;
            }
        }
        
        public ExpressionGraphWindow(FlaxEditor.Editor editor, AssetItem item) : base(editor, item)
        {
            // Asset preview
            _preview = new ExpressionGraphPreview(true)
            {
                Parent = _split2.Panel1
            };

            // Asset properties proxy
            _properties = new PropertiesProxy();
            _propertiesEditor.Select(_properties);
            
            // Surface
            _surface = new ExpressionGraphSurface(this, Save, _undo)
            {
                Parent = _split1.Panel1,
                Enabled = false
            };
            
            PerformCommonSetup(this, _toolstrip, _surface, out _saveButton, out _undoButton, out _redoButton);
        }
        
        internal static void PerformCommonSetup(AssetEditorWindow window, ToolStrip toolStrip, VisjectSurface surface,
            out ToolStripButton saveButton, out ToolStripButton undoButton, out ToolStripButton redoButton)
        {
            var editor = window.Editor;
            var inputOptions = editor.Options.Options.Input;
            var undo = surface.Undo;

            // Toolstrip
            saveButton = toolStrip.AddButton(editor.Icons.Save64, window.Save).LinkTooltip("Save", ref inputOptions.Save);
            toolStrip.AddSeparator();
            undoButton = toolStrip.AddButton(editor.Icons.Undo64, undo.PerformUndo).LinkTooltip("Undo", ref inputOptions.Undo);
            redoButton = toolStrip.AddButton(editor.Icons.Redo64, undo.PerformRedo).LinkTooltip("Redo", ref inputOptions.Redo);
            toolStrip.AddSeparator();
            toolStrip.AddButton(editor.Icons.Search64, editor.ContentFinding.ShowSearch).LinkTooltip("Open content search tool",  ref inputOptions.Search);
            toolStrip.AddButton(editor.Icons.CenterView64, surface.ShowWholeGraph).LinkTooltip("Show whole graph");

            // Setup input actions
            window.InputActions.Add(options => options.Undo, undo.PerformUndo);
            window.InputActions.Add(options => options.Redo, undo.PerformRedo);
            window.InputActions.Add(options => options.Search, editor.ContentFinding.ShowSearch);
        }
        
        protected override void UnlinkItem()
        {
            _properties.OnClean();
            _preview.ExpressionGraph = null;
            
            base.UnlinkItem();
        }


        protected override void OnAssetLinked()
        {
            _graph = _asset.CreateInstance<ExpressionGraph>();
            _preview.ExpressionGraph = _graph;
            
            base.OnAssetLinked();
        }
        
        public override byte[] SurfaceData
        {
            get => ExpressionGraphSurface.LoadSurface(_graph, true);
            set
            {
                if (ExpressionGraphSurface.SaveSurface(_asset, _graph, value))
                {
                    _surface.MarkAsEdited();
                    Debug.LogError("Failed to save surface data");
                }
            }
        }

        protected override bool LoadSurface()
        {
            _properties.OnLoad(this);

            if (_surface.Load())
            {
                Debug.LogError("Failed ot load expression graph surface");
                return true;
            }
            
            return false;
        }
        
        protected override bool SaveSurface()
        {
            _surface.CompileSurface(_graph);
            _surface.Save();
            return false;
        }


        public override void SetParameter(int index, object value)
        {
            _graph.Parameters.First(p => p.Index == index).Value = value;
            base.SetParameter(index, value);
        }

        public override IEnumerable<ScriptType> NewParameterTypes => _newParameterTypes;
    }
}

