using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using Newtonsoft.Json;



namespace ConsoleApp1
{
	internal class Program
	{
		public static string SessionLogPath = @"D:\pslog\session.json";

		static void Main(string[] args)
		{

			if(File.Exists(SessionLogPath))
				File.Delete(SessionLogPath);

			var serverStarted = false;
			ProcessStartInfo startInfo = new ProcessStartInfo();
			startInfo.FileName = @"powershell.exe";
			startInfo.Arguments = @"& './startup.ps1'";
			startInfo.RedirectStandardOutput = true;
			startInfo.RedirectStandardError = true;
			startInfo.StandardErrorEncoding = System.Text.Encoding.UTF8;
			startInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;
			startInfo.UseShellExecute = false;
			//startInfo.CreateNoWindow = true;


			Process process = new Process();
			process.StartInfo = startInfo;

			//process.OutputDataReceived += OutputHandler;
			//process.ErrorDataReceived += ErrorHandler;
			//
			//void ErrorHandler(object sender, DataReceivedEventArgs e)
			//{
			//	if (null != e.Data)
			//		Console.WriteLine(e.Data);
			//}
			//
			//void OutputHandler(object sender, DataReceivedEventArgs e)
			//{
			//	if (null != e.Data)
			//	{
			//		Console.WriteLine(e.Data);
			//	}
			//}

			process.Start();

			//process.BeginErrorReadLine();
			//process.BeginOutputReadLine();


			SessionInfoResult sessionInfoObj = new SessionInfoResult();
			while (!serverStarted)
			{
				System.Threading.Thread.Sleep(10);
				Console.WriteLine("Waiting... Waiting... Waiting...");
				
				if (!File.Exists(SessionLogPath)) continue;

				sessionInfoObj = JsonConvert.DeserializeObject<SessionInfoResult>(File.ReadAllText(SessionLogPath));

				if (sessionInfoObj.status == "started")
				{
					Console.WriteLine("session status: " + sessionInfoObj.status);
					serverStarted = true;
				}
				else 
				{ 
					Console.WriteLine("session status: " + sessionInfoObj.status);
				}

				
			}

			Console.WriteLine("OK, server started, lets try to ping it...");

			Console.WriteLine("Session info:");
			Console.WriteLine("Status: " + sessionInfoObj.status);
			Console.WriteLine("languageServiceTransport: " + sessionInfoObj.languageServiceTransport);
			Console.WriteLine("debugServiceTransport: " + sessionInfoObj.debugServiceTransport);
			Console.WriteLine("languageServicePipeName: " + sessionInfoObj.languageServicePipeName);
			Console.WriteLine("debugServicePipeName: " + sessionInfoObj.debugServicePipeName);


			var stdInPipeName = sessionInfoObj.languageServicePipeName;
			var stdOutPipeName = sessionInfoObj.languageServicePipeName;

			var readerPipe = new NamedPipeClientStream(stdInPipeName);
			var writerPipe = new NamedPipeClientStream(stdOutPipeName);

			readerPipe.Connect();
			writerPipe.Connect();


			Console.ReadLine();




			Console.ReadLine();
			process.Kill();

		}
	}

	public class SessionInfoResult
	{
		public string status { get; set; }
		public string languageServiceTransport { get; set; }
		public string languageServicePipeName { get; set; }
		public string debugServiceTransport { get; set; }
		public string debugServicePipeName { get; set; }
	}

}

