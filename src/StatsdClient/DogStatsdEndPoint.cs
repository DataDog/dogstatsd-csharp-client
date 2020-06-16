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

        /// <summary>
        /// AreEquals returns whether `this` and `endPoint` have the same values
        /// </summary>
        /// <param name="endPoint">The endpoint to compare with `this`</param>
        /// <returns>Returns whether `this` and `endPoint` have the same values</returns>
        public bool AreEquals(DogStatsdEndPoint endPoint)
        {
            return endPoint != null && this.Name == endPoint.Name && this.Port == endPoint.Port;
        }
    }
}