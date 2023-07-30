namespace Skynet.Entities
{
    using System.Collections.Specialized;

    internal class LimitedOrderedDictionary : OrderedDictionary
    {
        new public void Add(object key, object? value)
        {
            if (Count > 1000)
            {
                RemoveAt(0);
            }

            base.Add(key, value);
        }
    }
}
