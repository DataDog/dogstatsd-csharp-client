namespace StatsdClient
{
    /// <summary>
    /// DogStatsdEndPoint is a DogStatsd endpoint (UDP or UDS).
    /// </summary>
    public class DogStatsdEndPoint
    {
        /// <summary>
        /// Gets or sets a value defining the name of the endpoint
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a value defining the port number if any
        /// </summary>
        public int Port { get; set; }
    }
}