// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;

namespace AspNetCore.ResponseCaching
{
    public static class StringBuilderExtensions
    {
        public static StringBuilder AppendUpperInvariant(this StringBuilder builder, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                builder.EnsureCapacity(builder.Length + value.Length);
                for (var i = 0; i < value.Length; i++)
                {
                    builder.Append(char.ToUpperInvariant(value[i]));
                }
            }

            return builder;
        }
    }
}
