using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSync
{
    class AppCore
    {
        private static AppCore mInstance = null;
        public static AppCore getInstance()
        {
            if (mInstance == null) mInstance = new AppCore();
            return mInstance;
        }
        public AppCore()
        {

        }
        private void init()
        {

        }
    }
}
