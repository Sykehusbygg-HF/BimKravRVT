using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BimkravRvt.Utils
{
    public static class Registry
    {
        public static T Read<T>(string keyName, string valueName, object defaultValue, bool LocalMachineOnly = false) where T : IConvertible
        {
            if (!LocalMachineOnly)
            {
                RegistryKey keyCU = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(keyName, false);
                if (keyCU != null)
                {
                    string[] subkeys = keyCU.GetValueNames();
                    if (subkeys.Contains(valueName))
                    {

                        object o = (T)Convert.ChangeType(keyCU.GetValue(valueName), typeof(T));
                        if (o != null)
                            return (T)o;
                        else
                        {
                            return ReadLM<T>(keyName, valueName, defaultValue);

                        }

                    }
                    else
                        return ReadLM<T>(keyName, valueName, defaultValue);
                }
                else
                    return ReadLM<T>(keyName, valueName, defaultValue);
            }
            else
            {
                return ReadLM<T>(keyName, valueName, defaultValue);
            }
        }
        private static T ReadLM<T>(string keyName, string valueName, object defaultValue) where T : IConvertible
        {
            RegistryKey keyLM = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(keyName, false);
            if (keyLM != null)
            {
                string[] subkeys = keyLM.GetValueNames();
                if (subkeys.Contains(valueName))
                {

                    object o = (T)Convert.ChangeType(keyLM.GetValue(valueName), typeof(T));
                    if (o != null)
                        return (T)o;
                    else
                        return (T)defaultValue;
                }
                else
                    return (T)defaultValue;
            }
            else
                return (T)defaultValue;
        }

        public static bool SaveString(string registryPath, string Name,  string value)
        {
            try
            {
                if (registryPath == null || registryPath == "" || string.IsNullOrEmpty(Name))
                {
                    return false;
                }
                RegistryKey registryPathKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(registryPath, true);
                if (registryPathKey == null)
                {
                    registryPathKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(registryPath, RegistryKeyPermissionCheck.ReadWriteSubTree);
                }
                registryPathKey.SetValue(Name, value, RegistryValueKind.String);
                return true;
            }
            catch
            {
                return false;
            }
        }
            public static bool Save(string registryPath = null, object o = null)
        {
            if (registryPath == null || registryPath == "")
            {
                return false;
            }
            FieldInfo[] fields = o.GetType().GetFields(BindingFlags.Static | BindingFlags.NonPublic);

            RegistryKey registryPathKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(registryPath, true);
            if (registryPathKey == null)
            {
                registryPathKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(registryPath, RegistryKeyPermissionCheck.ReadWriteSubTree);
            }
            string[] subkeys = registryPathKey.GetValueNames();
            foreach (FieldInfo field in fields)
            {
                try
                {
                    if (field.FieldType == typeof(string))
                    {
                        registryPathKey.SetValue(field.Name.Substring(1), field.GetValue(o).ToString(), RegistryValueKind.String);
                        continue;
                    }
                    else if (field.FieldType == typeof(int))
                    {
                        registryPathKey.SetValue(field.Name.Substring(1), field.GetValue(o).ToString(), RegistryValueKind.String);
                        continue;

                    }
                    else if (field.FieldType == typeof(bool))
                    {
                        registryPathKey.SetValue(field.Name.Substring(1), field.GetValue(o).ToString(), RegistryValueKind.String);
                        continue;
                    }
                    else if (field.FieldType == typeof(DateTime))
                    {
                        registryPathKey.SetValue(field.Name.Substring(1), field.GetValue(o).ToString(), RegistryValueKind.String);
                        continue;
                    }
                    else// (field.FieldType == typeof(object))
                    {
                        //try handle it as an generic object and look if there's some subfields in there.
                        //Load settings from Subfolder
                        //string subkeyPath = String.Format("{0}\\{1}", registryPath, field.Name.Substring(1));
                        //Save(subkeyPath, field.GetValue(o) as object);
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    continue;
                }

            }
            return true;
        }
    }
    
}
