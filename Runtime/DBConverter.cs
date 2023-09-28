namespace FDB
{
    public partial class DBConverter<T>
    {
        private readonly DBResolver _resolver;

        public DBConverter(DBResolver resolver)
        {
            _resolver = resolver;
        }
    }
}