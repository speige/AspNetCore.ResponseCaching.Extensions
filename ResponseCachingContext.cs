// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace AspNetCore.ResponseCaching
{
    public class ResponseCachingContext
    {
        private DateTimeOffset? _responseDate;
        private bool _parsedResponseDate;
        private DateTimeOffset? _responseExpires;
        private bool _parsedResponseExpires;
        private TimeSpan? _responseSharedMaxAge;
        private bool _parsedResponseSharedMaxAge;
        private TimeSpan? _responseMaxAge;
        private bool _parsedResponseMaxAge;

        public ResponseCachingContext(HttpContext httpContext, ILogger logger)
        {
            HttpContext = httpContext;
            Logger = logger;
        }

        public HttpContext HttpContext { get; }

        public DateTimeOffset? ResponseTime { get; set; }

        public TimeSpan? CachedEntryAge { get; set; }

        public CachedVaryByRules CachedVaryByRules { get; set; }

        public ILogger Logger { get; }

        public bool ShouldCacheResponse { get; set; }

        public string BaseKey { get;  set; }

        public string StorageVaryKey { get;  set; }

        public TimeSpan CachedResponseValidFor { get;  set; }

        public CachedResponse CachedResponse { get;  set; }

        public bool ResponseStarted { get; set; }

        public Stream OriginalResponseStream { get; set; }

        public ResponseCachingStream ResponseCachingStream { get; set; }

        public IHeaderDictionary CachedResponseHeaders { get; set; }

        public DateTimeOffset? ResponseDate
        {
            get
            {
                if (!_parsedResponseDate)
                {
                    _parsedResponseDate = true;
                    DateTimeOffset date;
                    if (HeaderUtilities.TryParseDate(HttpContext.Response.Headers[HeaderNames.Date].ToString(), out date))
                    {
                        _responseDate = date;
                    }
                    else
                    {
                        _responseDate = null;
                    }
                }
                return _responseDate;
            }
            set
            {
                // Don't reparse the response date again if it's explicitly set
                _parsedResponseDate = true;
                _responseDate = value;
            }
        }

        public DateTimeOffset? ResponseExpires
        {
            get
            {
                if (!_parsedResponseExpires)
                {
                    _parsedResponseExpires = true;
                    DateTimeOffset expires;
                    if (HeaderUtilities.TryParseDate(HttpContext.Response.Headers[HeaderNames.Expires].ToString(), out expires))
                    {
                        _responseExpires = expires;
                    }
                    else
                    {
                        _responseExpires = null;
                    }
                }
                return _responseExpires;
            }
        }

        public TimeSpan? ResponseSharedMaxAge
        {
            get
            {
                if (!_parsedResponseSharedMaxAge)
                {
                    _parsedResponseSharedMaxAge = true;
                    HeaderUtilities.TryParseSeconds(HttpContext.Response.Headers[HeaderNames.CacheControl], CacheControlHeaderValue.SharedMaxAgeString, out _responseSharedMaxAge);
                }
                return _responseSharedMaxAge;
            }
        }

        public TimeSpan? ResponseMaxAge
        {
            get
            {
                if (!_parsedResponseMaxAge)
                {
                    _parsedResponseMaxAge = true;
                    HeaderUtilities.TryParseSeconds(HttpContext.Response.Headers[HeaderNames.CacheControl], CacheControlHeaderValue.MaxAgeString, out _responseMaxAge);
                }
                return _responseMaxAge;
            }
        }
    }
}
