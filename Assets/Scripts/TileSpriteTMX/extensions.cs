﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TileSpriteTMX
{
    static class Extensions
    {
        public static void Each<T>(this IEnumerable<T> ie, Action<T, int> action)
        {
            var i = 0;
            foreach (var e in ie) action(e, i++);
        }
    }


}