using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetBench_Client.Util
{
    public class ApplicationAddEvent : EventArgs
    {
        /// <summary>
        /// 应用程序名称
        /// </summary>
        public string Name = string.Empty;
    }
}
