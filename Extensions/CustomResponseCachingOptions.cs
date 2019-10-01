using System;
using Microsoft.Extensions.Options;

namespace AspNetCore.ResponseCaching.Extensions
{
  public class CustomResponseCachingOptions
  {
    public Func<IOptions<ResponseCachingOptions>, IResponseCache> ResponseCacheFactory { get; set; }
  }
}