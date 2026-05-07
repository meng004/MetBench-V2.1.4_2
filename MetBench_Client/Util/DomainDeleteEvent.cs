using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetBench_Client.Util
{
    public class DomainDeleteEvent : EventArgs
    {
        /// <summary>
        /// 应用领域名称
        /// </summary>
        public string Name = string.Empty;
    }
}
