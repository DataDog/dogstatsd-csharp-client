namespace StatsdClient.Worker
{
    interface IAsynchronousWorkerHandler<T>
    {
        /// <summary>
        /// Called when a new value is ready to be handled by the worker.
        /// </summary> 
        void OnNewValue(T v);

        /// <summary>
        /// Called when the worker is waiting for new value to handle.
        /// </summary>
        /// <returns>Return true to make the worker in a sleep state, false otherwise.</returns>
        bool OnIdle();
    }
}