using DebWorker.Lib.Deb;
using Microsoft.Build.Framework;

namespace DebWorker.Console
{
    internal static class Program
    {
        private static string m_PathToStartFolder = "D:/test";
        private static string m_PathToStartDeb = "sdp-gateway-1.0.0_1.0.6_amd64.deb";
        private static string m_Output = "output";
        private static string m_IniPath = "etc/opt/sdp/app_config.ini";
        private static string m_FinalName = "sdp-app";
    
        private static readonly Dictionary<string, string> m_Properties = new()
        {
            { "gateway_id", "175" },
            { "client_secret", "" },
            { "controller_url", "" },
        };

        public static void Main(string[] args)
        {
            var pathToOutput = Path.Combine(m_PathToStartFolder, m_Output);

            using (var s = File.Open(Path.Combine(m_PathToStartFolder, m_PathToStartDeb), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                var data = DebPackageReader.GetPayloadStream(s);
                DebPackageReader.unTAR(data, pathToOutput);
            }
            System.Console.WriteLine("Unpacked");
        
            ///////////////////////////////////////////////////////////////
        
            EditIni(Path.Combine(pathToOutput, m_IniPath));
        
            System.Console.WriteLine("Edited");
        
            ///////////////////////////////////////////////////////////////

            var debTask = new DebTask()
            {
                DebPath = Path.Combine(m_PathToStartFolder, m_FinalName + ".deb"),
                DebTarPath = Path.Combine(m_PathToStartFolder, m_FinalName + ".tar"),
                PublishDir = Path.Combine(m_PathToStartFolder, m_Output),
                Prefix = "",
                Content = Array.Empty<ITaskItem>(),
                DebTarXzPath = Path.Combine(m_PathToStartFolder, m_FinalName + ".tar.xz"),
                PackageName = $"{m_FinalName}.deb",
                Description = "",
                Maintainer = "",
                Version = "1.0.0",
                DebPackageArchitecture = "amd64",
            };

            debTask.Execute();
        
            System.Console.WriteLine("End");
        }
    
        private static void EditIni(string pathIni)
        {
            var lines = File.ReadAllLines(pathIni);
        
            for (var i = 0; i < lines.Length; i++)
            {
                var v = lines[i].Split(':');
                if (v.Length <= 1) continue;
                var key = v[0].Trim();
                var value = v[1].Trim();
                if (m_Properties.ContainsKey(key))
                {
                    lines[i] = $"{key}: {m_Properties[key]}";
                    System.Console.WriteLine(@"Added property: {0}: {1}", key, value);
                }
            }

            File.WriteAllLines(pathIni, lines); 
        }
    }
}