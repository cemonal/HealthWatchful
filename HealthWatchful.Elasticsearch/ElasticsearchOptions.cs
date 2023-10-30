using Elasticsearch.Net;
using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace HealthWatchful.Elasticsearch
{
    /// <summary>
    /// Represents configuration settings for Elasticsearch health checks.
    /// </summary>
    public class ElasticsearchOptions
    {
        public string Uri { get; private set; } = null;

        public string UserName { get; private set; }

        public string Password { get; private set; }

        public X509Certificate Certificate { get; private set; }

        public ApiKeyAuthenticationCredentials ApiKeyAuthenticationCredentials { get; set; }

        public bool AuthenticateWithBasicCredentials { get; private set; }

        public bool AuthenticateWithCertificate { get; private set; }

        public bool AuthenticateWithApiKey { get; private set; }

        public bool UseClusterHealthApi { get; set; }

        public Func<object, X509Certificate, X509Chain, SslPolicyErrors, bool> CertificateValidationCallback { get; private set; }

        public TimeSpan? RequestTimeout { get; set; }

        // <summary>
        /// Configures the health check to use basic authentication with the specified username and password.
        /// </summary>
        /// <param name="name">The username for basic authentication.</param>
        /// <param name="password">The password for basic authentication.</param>
        /// <returns>The current instance of ElasticsearchOptions.</returns>
        public ElasticsearchOptions UseBasicAuthentication(string name, string password)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name), "UserName cannot be null or whitespace!");

            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentNullException(nameof(password), "Password cannot be null or whitespace!");

            UserName = name;
            Password = password;

            Certificate = null;
            AuthenticateWithApiKey = false;
            AuthenticateWithCertificate = false;
            AuthenticateWithBasicCredentials = true;
            return this;
        }

        /// <summary>
        /// Configures the health check to use a client certificate for authentication.
        /// </summary>
        /// <param name="certificate">The X509 certificate for authentication.</param>
        /// <returns>The current instance of ElasticsearchOptions.</returns>
        public ElasticsearchOptions UseCertificate(X509Certificate certificate)
        {
            if (certificate == null) throw new ArgumentNullException(nameof(certificate));

            Certificate = certificate;

            UserName = string.Empty;
            Password = string.Empty;
            AuthenticateWithApiKey = false;
            AuthenticateWithBasicCredentials = false;
            AuthenticateWithCertificate = true;
            return this;
        }

        /// <summary>
        /// Configures the health check to use an API key for authentication.
        /// </summary>
        /// <param name="apiKey">The API key authentication credentials.</param>
        /// <returns>The current instance of ElasticsearchOptions.</returns>
        public ElasticsearchOptions UseApiKey(ApiKeyAuthenticationCredentials apiKey)
        {
            if (apiKey == null) throw new ArgumentNullException(nameof(apiKey));

            ApiKeyAuthenticationCredentials = apiKey;

            UserName = string.Empty;
            Password = string.Empty;
            Certificate = null;
            AuthenticateWithBasicCredentials = false;
            AuthenticateWithCertificate = false;
            AuthenticateWithApiKey = true;

            return this;
        }

        /// <summary>
        /// Configures the health check to use the specified Elasticsearch server URI.
        /// </summary>
        /// <param name="uri">The URI of the Elasticsearch server.</param>
        /// <returns>The current instance of ElasticsearchOptions.</returns>
        public ElasticsearchOptions UseServer(string uri)
        {
            if (string.IsNullOrWhiteSpace(uri))
                throw new ArgumentNullException(nameof(uri), "Uri cannot be null or whitespace!");

            Uri = uri;

            return this;
        }

        /// <summary>
        /// Configures the health check to use a custom certificate validation callback.
        /// </summary>
        /// <param name="callback">The custom certificate validation callback.</param>
        /// <returns>The current instance of ElasticsearchOptions.</returns>
        public ElasticsearchOptions UseCertificateValidationCallback(Func<object, X509Certificate, X509Chain, SslPolicyErrors, bool> callback)
        {
            CertificateValidationCallback = callback;
            return this;
        }
    }
}
