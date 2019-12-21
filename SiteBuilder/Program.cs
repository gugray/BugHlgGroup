using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using dotless.Core;
using dotless.Core.configuration;
using System.IO;

namespace SiteBuilder
{
    class Program
    {
        static GroupData data;
        static Builder builder;
        static FileSystemWatcher watcher;

        static void buildStyle()
        {
            DotlessConfiguration conf = new DotlessConfiguration
            {
                MinifyOutput = true,
            };
            using (var sr = new StreamReader("./src/style.less"))
            using (var sw = new StreamWriter("../_www/style.css"))
            {
                sw.NewLine = "\n";
                string less = sr.ReadToEnd();
                var css = Less.Parse(less, conf);
                sw.WriteLine(css);
            }
        }

        static void onSourceChanged(object sender, FileSystemEventArgs e)
        {
            if (!e.FullPath.EndsWith(".html") && !e.FullPath.EndsWith(".less")) return;
            builder = new Builder(data);
            builder.Build();
            buildStyle();
            Console.WriteLine("Rebuilt site.");
        }


        static void Main(string[] args)
        {
            try
            {
                bool serve = false;
                if (args.Length > 0 && args[0] == "serve") serve = true;
                data = new GroupData("../_data");
                builder = new Builder(data);
                builder.Build();
                buildStyle();
                if (!serve) return;
                watcher = new FileSystemWatcher("./src");
                watcher.IncludeSubdirectories = true;
                watcher.Changed += onSourceChanged;
                watcher.Created += onSourceChanged;
                watcher.Renamed += onSourceChanged;
                watcher.EnableRaisingEvents = true;
                Console.WriteLine("Watching. Press Ctrl+C to quit.");
                while (true) Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            Console.ReadLine();
        }
    }
}
