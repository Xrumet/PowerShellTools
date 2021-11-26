using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Threading;

using OmniSharp.Extensions.LanguageServer.Protocol.Models;

using Nerdbank.Streams;
using Newtonsoft.Json;
using StreamJsonRpc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp2
{
	internal class Program
	{
		public static string SessionLogPath = @"D:\pslog\session.json";
		public static Connection connection;
		public static bool serverStarted = false;

		static async Task Main(string[] args)
		{
			if (File.Exists(SessionLogPath))
				File.Delete(SessionLogPath);

			Process process = StartServer();
			WaitTillServerStarted();

			var stdioStream = FullDuplexStream.Splice(process.StandardOutput.BaseStream, process.StandardInput.BaseStream);

			Console.WriteLine("Connected. Sending request...");
			//OmniSharp.Extensions.LanguageServer.Protocol.Models.InternalInitializeParams
			
			using (var jsonRpc = JsonRpc.Attach(stdioStream)) 
			{
				Microsoft.VisualStudio.LanguageServer.Protocol.InitializeParams parameters = new Microsoft.VisualStudio.LanguageServer.Protocol.InitializeParams() 
				{ 
					RootUri = new Uri(@"D:\Git\SetupDevEnv"),
					ProcessId = process.Id,
					Trace = Microsoft.VisualStudio.LanguageServer.Protocol.TraceSetting.Verbose,
					Capabilities = new Microsoft.VisualStudio.LanguageServer.Protocol.ClientCapabilities()
				};

				InitializeParams p2 = new InitializeParams() 
				{
					WorkspaceFolders = new List<WorkspaceFolder>() {
						new WorkspaceFolder() {
							Name = "MyProj",
							Uri = OmniSharp.Extensions.LanguageServer.Protocol.DocumentUri.From(new Uri(@"D:\Git\SetupDevEnv"))
						}
					},
					Trace = InitializeTrace.Verbose
				};
				
				//@"D:\Git\SetupDevEnv"
				//https://github.com/OmniSharp/csharp-language-server-protocol
				//OmniSharp.Extensions.JsonRpc.Client client = new OmniSharp.Extensions.JsonRpc.Client()

				object sum = await jsonRpc.InvokeAsync<object>("initialize", p2);
				Console.WriteLine($"3 + 5 = {sum}");
			}

			
		}

		static Process StartServer() 
		{
			ProcessStartInfo startInfo = new ProcessStartInfo();
			startInfo.FileName = @"powershell.exe";
			startInfo.Arguments = @"& './startup.ps1'";

			startInfo.RedirectStandardInput = true;
			startInfo.RedirectStandardOutput = true;
			startInfo.RedirectStandardError = true;

			startInfo.StandardErrorEncoding = System.Text.Encoding.UTF8;
			startInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;

			startInfo.UseShellExecute = false;
			//startInfo.CreateNoWindow = true;


			Process process = new Process();
			process.StartInfo = startInfo;

			process.Start();

			//process.BeginErrorReadLine();
			//process.BeginOutputReadLine();
			
			return process;

		}
		static void WaitTillServerStarted() 
		{
			SessionInfoResult sessionInfoObj = new SessionInfoResult();
			while (!serverStarted)
			{
				System.Threading.Thread.Sleep(1000);
				//Console.WriteLine("Waiting... Waiting... Waiting...");

				if (!File.Exists(SessionLogPath)) continue;

				sessionInfoObj = JsonConvert.DeserializeObject<SessionInfoResult>(File.ReadAllText(SessionLogPath));

				if (sessionInfoObj.status == "started")
				{
					//Console.WriteLine("session status: " + sessionInfoObj.status);
					serverStarted = true;
				}
				else
				{
					//Console.WriteLine("session status: " + sessionInfoObj.status);
				}


			}

			//Console.WriteLine("OK, server started, lets try to ping it...");
			//Console.WriteLine("Session info:");
			//Console.WriteLine("Status: " + sessionInfoObj.status);
			//Console.WriteLine("languageServiceTransport: " + sessionInfoObj.languageServiceTransport);
			//Console.WriteLine("debugServiceTransport: " + sessionInfoObj.debugServiceTransport);
			//Console.WriteLine("languageServicePipeName: " + sessionInfoObj.languageServicePipeName);
			//Console.WriteLine("debugServicePipeName: " + sessionInfoObj.debugServicePipeName);

		}


	}

	public class Client : ILanguageClient
	{
		public string Name => throw new NotImplementedException();

		public IEnumerable<string> ConfigurationSections => throw new NotImplementedException();

		public object InitializationOptions => throw new NotImplementedException();

		public IEnumerable<string> FilesToWatch => throw new NotImplementedException();

		public bool ShowNotificationOnInitializeFailed => throw new NotImplementedException();

		public event AsyncEventHandler<EventArgs> StartAsync;
		public event AsyncEventHandler<EventArgs> StopAsync;

		public Task<Connection> ActivateAsync(CancellationToken token)
		{
			throw new NotImplementedException();
		}

		public Task OnLoadedAsync()
		{
			throw new NotImplementedException();
		}

		public Task OnServerInitializedAsync()
		{
			throw new NotImplementedException();
		}

		public Task<InitializationFailureContext> OnServerInitializeFailedAsync(ILanguageClientInitializationInfo initializationState)
		{
			throw new NotImplementedException();
		}
	}
	//public class Client : ILanguageClientCustomMessage2
	//{
	//	public object MiddleLayer => throw new NotImplementedException();
	//
	//	public object CustomMessageTarget => throw new NotImplementedException();
	//
	//	public Task AttachForCustomMessageAsync(JsonRpc rpc)
	//	{
	//		throw new NotImplementedException();
	//	}
	//}

	public class SessionInfoResult
	{
		public string status { get; set; }
		public string languageServiceTransport { get; set; }
		public string languageServicePipeName { get; set; }
		public string debugServiceTransport { get; set; }
		public string debugServicePipeName { get; set; }
	}


}
