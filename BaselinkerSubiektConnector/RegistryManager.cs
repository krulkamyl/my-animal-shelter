using Microsoft.Win32;


namespace BaselinkerSubiektConnector
{
    public class RegistryManager
    {
        private readonly string _registryKey;

        public RegistryManager()
        {
            _registryKey = "HKEY_CURRENT_USER\\Software\\BaselinkerSubiektIntegration";
        }

        public string GetValue(string name)
        {
            return Registry.GetValue(_registryKey, name, "") as string;
        }

        public void SetValue(string name, string value)
        {
            Registry.SetValue(_registryKey, name, value);
        }
    }
}
