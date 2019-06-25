// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.ResponseCaching
{
    internal class ResponseCachingPolicyProvider : IResponseCachingPolicyProvider
    {
        public virtual bool AttemptResponseCaching(ResponseCachingContext context)
        {
            var request = context.HttpContext.Request;

            // Verify the method
            if (!HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method))
            {
                context.Logger.RequestMethodNotCacheable(request.Method);
                return false;
            }

            // Verify existence of authorization headers
            if (!StringValues.IsNullOrEmpty(request.Headers[HeaderNames.Authorization]))
            {
                context.Logger.RequestWithAuthorizationNotCacheable();
                return false;
            }

            return true;
        }

        public virtual bool AllowCacheLookup(ResponseCachingContext context)
        {
            var requestHeaders = context.HttpContext.Request.Headers;

            // Verify request cache-control parameters
            if (!StringValues.IsNullOrEmpty(requestHeaders[HeaderNames.CacheControl]))
            {
                if (HeaderUtilities.ContainsCacheDirective(requestHeaders[HeaderNames.CacheControl], CacheControlHeaderValue.NoCacheString))
                {
                    context.Logger.RequestWithNoCacheNotCacheable();
                    return false;
                }
            }
            else
            {
                // Support for legacy HTTP 1.0 cache directive
                if (HeaderUtilities.ContainsCacheDirective(requestHeaders[HeaderNames.Pragma], CacheControlHeaderValue.NoCacheString))
                {
                    context.Logger.RequestWithPragmaNoCacheNotCacheable();
                    return false;
                }
            }

            return true;
        }

        public virtual bool AllowCacheStorage(ResponseCachingContext context)
        {
            // Check request no-store
            return !HeaderUtilities.ContainsCacheDirective(context.HttpContext.Request.Headers[HeaderNames.CacheControl], CacheControlHeaderValue.NoStoreString);
        }

        public virtual bool IsResponseCacheable(ResponseCachingContext context)
        {
            var responseCacheControlHeader = context.HttpContext.Response.Headers[HeaderNames.CacheControl];

            // Only cache pages explicitly marked with public
            if (!HeaderUtilities.ContainsCacheDirective(responseCacheControlHeader, CacheControlHeaderValue.PublicString))
            {
                context.Logger.ResponseWithoutPublicNotCacheable();
                return false;
            }

            // Check response no-store
            if (HeaderUtilities.ContainsCacheDirective(responseCacheControlHeader, CacheControlHeaderValue.NoStoreString))
            {
                context.Logger.ResponseWithNoStoreNotCacheable();
                return false;
            }

            // Check no-cache
            if (HeaderUtilities.ContainsCacheDirective(responseCacheControlHeader, CacheControlHeaderValue.NoCacheString))
            {
                context.Logger.ResponseWithNoCacheNotCacheable();
                return false;
            }

            var response = context.HttpContext.Response;

            // Do not cache responses with Set-Cookie headers
            if (!StringValues.IsNullOrEmpty(response.Headers[HeaderNames.SetCookie]))
            {
                context.Logger.ResponseWithSetCookieNotCacheable();
                return false;
            }

            // Do not cache responses varying by *
            var varyHeader = response.Headers[HeaderNames.Vary];
            if (varyHeader.Count == 1 && string.Equals(varyHeader, "*", StringComparison.OrdinalIgnoreCase))
            {
                context.Logger.ResponseWithVaryStarNotCacheable();
                return false;
            }

            // Check private
            if (HeaderUtilities.ContainsCacheDirective(responseCacheControlHeader, CacheControlHeaderValue.PrivateString))
            {
                context.Logger.ResponseWithPrivateNotCacheable();
                return false;
            }

            // Check response code
            if (response.StatusCode != StatusCodes.Status200OK)
            {
                context.Logger.ResponseWithUnsuccessfulStatusCodeNotCacheable(response.StatusCode);
                return false;
            }

            // Check response freshness
            if (!context.ResponseDate.HasValue)
            {
                if (!context.ResponseSharedMaxAge.HasValue &&
                    !context.ResponseMaxAge.HasValue &&
                    context.ResponseTime.Value >= context.ResponseExpires)
                {
                    context.Logger.ExpirationExpiresExceeded(context.ResponseTime.Value, context.ResponseExpires.Value);
                    return false;
                }
            }
            else
            {
                var age = context.ResponseTime.Value - context.ResponseDate.Value;

                // Validate shared max age
                if (age >= context.ResponseSharedMaxAge)
                {
                    context.Logger.ExpirationSharedMaxAgeExceeded(age, context.ResponseSharedMaxAge.Value);
                    return false;
                }
                else if (!context.ResponseSharedMaxAge.HasValue)
                {
                    // Validate max age
                    if (age >= context.ResponseMaxAge)
                    {
                        context.Logger.ExpirationMaxAgeExceeded(age, context.ResponseMaxAge.Value);
                        return false;
                    }
                    else if (!context.ResponseMaxAge.HasValue)
                    {
                        // Validate expiration
                        if (context.ResponseTime.Value >= context.ResponseExpires)
                        {
                            context.Logger.ExpirationExpiresExceeded(context.ResponseTime.Value, context.ResponseExpires.Value);
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        public virtual bool IsCachedEntryFresh(ResponseCachingContext context)
        {
            var age = context.CachedEntryAge.Value;
            var cachedCacheControlHeaders = context.CachedResponseHeaders[HeaderNames.CacheControl];
            var requestCacheControlHeaders = context.HttpContext.Request.Headers[HeaderNames.CacheControl];

            // Add min-fresh requirements
            TimeSpan? minFresh;
            if (HeaderUtilities.TryParseSeconds(requestCacheControlHeaders, CacheControlHeaderValue.MinFreshString, out minFresh))
            {
                age += minFresh.Value;
                context.Logger.ExpirationMinFreshAdded(minFresh.Value);
            }

            // Validate shared max age, this overrides any max age settings for shared caches
            TimeSpan? cachedSharedMaxAge;
            HeaderUtilities.TryParseSeconds(cachedCacheControlHeaders, CacheControlHeaderValue.SharedMaxAgeString, out cachedSharedMaxAge);

            if (age >= cachedSharedMaxAge)
            {
                // shared max age implies must revalidate
                context.Logger.ExpirationSharedMaxAgeExceeded(age, cachedSharedMaxAge.Value);
                return false;
            }
            else if (!cachedSharedMaxAge.HasValue)
            {
                TimeSpan? requestMaxAge;
                HeaderUtilities.TryParseSeconds(requestCacheControlHeaders, CacheControlHeaderValue.MaxAgeString, out requestMaxAge);

                TimeSpan? cachedMaxAge;
                HeaderUtilities.TryParseSeconds(cachedCacheControlHeaders, CacheControlHeaderValue.MaxAgeString, out cachedMaxAge);

                var lowestMaxAge = cachedMaxAge < requestMaxAge ? cachedMaxAge : requestMaxAge ?? cachedMaxAge;
                // Validate max age
                if (age >= lowestMaxAge)
                {
                    // Must revalidate or proxy revalidate
                    if (HeaderUtilities.ContainsCacheDirective(cachedCacheControlHeaders, CacheControlHeaderValue.MustRevalidateString)
                        || HeaderUtilities.ContainsCacheDirective(cachedCacheControlHeaders, CacheControlHeaderValue.ProxyRevalidateString))
                    {
                        context.Logger.ExpirationMustRevalidate(age, lowestMaxAge.Value);
                        return false;
                    }

                    TimeSpan? requestMaxStale;
                    var maxStaleExist = HeaderUtilities.ContainsCacheDirective(requestCacheControlHeaders, CacheControlHeaderValue.MaxStaleString);
                    HeaderUtilities.TryParseSeconds(requestCacheControlHeaders, CacheControlHeaderValue.MaxStaleString, out requestMaxStale);

                    // Request allows stale values with no age limit
                    if (maxStaleExist && !requestMaxStale.HasValue)
                    {
                        context.Logger.ExpirationInfiniteMaxStaleSatisfied(age, lowestMaxAge.Value);
                        return true;
                    }

                    // Request allows stale values with age limit
                    if (requestMaxStale.HasValue && age - lowestMaxAge < requestMaxStale)
                    {
                        context.Logger.ExpirationMaxStaleSatisfied(age, lowestMaxAge.Value, requestMaxStale.Value);
                        return true;
                    }

                    context.Logger.ExpirationMaxAgeExceeded(age, lowestMaxAge.Value);
                    return false;
                }
                else if (!cachedMaxAge.HasValue && !requestMaxAge.HasValue)
                {
                    // Validate expiration
                    DateTimeOffset expires;
                    if (HeaderUtilities.TryParseDate(context.CachedResponseHeaders[HeaderNames.Expires].ToString(), out expires) &&
                        context.ResponseTime.Value >= expires)
                    {
                        context.Logger.ExpirationExpiresExceeded(context.ResponseTime.Value, expires);
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
