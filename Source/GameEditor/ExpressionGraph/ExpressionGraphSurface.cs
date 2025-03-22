using System;
using System.Collections.Generic;
using System.Linq;
using FlaxEditor;
using FlaxEditor.Surface;
using FlaxEditor.Surface.Elements;
using FlaxEngine;

namespace Game.Editor
{
    public class ExpressionGraphSurface : VisjectSurface
    {
        private const int MainModeGroupId = 1;
        private const int MainNodeTypeId = 1;
        private const int ParameterGroupId = 6;
        
        private const int CurvesGroupId = 7;
        private const int FloatCurveTypeId = 12;
        
        private readonly NodeVariableIndexLookup _indexLookup = new ();

        private static readonly NodeArchetype[] ExpressionGraphNodes =
        [
            new NodeArchetype()
            {
                TypeID = 1,
                Title = "ExpressionGraph",
                Description = "Main number graph node",
                Flags = NodeFlags.AllGraphs | NodeFlags.NoRemove | NodeFlags.NoSpawnViaGUI | NodeFlags.NoCloseButton,
                Size = new Float2(150, 40),
                Elements =
                [
                    NodeElementArchetype.Factory.Input(0, "Float", true, typeof(float), 0) // Last optional param: Value Index
                ]
            },
            // Random float
            new NodeArchetype
            {
                TypeID = 2,
                Title = "Random float",
                Description = "A random float",
                Flags = NodeFlags.AllGraphs,
                Size = new Vector2(150, 40),
                Elements =
                [
                    NodeElementArchetype.Factory.Output(0, "Float", typeof(float), 0)
                ]
            },
            // Get X
            new NodeArchetype()
            {
                TypeID = 3,
                Title = "Get X",
                Description = "Get the X coordinate",
                Flags = NodeFlags.AllGraphs,
                Size = new Vector2(150, 40),
                Elements = 
                [
                    NodeElementArchetype.Factory.Output(1, "X", typeof(float), 0)
                ]
            }
        ];

        private static readonly List<GroupArchetype> ExpressionGraphGroups =
        [
            new GroupArchetype
            {
                GroupID = MainModeGroupId,
                Name = "ExpressionGraph",
                Color = new Color(231, 231, 60),
                Archetypes = ExpressionGraphNodes
            },
            // All math nodes
            new GroupArchetype
            {
                GroupID = 3,
                Name = "Math",
                Color = new Color(52, 152, 219),
                Archetypes = FlaxEditor.Surface.Archetypes.Math.Nodes
            },
            // Just a single parameter node
            new GroupArchetype
            {
                GroupID = 6,
                Name = "Parameters",
                Color = new Color(52, 73, 94),
                Archetypes = [ FlaxEditor.Surface.Archetypes.Parameters.Nodes[0] ]
            },
            
            new GroupArchetype()
            {
                GroupID = 7,
                Name = "Curves",
                Color = new Color(52, 73, 94),
                Archetypes = [ FlaxEditor.Surface.Archetypes.Tools.Nodes[11] ]
            }
        ];
        
        public ExpressionGraphSurface(IVisjectSurfaceOwner owner, Action onSave, Undo undo = null, SurfaceStyle style = null) 
            : base(owner, onSave, undo, style, ExpressionGraphGroups)
        {
        }

