namespace FDB.Editor
{
    public partial class ModelInspector<T>
    {
        struct InputState
        {
            public enum Target
            {
                Free,
                ResizeHeader
            }

            public Target Type;

            public string ResizePath;
            public float ResizeStartWidth;
            public float ResizeStartX;
        }
    }
}
