namespace Game
{
    public class ExpressionGraphParameter
    {
        public string Name;
        public int Index;
        public object Value;
        public int OutputIndex;

        public void Execute(ExpressionGraphContext context)
        {
            context.Variables[OutputIndex] = Value;
        }
    }
}
