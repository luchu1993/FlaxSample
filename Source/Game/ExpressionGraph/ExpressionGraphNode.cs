using System;

namespace Game
{
    /// <summary>
    /// A generic graph node that can execute an action
    /// </summary>
    public class ExpressionGraphNode
    {
        public int GroupId;
        public int TypeId;
        public object[] InputValues;
        public int[] InputIndices;
        public int[] OutputIndices;

        public ExpressionGraphContext Context { get; set; }

        public void Execute(ExpressionGraphContext context)
        {
            Context = context;
            UpdateInputValues();
            Context.ExecuteNode(this);
        }

        private void UpdateInputValues()
        {
            if (InputValues == null || InputValues.Length <= 0)
                return;
            
            for (int i = 0; i < InputValues.Length; i++)
            {
                if (InputIndices[i] == -1) continue;
                InputValues[i] = Context.Variables[InputIndices[i]];
            }
        }

        public T InputAs<T>(int index)
        {
            return CastTo<T>(InputValues[index]);
        }
        
        private T CastTo<T>(object value)
        {
            if (value == null)
            {
                return default(T);
            }
            
            if (typeof(T) == typeof(float))
            {
                // Special handling for numbers
                // TODO: Replace this with something more efficient and/or better
                return (T)Convert.ChangeType(value, typeof(T));
            }
            
            if (value is T castedValue)
            {
                return castedValue;
            }
            
            return default(T);
        }
        
        public void Return<T>(int index, T returnValue)
        {
            Context.Variables[OutputIndices[index]] = returnValue;
        }
    }
}
