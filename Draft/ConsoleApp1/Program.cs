using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;



using Microsoft.PowerShell.EditorServices.Commands;
using Microsoft.PowerShell.EditorServices.Hosting;

namespace ConsoleApp1
{
	internal class Program
	{
		//private static Connection Connection = null;
		private static Process Process = null;

		static void Main(string[] args)
		{

			//var logger = new HostLogger(PsesLogLevel.Normal);
			//var config = new EditorServicesConfig()
			//var server = EditorServicesLoader.Create(logger, null,null);

			//server.LoadAndRunEditorServicesAsync();
			//var test = new StartEditorServicesCommand();
			//test.LanguageServiceInPipeName
			Console.WriteLine("halo");

			ProcessStartInfo info = new ProcessStartInfo();
			var location = Assembly.GetExecutingAssembly().Location;
			var directory = Path.GetDirectoryName(location);
			info.FileName = Path.Combine(directory, @"Microsoft.PowerShell.EditorServices.Host.x86.exe");
			info.Arguments = "ps2";
			info.RedirectStandardInput = true;
			info.RedirectStandardOutput = true;
			info.UseShellExecute = false;
			info.CreateNoWindow = true;

			

			Process = new Process();
			Process.StartInfo = info;


			Process.OutputDataReceived += OutputHandler;
			Process.ErrorDataReceived += ErrorHandler;

			void ErrorHandler(object sender, DataReceivedEventArgs e)
			{
				if (null != e.Data)
					Console.WriteLine(e.Data);
			}

			void OutputHandler(object sender, DataReceivedEventArgs e)
			{
				if (null != e.Data)
				{
					Console.WriteLine(e.Data);
				}
			}


			if (Process.Start())
			{
				//Connection = new Connection(Process.StandardOutput.BaseStream, Process.StandardInput.BaseStream);

				Process.StandardInput.WriteLine("{test: 123}");
			}


			Console.WriteLine("byby");
			Console.ReadLine();

		}
	}
}
