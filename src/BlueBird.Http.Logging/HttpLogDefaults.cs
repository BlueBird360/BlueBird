using System;
using System.Net.Http;

namespace BlueBird.Http.Logging
{
    internal static class HttpLogDefaults
    {
        private const string RedactedValue = "[REDACTED]";

        public static string RedactSensitiveHeader(string headerName, string value)
        {
            return IsSensitiveHeader(headerName) ? RedactedValue : value;
        }

        public static string RedactRequestUri(Uri requestUri)
        {
            if (requestUri.IsAbsoluteUri)
            {
                return requestUri.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.UriEscaped);
            }

            string uri = requestUri.OriginalString;
            int queryIndex = uri.IndexOf('?');
            int fragmentIndex = uri.IndexOf('#');
            int separatorIndex;

            if (queryIndex < 0)
            {
                separatorIndex = fragmentIndex;
            }
            else if (fragmentIndex < 0)
            {
                separatorIndex = queryIndex;
            }
            else
            {
                separatorIndex = Math.Min(queryIndex, fragmentIndex);
            }

            return separatorIndex < 0 ? uri : uri.Substring(0, separatorIndex);
        }

        public static bool IsTextContent(HttpContent content)
        {
            string? mediaType = content.Headers.ContentType?.MediaType;
            if (mediaType == null)
            {
                return false;
            }

            if (mediaType.StartsWith("text/", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (mediaType.EndsWith("+json", StringComparison.OrdinalIgnoreCase) ||
                mediaType.EndsWith("+xml", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return mediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase)
                || mediaType.Equals("application/xml", StringComparison.OrdinalIgnoreCase)
                || mediaType.Equals("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase)
                || mediaType.Equals("application/javascript", StringComparison.OrdinalIgnoreCase)
                || mediaType.Equals("application/graphql", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSensitiveHeader(string headerName)
        {
            return headerName.Equals("Authorization", StringComparison.OrdinalIgnoreCase)
                || headerName.Equals("Proxy-Authorization", StringComparison.OrdinalIgnoreCase)
                || headerName.Equals("Cookie", StringComparison.OrdinalIgnoreCase)
                || headerName.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase)
                || headerName.Equals("Authentication-Info", StringComparison.OrdinalIgnoreCase)
                || headerName.Equals("Proxy-Authentication-Info", StringComparison.OrdinalIgnoreCase)
                || headerName.Equals("Api-Key", StringComparison.OrdinalIgnoreCase)
                || headerName.Equals("X-Api-Key", StringComparison.OrdinalIgnoreCase)
                || headerName.Equals("X-Auth-Token", StringComparison.OrdinalIgnoreCase)
                || headerName.Equals("X-Access-Token", StringComparison.OrdinalIgnoreCase)
                || headerName.Equals("X-Amz-Security-Token", StringComparison.OrdinalIgnoreCase)
                || headerName.Equals("X-Goog-Api-Key", StringComparison.OrdinalIgnoreCase);
        }
    }
}
