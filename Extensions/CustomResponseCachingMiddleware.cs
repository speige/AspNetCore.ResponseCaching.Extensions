using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AspNetCore.ResponseCaching.Extensions
{
	public class CustomResponseCachingMiddleware : ResponseCachingMiddleware
  {
    public CustomResponseCachingMiddleware(RequestDelegate next, IOptions<ResponseCachingOptions> responseCachingOptions, IOptions<CustomResponseCachingOptions> overridableResponseCachingOptions, ILoggerFactory loggerFactory, IResponseCachingPolicyProvider policyProvider, IResponseCachingKeyProvider keyProvider) : base(next, responseCachingOptions, loggerFactory, policyProvider, overridableResponseCachingOptions.Value.ResponseCacheFactory(responseCachingOptions), keyProvider)
    {
    }
  }
}
