﻿using System;
using System.Linq;
using System.Text;

namespace Weingartner.Json.Migration.Common
{
    public class MethodParameter
    {
        public MethodParameter(SimpleType type)
        {
            Type = type;
        }

        public SimpleType Type { get; }
    }
}
