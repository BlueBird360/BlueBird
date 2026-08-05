using System;

namespace BlueBird.Http.Logging
{
    /// <summary>
    /// Specifies which optional parts of an HTTP request or response should be captured for logging.
    /// </summary>
    [Flags]
    public enum HttpLogFields
    {
        /// <summary>
        /// Capture no optional fields.
        /// </summary>
        None = 0,

        /// <summary>
        /// Capture the HTTP message headers, excluding content headers.
        /// </summary>
        Headers = 0x0001,

        /// <summary>
        /// Capture the HTTP content headers.
        /// </summary>
        ContentHeaders = 0x0002,

        /// <summary>
        /// Capture the HTTP content body.
        /// </summary>
        Content = 0x0004,

        /// <summary>
        /// Capture all optional fields.
        /// </summary>
        All = Headers | ContentHeaders | Content,
    }
}
