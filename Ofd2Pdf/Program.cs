using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Ofd2Pdf
{
    internal static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
            else
            {
                Converter converter = new Converter();

                bool hasFailed = false;

                // 参数可以是 OFD 文件，也可以是文件夹（递归扫描其中的 OFD 文件）
                var files = OfdScanner.Expand(args);
                if (files.Count == 0)
                {
                    Console.WriteLine("[Warning]: no .ofd files found");
                    Environment.Exit(1);
                }

                for (int i = 0; i < files.Count; i++)
                {
                    string file = files[i];
                    string PdfName = OfdScanner.GetPdfPath(file);

                    var result = converter.ConvertToPdf(file, PdfName);

                    if (result == ConvertResult.Failed)
                    {
                        Console.WriteLine("[Failed]: " + file);
                        hasFailed = true;
                    } else
                    {
                        Console.WriteLine("[Success]: " + file);
                    }
                }

                if (hasFailed)
                {
                    Environment.Exit(1);
                }
                else
                {
                    Environment.Exit(0);
                }
            }
        }
    }
}
