﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace AspNetCore.ResponseCaching
{
    /// <summary>
    /// Provides access to the normal system clock.
    /// </summary>
    public class SystemClock : ISystemClock
    {
        /// <summary>
        /// Retrieves the current system time in UTC.
        /// </summary>
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
