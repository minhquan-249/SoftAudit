using System.IO;
using System.Text.Json;

namespace SoftAudit.Config
{
    public class RuleLoader
    {
        public static SoftwareRules Load(string path)
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<SoftwareRules>(json)
                   ?? new SoftwareRules();
        }
    }
}
