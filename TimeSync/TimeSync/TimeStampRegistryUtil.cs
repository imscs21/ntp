using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.Data.SQLite;
using System.Collections;
using System.Threading;
namespace TimeSync
{
    class TimeStampRegistryUtil
    {
        private static TimeStampRegistryUtil mInstance = null;
        public static TimeStampRegistryUtil getInstance()
        {
            if (mInstance == null) mInstance = new TimeStampRegistryUtil();
            return mInstance;
        }
        public TimeStampRegistryUtil()
        {
            init();
        }
        public void onDestroy()
        {
            reg_semaphore.WaitOne();
            
            reg.Flush();
            reg.Close();
            reg = null;
            reg_semaphore.Release();
        }
        private System.Threading.Semaphore reg_semaphore;
        public bool SetTimeUpdatePeriod(uint sec)
        {
            bool rst = false;
            reg_semaphore.WaitOne();
            if (reg != null)
            {
                try
                {
                    reg.SetValue("time_update_period", sec, RegistryValueKind.DWord);
                    reg.Flush();
                    rst = true;
                }catch(Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            reg_semaphore.Release();
            return rst;
        }
        public String getLog()
        {
            String rst = "";
            rst+="<<GetSubKeyNames>>"+"\n";
            foreach (var subkey in reg.GetSubKeyNames())
            {
                rst += "\t" + subkey+"\n";
                //Console.WriteLine("\t" + subkey);
            }
            rst += "\n";
            //Console.WriteLine();
            rst+="<<GetValueNames>>"+reg.GetValueNames().Length+"\n";
            foreach (var subkey in reg.GetValueNames())
            {
                rst += String.Format("\t'{0}'={1}\n", subkey, reg.GetValue(subkey));
                //Console.WriteLine();
            }
            rst += reg.ToString();
            return rst;
        }
        public uint GetTimeUpdatePeriod()
        {
            uint rst = 0;
            reg_semaphore.WaitOne();
            if (reg != null)
            {
                
                //Console.WriteLine(Array.)
                object tmp = reg.GetValue("time_update_period");
                if (tmp!=null)
                {
                    rst = Convert.ToUInt32(tmp);
                }
                else
                {
                    rst =  30 * 60;
                    reg_semaphore.Release();
                    SaveTimeStamp(rst);
                    reg_semaphore.WaitOne();
                }
            }
            reg_semaphore.Release();
            return rst;
        }
        public ulong GetSavedTimeStamp()
        {
            ulong rst = 1585211880;
            reg_semaphore.WaitOne();
            if (reg != null) {
                if (reg.GetValueNames().Contains("last_saved_timestamp"))
                    rst = Convert.ToUInt64(reg.GetValue("last_saved_timestamp", 0, RegistryValueOptions.DoNotExpandEnvironmentNames));
                else {
                    reg_semaphore.Release();
                    SaveTimeStamp(rst);
                    reg_semaphore.WaitOne();
                }
            }
            reg_semaphore.Release();
            return rst;
            //return (ulong)reg.GetValue("last_saved_timestamp",0,RegistryValueOptions.DoNotExpandEnvironmentNames);
        }
        public void SaveTimeStamp(ulong ts)
        {
            
            try
            {
                if (reg.GetValueNames().Contains("last_saved_timestamp"))
                {
                    
                    ulong bef = GetSavedTimeStamp();
                    reg_semaphore.WaitOne();
                    if (bef < ts)
                    {
                        reg.SetValue("last_saved_timestamp", ts, RegistryValueKind.QWord);
                        reg.Flush();
                    }
                    reg_semaphore.Release();
                }
                else
                {
                    reg_semaphore.WaitOne();
                    reg.SetValue("last_saved_timestamp", ts, RegistryValueKind.QWord);
                    reg.Flush();
                    reg_semaphore.Release();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            
        }
        RegistryKey reg;
        private void init()
        {
            reg = Registry.LocalMachine.CreateSubKey("Software").CreateSubKey("HshTimeSync");
            //Registry.
            reg_semaphore = new System.Threading.Semaphore(1, 1);

        }
    }
}
