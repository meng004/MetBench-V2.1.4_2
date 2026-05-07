using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetBench_Client.Util
{
    public class DomainModifyEvent : EventArgs
    {
        /// <summary>
        /// 修改前的应用领域名称
        /// </summary>
        public string Name = string.Empty;

        /// <summary>
        /// 修改后的应用领域名称
        /// </summary>
        public string newName = string.Empty;
    }
}
