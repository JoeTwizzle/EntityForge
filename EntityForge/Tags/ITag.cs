using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityForge.Tags
{
    public interface ITag<T> where T : struct, ITag<T>
    {
        /// <summary>
        /// DO NOT MANUALLY ASSIGN
        /// </summary>
        internal static virtual int BitIndex { get; set; }
    }
}
