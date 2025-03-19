using FlaxEditor.Viewport.Previews;
using FlaxEngine;
using FlaxEngine.GUI;

namespace Game.Editor
{
    public class ExpressionGraphPreview : AssetPreview
    {
        public ExpressionGraphPreview(bool useWidgets): base(useWidgets)
        {
            ShowDefaultSceneActors = false;
            Task.Enabled = false;
        }
        
        public ExpressionGraph ExpressionGraph { get; set; }
        
        private float[] _graphValues = new float[0];
        
        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);
            
            // Manually update simulation
            ExpressionGraph?.Update();
        }

        public override void Draw()
        {
            base.Draw();
            
            if (ExpressionGraph == null) return;

            if (ExpressionGraph.OutputFloats.Length != _graphValues.Length)
            {
                _graphValues = new float[ExpressionGraph.OutputFloats.Length];
            }

            Vector2 scale = new Vector2(Width / _graphValues.Length, -10f);
            Vector2 offset = new Vector2(0, Height / 2f);

            // Horizontal line
            Render2D.DrawLine(new Vector2(0, offset.Y), new Vector2(Width, offset.Y), Color.Red);

            // Vertical line
            //Render2D.DrawLine(new Vector2(offset.X, 0), new Vector2(offset.X, Height), Color.Red);

            for (int i = 0; i < _graphValues.Length - 1; i++)
            {
                _graphValues[i] = Mathf.Lerp(_graphValues[i], ExpressionGraph.OutputFloats[i], 0.7f);

                Vector2 from = new Vector2(i, _graphValues[i]) * scale + offset;
                Vector2 to = new Vector2(i + 1, _graphValues[i + 1]) * scale + offset;
                Render2D.DrawLine(from, to, Color.White);
            }
        }

        public override void OnDestroy()
        {
            ExpressionGraph = null;
            base.OnDestroy();
        }
    }
}

