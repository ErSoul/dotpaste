namespace dotpaste
{
    public class ThreadSafeCounter
    {
        private int _count;

        public int Increment()
        {
            // Atomically increments the counter and returns the new value
            return Interlocked.Increment(ref _count);
        }

        public int Decrement()
        {
            // Atomically decrements the counter and returns the new value
            return Interlocked.Decrement(ref _count);
        }

        public int Value
        {
            get { return _count; }
        }
    }
}
