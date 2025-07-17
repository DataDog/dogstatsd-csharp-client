using System;
using System.Text.RegularExpressions;

namespace StatsdClient
{
    internal class OriginDetection
    {
        internal OriginDetection()
        {
            // Read the external data from the environment variable called `DD_EXTERNAL_ENV`.
            //
            // This is injected via admission controller, in Kubernetes environments, to provide origin detection information
            // for clients that cannot otherwise detect their origin automatically.
            var externalData = Environment.GetEnvironmentVariable("DD_EXTERNAL_ENV");
            Initialize(externalData);
        }

        internal OriginDetection(string externalData)
        {
            Initialize(externalData);
        }

        /// <summary>
        /// Gets the detected External Data configuration if it exists.
        /// </summary>
        internal string ExternalData { get; private set; }

        private void Initialize(string externalData)
        {
            // If we have external data, trim any leading or trailing whitespace, remove all non-printable characters, and remove all `|` characters.
            if (!string.IsNullOrEmpty(externalData))
            {
                ExternalData = Regex.Replace(externalData.Trim(), @"[\p{Cc}|]+", string.Empty);
            }
        }
    }
}
