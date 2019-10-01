﻿using Microsoft.AspNetCore.ResponseCaching;

namespace AspNetCore.ResponseCaching.Extensions
{
  public class IgnoreBrowserNoCacheNoStoreResponseCachingPolicyProvider : ResponseCachingPolicyProvider
  {
    public override bool AllowCacheLookup(ResponseCachingContext context)
    {
      return true;
    }

    public override bool AllowCacheStorage(ResponseCachingContext context)
    {
      return true;
    }
  }
}