        public static byte[] LoadSurface(ExpressionGraph graph, bool createDefaultIfMissing)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));

            if (graph.VisjectSurface != null && graph.VisjectSurface.Length > 0)
            {
                return graph.VisjectSurface;
            }

            if (createDefaultIfMissing)
            {
                var surfaceContext = new VisjectSurfaceContext(null, null, new FakeSurfaceContext());
                
                // Add the main node
                var node = NodeFactory.CreateNode(ExpressionGraphGroups, 1, surfaceContext, MainModeGroupId,
                    MainNodeTypeId);
                if (node == null)
                {
                    Debug.LogWarning("Failed to create main node.");
                    return null;
                }
                
                surfaceContext.Nodes.Add(node);
                node.Location = Float2.Zero;
                
                surfaceContext.Save();

                return surfaceContext.Context.SurfaceData;
            }
            else
            {
                return null;
            }
        }

        public static bool SaveSurface(JsonAsset asset, ExpressionGraph graph, byte[] surfaceData)
        {
            if (!asset) throw new ArgumentNullException(nameof(asset));
            graph.VisjectSurface = surfaceData;
            
            bool success = FlaxEditor.Editor.SaveJsonAsset(asset.Path, graph);
            asset.Reload();
            
            return success;
        }

        public void CompileSurface(ExpressionGraph graph)
        {
            _indexLookup.Reset();
            
            var graphParameters = new Dictionary<Guid, ExpressionGraphParameter>();

            if (Parameters != null && Parameters.Count > 0)
            {
                for (int i = 0; i < Parameters.Count; i++)
                {
                    var surfaceParameter = Parameters[i];
                    graphParameters.Add(surfaceParameter.ID, new ExpressionGraphParameter
                    {
                        Name = surfaceParameter.Name,
                        Index = i,
                        Value = surfaceParameter.Value,
                        OutputIndex = _indexLookup.GetFreeVariableIndex(),
                    });
                }
            }

            graph.Parameters = graphParameters.Values.ToArray();
            
            var surfaceNodes = FindNode(MainModeGroupId, MainNodeTypeId).DepthFirstTraversal(true, true);

            List<ExpressionGraphNode> graphNodes = new List<ExpressionGraphNode>();
            
            foreach (var surfaceNode in surfaceNodes)
            {
                graphNodes.Add(TransferToGraphNode(surfaceNode, graphParameters));
            }
            
            graph.Nodes = graphNodes.ToArray();
        }

        private ExpressionGraphNode TransferToGraphNode(SurfaceNode surfaceNode, Dictionary<Guid, ExpressionGraphParameter> graphParameters)
        {
            
            List<object> inputValues = new List<object>();
            List<int> outputIndices = new List<int>();
            List<int> inputIndices = new List<int>();
            
            foreach (var element in surfaceNode.Elements)
            {
                if (element is InputBox inputBox)
                {
                    int valueIndex = inputBox.Archetype.ValueIndex;
                    inputValues.Add(valueIndex != -1 ? surfaceNode.Values[valueIndex] : null);

                    int index = -1;
                    if (inputBox.HasAnyConnection)
                        index = _indexLookup.GetVariableIndex(inputBox);
                    inputIndices.Add(index);
                }
                else if (element is OutputBox outputBox)
                {
                    int outputIndex = _indexLookup.RegisterOutputBox(outputBox);
                    outputIndices.Add(outputIndex);
                }
            }

            int groupId = surfaceNode.GroupArchetype.GroupID;
            int typeId = surfaceNode.Archetype.TypeID;

            if (surfaceNode.GroupArchetype.GroupID == ParameterGroupId)
            {
                var graphParam = graphParameters[(Guid)surfaceNode.Values[0]];
                return new ExpressionGraphNode()
                {
                    GroupId = groupId,
                    TypeId = typeId,
                    InputValues = [null],
                    InputIndices = [graphParam.OutputIndex],
                    OutputIndices = outputIndices.ToArray(),
                };
            }

            if (surfaceNode.GroupArchetype.GroupID == CurvesGroupId && surfaceNode.Archetype.TypeID == FloatCurveTypeId)
            {
                var keyframes = new List<BezierCurve<float>.Keyframe>();
                for (int i = 0; i < (int)surfaceNode.Values[0]; i++)
                {
                    int idx = i * 4;
                    var keyframe = new BezierCurve<float>.Keyframe()
                    {
                        Time = (float)surfaceNode.Values[idx + 1],
                        Value = (float)surfaceNode.Values[idx + 2],
                        TangentIn = (float)surfaceNode.Values[idx + 3],
                        TangentOut = (float)surfaceNode.Values[idx + 4],
                    };
                    keyframes.Add(keyframe);
                }
                
                var curve = new BezierCurve<float>() { Keyframes = keyframes.ToArray() };
                return new ExpressionGraphNode()
                {
                    GroupId = groupId,
                    TypeId = typeId,
                    FloatCurve = curve,
                    InputValues = inputValues.ToArray(),
                    InputIndices = inputIndices.ToArray(),
                    OutputIndices = outputIndices.ToArray(),
                };
            }
            
            return new ExpressionGraphNode()
            {
                GroupId = groupId,
                TypeId = typeId,
                InputValues = inputValues.ToArray(),
                InputIndices = inputIndices.ToArray(),
                OutputIndices = outputIndices.ToArray(),
            };
        }
    }

    internal sealed class FakeSurfaceContext : ISurfaceContext
    {
        public Asset SurfaceAsset { get; }
        public string SurfaceName { get; }
        public byte[] SurfaceData { get; set; }
        public VisjectSurfaceContext ParentContext { get; }
        public void OnContextCreated(VisjectSurfaceContext context) { }
    }

    internal sealed class NodeVariableIndexLookup
    {
        private class OutputUsage
        {
            public int Index;
            public int UsageLeft;
        }
        
        private Dictionary<Int2, OutputUsage> _outputBoxes = new Dictionary<Int2, OutputUsage>();
        private HashSet<int> _usedIndices = new HashSet<int>();
        
        public int RegisterOutputBox(OutputBox box)
        {
            if (!box.HasAnyConnection) return -1;
            if (_outputBoxes.TryGetValue(GetBoxId(box), out var outputUsage))
                return outputUsage.Index;

            int freeIndex = GetFreeVariableIndex();
            _outputBoxes.Add(GetBoxId(box), new OutputUsage { Index = freeIndex, UsageLeft = box.Connections.Count });
            return freeIndex;
        }

        public int GetVariableIndex(InputBox box)
        {
            if (!box.HasAnyConnection) return -1;

            var outputBox = box.Connections[0];

            if (_outputBoxes.TryGetValue(GetBoxId(outputBox), out var outputUsage))
            {
                outputUsage.UsageLeft--;
                if (outputUsage.UsageLeft <= 0)
                {
                    RemoveOutputBox(outputBox);
                }
                return outputUsage.Index;
            }
            
            Debug.LogError("Output box has not registered.");
            return -1;
        }

        public int GetFreeVariableIndex()
        {
            int freeIndex = 0;
            while (true)
            {
                if (!_usedIndices.Contains(freeIndex))
                {
                    _usedIndices.Add(freeIndex);
                    return freeIndex;
                }
                ++freeIndex;
            }
        }

        private Int2 GetBoxId(Box box)
        {
            return new Int2((int)box.ParentNode.ID, box.ID);
        }

        private void RemoveOutputBox(Box box)
        {
            var id = GetBoxId(box);
            if (_outputBoxes.TryGetValue(id, out var outputUsage))
            {
                _outputBoxes.Remove(id);
                _usedIndices.Remove(outputUsage.Index);
            }
        }

        public void Reset()
        {
            _outputBoxes.Clear();
            _usedIndices.Clear();
        }
    }
}

