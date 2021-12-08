
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Client;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Window;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;
using System.Linq;
using Newtonsoft.Json.Linq;
using MediatR;
using Microsoft.Extensions.Logging;
using System.IO.Pipelines;
using System.IO.Pipes;
using System.Security.Principal;
using System.Security.AccessControl;
using StreamJsonRpc;

namespace ConsoleApp2
{
	internal class Program
	{
		public static string SessionLogPath = @"D:\pslog\session.json";
		public static bool serverStarted = false;

		public static readonly ILoggerFactory LoggerFactory = new LoggerFactory();


		static async Task Main(string[] args)
		{
			if (File.Exists(SessionLogPath))
				File.Delete(SessionLogPath);

			Process process = StartServer();

			var sessionInfoObj = WaitTillServerStarted();

			var logger = LoggerFactory.CreateLogger<Program>();

			var parts = sessionInfoObj.languageServicePipeName.Split('\\').ToList();
			var serverName = parts[2];
			var pipeName = parts[4];
			NamedPipeClientStream inoutPipeClient = new NamedPipeClientStream(serverName, pipeName, PipeDirection.InOut, System.IO.Pipes.PipeOptions.WriteThrough);
			await inoutPipeClient.ConnectAsync(3000);

			//var inparts = sessionInfoObj.languageServiceReadPipeName.Split('\\').ToList();
			//var inserverName = inparts[2];
			//var inpipeName = inparts[4];
			//
			//var outparts = sessionInfoObj.languageServiceWritePipeName.Split('\\').ToList();
			//var outserverName = outparts[2];
			//var outpipeName = outparts[4];
			//
			//NamedPipeClientStream inPipeClient = new NamedPipeClientStream(inserverName, inpipeName, PipeDirection.In);
			//NamedPipeClientStream outPipeClient = new NamedPipeClientStream(outserverName, outpipeName, PipeDirection.Out, System.IO.Pipes.PipeOptions.WriteThrough);
			//
			//await inPipeClient.ConnectAsync();
			//
			//System.Security.Principal.SecurityIdentifier sid = new System.Security.Principal.SecurityIdentifier(System.Security.Principal.WellKnownSidType.BuiltinUsersSid, null);
			//
			//PipeSecurity ps = new PipeSecurity();
			//PipeAccessRule par = new PipeAccessRule(sid, PipeAccessRights.ReadWrite, System.Security.AccessControl.AccessControlType.Allow);
			//ps.AddAccessRule(par);
			//
			//await outPipeClient.ConnectAsync(4000);


			DirectoryInfo testdir = new DirectoryInfo(@"D:\Git\SomeDevCode");

			var TelemetryEvents = new List<PsesTelemetryEvent>();

			var PsesLanguageClient = LanguageClient.Create(options =>
			{
				options.WithInput(inoutPipeClient);
				options.WithOutput(inoutPipeClient);
				//options.WithInput(process.StandardOutput.BaseStream);
				//options.WithOutput(process.StandardOutput.BaseStream);

				options.OnUnhandledException += (Exception exception) =>
				{
					Console.WriteLine(exception.ToString());
				};

				options.Trace = InitializeTrace.Verbose;
				options.WithTrace(InitializeTrace.Verbose);
				options.OnInitialize((ILanguageClient client, InitializeParams request, CancellationToken cancellationToken) => 
					{
						//request.GetType().GetProperty("ProcessId").SetValue(request, (long)process.Id);
						return Task.CompletedTask;
					}
				);
				options.OnInitialized((ILanguageClient client, InitializeParams request, InitializeResult response, CancellationToken cancellationToken) =>
					{
						return Task.CompletedTask;
					}
				);
				options.OnLogMessage(logMessageParams =>
				{
					logger.LogInformation($"{logMessageParams.Type}: {logMessageParams.Message}");
				});
				options.OnNotification("telemetry/event", () =>
					{
					}
				);
				options.OnNotification("window/showMessage", () =>
					{
					}
				);
				options.OnNotification("window/logMessage", () =>
					{
					}
				);
				options.OnTelemetryEvent(telemetryEventParams =>
				{
					TelemetryEvents.Add(
						new PsesTelemetryEvent
						{
							EventName = (string)telemetryEventParams.ExtensionData["eventName"],
							Data = telemetryEventParams.ExtensionData["data"] as JObject
						});
				});
				//options.WithServices(services => 
				//	{
				//	}
				//);

				//options.WithInitializationOptions((InitializeParams inioptions) => {
				//	return inioptions;
				//});
				options.InitializationOptions = new InitializeParams()
				{
					//ProcessId = (int)process.Id,
					ClientInfo = new ClientInfo()
					{
						Name = "myclient",
						Version = "3.16.0"
					},
					RootUri = DocumentUri.FromFileSystemPath(@"D:\Git\SomeDevCode\"),
					Trace = InitializeTrace.Verbose,
					Capabilities = new ClientCapabilities()
					{
						General = new GeneralClientCapabilities(),
						Window = new WindowClientCapabilities() {
							ShowDocument = new Supports<ShowDocumentClientCapabilities>(true),
							ShowMessage = new Supports<ShowMessageRequestClientCapabilities>(true)
						},
						Workspace = new WorkspaceClientCapabilities()
						{
							Symbol = new Supports<WorkspaceSymbolCapability>(true),
							FileOperations = new Supports<FileOperationsWorkspaceClientCapabilities>(true, new FileOperationsWorkspaceClientCapabilities()
							{
								WillCreate = true,
								WillDelete = true,
								WillRename = true,
							})
						},
						TextDocument = new TextDocumentClientCapabilities() 
						{ 
							Completion = new Supports<CompletionCapability>(true)
						}
					}
				};
				
				options.WithCapability(
					new CompletionCapability
					{
						CompletionItem = new CompletionItemCapabilityOptions
						{
							DeprecatedSupport = true,
							DocumentationFormat = new Container<MarkupKind>(MarkupKind.Markdown, MarkupKind.PlainText),
							PreselectSupport = true,
							SnippetSupport = true,
							TagSupport = new CompletionItemTagSupportCapabilityOptions
							{
								ValueSet = new[] { CompletionItemTag.Deprecated }
							},
							CommitCharactersSupport = true
						}
					}
				);
			});



			var tok = new CancellationToken();

			await PsesLanguageClient.Initialize(tok).ConfigureAwait(true);

			//PsesLanguageClient.Start.Subscribe<InitializeResult>((Action<T> onNext) => {
			//});

			var actualCompletions = await PsesLanguageClient.TextDocument.RequestCompletion(
				new CompletionParams
				{
					TextDocument = new TextDocumentIdentifier(DocumentUri.FromFileSystemPath(@"D:\Git\SomeDevCode\Functions.psm1")),
					Position = (5, 10),
				}, new CancellationToken()
			);

			var aa = PsesLanguageClient.RequestWorkspaceSymbols(new WorkspaceSymbolParams
			{
				Query = "EnterRootModuleEcho",
				WorkDoneToken = new ProgressToken(123432),
				PartialResultToken = new ProgressToken(1234321)

			}, CancellationToken.None);
			aa.Subscribe(x => Console.WriteLine(x));

			var testt = aa.ProgressToken;

			var results1 = PsesLanguageClient.RequestDefinition(
				new DefinitionParams
				{
					PartialResultToken = new ProgressToken(23454),
					WorkDoneToken = new ProgressToken(4564),
					TextDocument = new TextDocumentIdentifier
					{
						Uri = DocumentUri.FromFileSystemPath(@"D:\Git\SomeDevCode\Functions.psm1")
					},
					Position = new Position(30, 15),
				},
				new CancellationToken(false)
			);

		}

		static Process StartServer()
		{
			ProcessStartInfo startInfo = new ProcessStartInfo();
			startInfo.FileName = @"powershell.exe";
			startInfo.Arguments = @"& './startup.ps1'";
			startInfo.Verb = "runAs";
			startInfo.RedirectStandardInput = true;
			startInfo.RedirectStandardOutput = true;
			startInfo.RedirectStandardError = true;
			
			startInfo.StandardErrorEncoding = System.Text.Encoding.UTF8;
			startInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;

			startInfo.UseShellExecute = false;
			startInfo.CreateNoWindow = true;


			Process process = new Process();
			process.StartInfo = startInfo;

			process.Start();

			process.BeginErrorReadLine();
			process.BeginOutputReadLine();

			return process;

		}
		static SessionInfoResult WaitTillServerStarted()
		{
			SessionInfoResult sessionInfoObj = new SessionInfoResult();
			while (!serverStarted)
			{
				Console.WriteLine("Waiting... Waiting... Waiting...");

				if (!File.Exists(SessionLogPath))
				{
					System.Threading.Thread.Sleep(1000);
					continue;
				}

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
			Console.WriteLine("languageServiceReadPipeName: " + sessionInfoObj.languageServiceReadPipeName);
			Console.WriteLine("languageServiceWritePipeName: " + sessionInfoObj.languageServiceWritePipeName);


			Console.WriteLine("debugServicePipeName: " + sessionInfoObj.debugServicePipeName);
			Console.WriteLine("debugServiceReadPipeName: " + sessionInfoObj.debugServiceReadPipeName);
			Console.WriteLine("debugServiceWritePipeName: " + sessionInfoObj.debugServiceWritePipeName);


			return sessionInfoObj;
		}


	}

	public interface ITestOutputHelper
	{
		void WriteLine(string message);

		void WriteLine(string format, params object[] args);
	}

	public class SessionInfoResult
	{
		public string status { get; set; }
		public string languageServiceTransport { get; set; }
		public string debugServiceTransport { get; set; }

		public string languageServicePipeName { get; set; }
		public string languageServiceReadPipeName { get; set; }
		public string languageServiceWritePipeName { get; set; }

		public string debugServicePipeName { get; set; }
		public string debugServiceReadPipeName { get; set; }
		public string debugServiceWritePipeName { get; set; }

	}
	internal class PsesTelemetryEvent : Dictionary<string, object>
	{
		public string EventName
		{
			get
			{
				return this["EventName"].ToString() ?? "PsesEvent";
			}
			set
			{
				this["EventName"] = value;
			}
		}

		public JObject Data
		{
			get
			{
				return this["Data"] as JObject ?? new JObject();
			}
			set
			{
				this["Data"] = value;
			}
		}
	}

	internal class PowerShellVersion
	{
		public string Version { get; set; }
		public string DisplayVersion { get; set; }
		public string Edition { get; set; }
		public string Architecture { get; set; }

		public PowerShellVersion()
		{
		}

		public PowerShellVersion(PowerShellVersionDetails versionDetails)
		{
			this.Version = versionDetails.VersionString;
			this.DisplayVersion = $"{versionDetails.Version.Major}.{versionDetails.Version.Minor}";
			this.Edition = versionDetails.Edition;

			switch (versionDetails.Architecture)
			{
				case PowerShellProcessArchitecture.X64:
					this.Architecture = "x64";
					break;
				case PowerShellProcessArchitecture.X86:
					this.Architecture = "x86";
					break;
				default:
					this.Architecture = "Architecture Unknown";
					break;
			}
		}
	}
	public class PowerShellVersionDetails
	{
		#region Properties

		/// <summary>
		/// Gets the version of the PowerShell runtime.
		/// </summary>
		public Version Version { get; private set; }

		/// <summary>
		/// Gets the full version string, either the ToString of the Version
		/// property or the GitCommitId for open-source PowerShell releases.
		/// </summary>
		public string VersionString { get; private set; }

		/// <summary>
		/// Gets the PowerShell edition (generally Desktop or Core).
		/// </summary>
		public string Edition { get; private set; }

		/// <summary>
		/// Gets the architecture of the PowerShell process.
		/// </summary>
		public PowerShellProcessArchitecture Architecture { get; private set; }

		#endregion



	}
	public enum PowerShellProcessArchitecture
	{
		/// <summary>
		/// The processor architecture is unknown or wasn't accessible.
		/// </summary>
		Unknown,

		/// <summary>
		/// The processor architecture is 32-bit.
		/// </summary>
		X86,

		/// <summary>
		/// The processor architecture is 64-bit.
		/// </summary>
		X64
	}
	internal class GetVersionParams : IRequest<PowerShellVersion> { }

}



/*
			var TelemetryEvents = new List<PsesTelemetryEvent>();
			var Diagnostics = new List<Diagnostic>();
			var PsesLanguageClient = LanguageClient.PreInit(options =>
			{
				options
					.WithInput(inoutPipeClient)
					.WithOutput(inoutPipeClient)
					.WithRootUri(DocumentUri.FromFileSystemPath(testdir.FullName))
					.OnPublishDiagnostics(diagnosticParams => Diagnostics.AddRange(diagnosticParams.Diagnostics.Where(d => d != null)))
					.OnLogMessage(logMessageParams => logger.LogInformation($"{logMessageParams.Type}: {logMessageParams.Message}"))
					.OnTelemetryEvent(telemetryEventParams => {
						TelemetryEvents.Add(
							new PsesTelemetryEvent
							{
								EventName = (string)telemetryEventParams.ExtensionData["eventName"],
								Data = telemetryEventParams.ExtensionData["data"] as JObject
							});
					});

				// Enable all capabilities this this is for testing.
				// This will be a built in feature of the Omnisharp client at some point.
				var capabilityTypes = typeof(ICapability).Assembly.GetExportedTypes()
					.Where(z => typeof(ICapability).IsAssignableFrom(z) && z.IsClass && !z.IsAbstract)
					.ToList();

				foreach (Type capabilityType in capabilityTypes)
				{
					options.WithCapability(Activator.CreateInstance(capabilityType, Array.Empty<object>()) as ICapability);
				}
			});
			*/