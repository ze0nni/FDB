namespace FDB.Editor
{

    public static class ListActions
    {
        public static void Swap(object list, int i0, int i1)
        {
            var items = list.GetType().GetProperty("Item");
            var t0 = items.GetValue(list, new object[] { i0 });
            var t1 = items.GetValue(list, new object[] { i1 });
            items.SetValue(list, t0, new object[] { i1 });
            items.SetValue(list, t1, new object[] { i0 });
        }

        public static void Insert(object list, int index, object item)
        {
            var insert = list.GetType().GetMethod("Insert");
            insert.Invoke(list, new object[] { index, item });
        }

        public static void RemoveAt(object list, int index)
        {
            var removeAt = list.GetType().GetMethod("RemoveAt");
            removeAt.Invoke(list, new object[] { index });
        }
    }

}