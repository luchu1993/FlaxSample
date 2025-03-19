using System;
using System.Collections.Generic;
using System.Linq;
using FlaxEngine;

namespace Game
{
    public class ExpressionGraphContext
    {
        public List<object> Variables;
        public Action<ExpressionGraphNode> ExecuteNode;
        public float XCoordinate;
    }
   
    public sealed partial class ExpressionGraph
    {
        /// <summary>
        /// Serialized visject surface
        /// </summary>
        public byte[] VisjectSurface { get; set; }
        
        private ExpressionGraphParameter[] _parameters;
        private ExpressionGraphNode[] _nodes;
        private ExpressionGraphNode _outputNode;
        private ExpressionGraphContext _context;

        private static readonly Dictionary<int, Action<ExpressionGraphNode>> NodeActions = new();
        private static readonly Random _rng = new Random();
        
        static ExpressionGraph()
        {
            AddAction(1, 1, (_) => { }); 
            AddAction(1, 2, (node) => { node.Return<float>(0,_rng.Next());});
            AddAction(1, 3, (node) => { node.Return<float>(0, node.Context.XCoordinate);});
            
            AddAction(3, 1, (node) => { node.Return<float>(0, node.InputAs<float>(0) + node.InputAs<float>(1)); });
            AddAction(3, 2, (node) => { node.Return<float>(0, node.InputAs<float>(0) - node.InputAs<float>(1)); });
            AddAction(3, 3, (node) => { node.Return<float>(0, node.InputAs<float>(0) * node.InputAs<float>(1)); });
            AddAction(3, 4, (node) => { node.Return<float>(0, node.InputAs<float>(0) % node.InputAs<float>(1)); });
            AddAction(3, 5, (node) => { node.Return<float>(0, node.InputAs<float>(0) / node.InputAs<float>(1)); });
            AddAction(3, 7, (node) => { node.Return<float>(0, Mathf.Abs(node.InputAs<float>(0))); });
            AddAction(3, 8, (node) => { node.Return<float>(0, Mathf.Ceil(node.InputAs<float>(0))); });
            AddAction(3, 9, (node) => { node.Return<float>(0, Mathf.Cos(node.InputAs<float>(0))); });
            AddAction(3, 10, (node) => { node.Return<float>(0, Mathf.Floor(node.InputAs<float>(0))); });
            AddAction(3, 15, (node) => { node.Return<float>(0, Mathf.Sin(node.InputAs<float>(0))); });
            
            AddAction(6, 1, (node) => { node.Return<float>(0, node.InputAs<float>(0));});
        }
        
        public ExpressionGraphParameter[] Parameters
        {
            get => _parameters;
            set => _parameters = value;
        }
        
        public ExpressionGraphNode[] Nodes
        {
            get => _nodes;
            set
            {
                _nodes = value;
                OnNodesSet();
            }
        }

        [NoSerialize] public float[] OutputFloats { get; private set; } = new float[100];

        public void Update()
        {
            if (Nodes == null || Nodes.Length <= 0) return;

            for (int i = 0; i < 100; ++i)
            {
                _context.XCoordinate = i;
                EvaluateOutput(i);
            }
        }

        private void EvaluateOutput(int index)
        {
            for (int i = 0; i < _context.Variables.Count; i++)
            {
                _context.Variables[i] = null;
            }

            for (int i = 0; i < Parameters.Length; i++)
            {
                Parameters[i].Execute(_context);
            }

            for (int i = 0; i < Nodes.Length; i++)
            {
                Nodes[i].Execute(_context);
            }

            OutputFloats[index] = _outputNode.InputAs<float>(0);
        }
        
        private void OnNodesSet()
        {
            if (Nodes == null || Nodes.Length <= 0)
            {
                _outputNode = null;
                _context = null;
            }
            else
            {
                int maxOutputIndex = 0;
                if (_parameters != null && _parameters.Length > 0)
                {
                    foreach (var parameter in _parameters)
                    {
                        maxOutputIndex = Math.Max(parameter.OutputIndex, maxOutputIndex);
                    }

                    foreach (var graphNode in _nodes)
                    {
                        if (graphNode.OutputIndices == null || graphNode.OutputIndices.Length <= 0)
                            continue;
                        foreach (var outputIndex in graphNode.OutputIndices)
                        {
                            if (outputIndex > maxOutputIndex)
                                maxOutputIndex = outputIndex;
                        }
                    }
                }

                _context = new ExpressionGraphContext()
                {
                    ExecuteNode = ExecuteNode,
                };
                _context.Variables = new List<object>(Enumerable.Repeat<object>(null, maxOutputIndex + 1));
                
                for (int i = 0; i < Nodes.Length; i++)
                {
                    var node = Nodes[i];
                    if (node.GroupId == 1 && node.TypeId == 1)
                    {
                        _outputNode = node;
                    }
                }
            }
        }

        private void ExecuteNode(ExpressionGraphNode node)
        {
            int actionKey = node.GroupId << 16 | node.TypeId;
            if (NodeActions.TryGetValue(actionKey, out Action<ExpressionGraphNode> action))
            {
                action.Invoke(node);
            }
            else
            {
                Debug.LogWarning($"Unknown node group ID: {node.GroupId}, type id: {node.TypeId}");
            }
        }

        private static void AddAction(int groupId, int typeId, Action<ExpressionGraphNode> action)
        {
            int actionKey = groupId << 16 | typeId;
            NodeActions[actionKey] = action;
        }
    }
}

