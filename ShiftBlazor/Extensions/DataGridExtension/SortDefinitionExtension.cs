﻿using MudBlazor;
using ShiftSoftware.ShiftBlazor.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Collections.Generic
{
    public static class SortDefinitionExtension
    {
        public static IEnumerable<string> ToODataFilter<T>(this ICollection<SortDefinition<T>> sortDefinitions)
        {
            return GetFilterList(sortDefinitions);
        }

        public static IEnumerable<string> ToODataFilter<T>(this IEnumerable<SortDefinition<T>> sortDefinitions)
        {
            return GetFilterList(sortDefinitions);
        }

        private static IEnumerable<string> GetFilterList<T>(IEnumerable<SortDefinition<T>> sortDefinitions)
        {
            return sortDefinitions
                    .OrderBy(x => x.Index)
                    .Select(x => Misc.GetFieldFromPropertyPath(x.SortBy) +  (x.Descending ? " desc" : ""));
        }
    }
}
