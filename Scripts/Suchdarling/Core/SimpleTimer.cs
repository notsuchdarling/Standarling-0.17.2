namespace Suchdarling.Core
{
    public class SimpleTimer
    {
        public DateTime DateTime { get; } = DateTime.UtcNow;

        public int Duration
        {
            get
            {
                TimeSpan timeSpan = DateTime.UtcNow - DateTime;
                return (int)timeSpan.TotalSeconds;
            }
        } 
    }
}
