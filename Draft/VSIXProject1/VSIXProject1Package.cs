using Microsoft;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Task = System.Threading.Tasks.Task;


namespace VSIXProject1
{
	/// <summary>
	/// This is the class that implements the package exposed by this assembly.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The minimum requirement for a class to be considered a valid package for Visual Studio
	/// is to implement the IVsPackage interface and register itself with the shell.
	/// This package uses the helper classes defined inside the Managed Package Framework (MPF)
	/// to do it: it derives from the Package class that provides the implementation of the
	/// IVsPackage interface and uses the registration attributes defined in the framework to
	/// register itself and its components with the shell. These attributes tell the pkgdef creation
	/// utility what data to put into .pkgdef file.
	/// </para>
	/// <para>
	/// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
	/// </para>
	/// </remarks>
	[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
	[Guid(VSIXProject1Package.PackageGuidString)]
	[ProvideAutoLoad(VSConstants.UICONTEXT.SolutionOpening_string, PackageAutoLoadFlags.BackgroundLoad)]
	[ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string, PackageAutoLoadFlags.BackgroundLoad)]
	[ProvideAutoLoad(VSConstants.UICONTEXT.FolderOpened_string, PackageAutoLoadFlags.BackgroundLoad)]
	public sealed class VSIXProject1Package : AsyncPackage
	{
		/// <summary>
		/// VSIXProject1Package GUID string.
		/// </summary>
		public const string PackageGuidString = "33713eb1-d72c-4735-b2f4-a72941e50af0";

		public static IComponentModel ComponentModel;
		public static IVsSolution SolutionObject;

		#region Package Members

		/// <summary>
		/// Initialization of the package; this method is called right after the package is sited, so this is the place
		/// where you can put all the initialization code that rely on services provided by VisualStudio.
		/// </summary>
		/// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
		/// <param name="progress">A provider for progress updates.</param>
		/// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
		protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
		{
			// When initialized asynchronously, the current thread may be a background thread at this point.
			// Do any initialization that requires the UI thread after switching to the UI thread.
			await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

			ComponentModel = await GetServiceAsync(typeof(SComponentModel)) as IComponentModel; //MEF-DI component provider

			Assumes.Present(ComponentModel);

			await InitLspClientAsync(cancellationToken);

		}

		private async Task InitLspClientAsync(CancellationToken cancellationToken)
		{
			await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

			SolutionObject = await GetServiceAsync(typeof(SVsSolution)) as IVsSolution;
			try
			{
				var langClient = new PsLanguageClient();
				var solutionEvents = new SolutionEvents(langClient, cancellationToken);

				SolutionObject?.AdviseSolutionEvents(solutionEvents, out uint _);
			}
			catch (Exception ex)
			{
				var a = ex;
				Console.Write(a.Message);
			}

		}

		#endregion
	}

	public class Ps2ContentDefinition
	{
		[Export]
		[Name("ps2")]
		[BaseDefinition(CodeRemoteContentDefinition.CodeRemoteContentTypeName)]
		internal static ContentTypeDefinition PsContentTypeDefinition;

		[Export]
		[FileExtension(".ps2")]
		[ContentType("ps2")]
		internal static FileExtensionToContentTypeDefinition PsFileExtensionDefinition;


		[Export]
		[Name("psm2")]
		[BaseDefinition(CodeRemoteContentDefinition.CodeRemoteContentTypeName)]
		internal static ContentTypeDefinition PsmContentTypeDefinition;

		[Export]
		[FileExtension(".psm2")]
		[ContentType("psm2")]
		internal static FileExtensionToContentTypeDefinition PsmFileExtensionDefinition;
	}

	//[ContentType("ps2")]
	//[ContentType("psm2")]
	//[Export(typeof(ILanguageClient))]
	public class PsLanguageClient : ILanguageClient
	{
		public string Name => "Bar Language Extension";

		public IEnumerable<string> ConfigurationSections => null;

		public object InitializationOptions => null;

		public IEnumerable<string> FilesToWatch => null;

		public bool ShowNotificationOnInitializeFailed => true;

		public event AsyncEventHandler<EventArgs> StartAsync;
		public event AsyncEventHandler<EventArgs> StopAsync;

		private static Connection Connection = null;
		private static Process Process = null;

		private void StartServer()
		{

			if (Process != null) return;

			ProcessStartInfo info = new ProcessStartInfo();
			var location = Assembly.GetExecutingAssembly().Location;
			var directory = Path.GetDirectoryName(location);
			info.FileName = Path.Combine(directory, @"Microsoft.PowerShell.EditorServices.Host.x86.exe");
			//info.Arguments = "ps2";
			info.RedirectStandardInput = true;
			info.RedirectStandardOutput = true;
			info.UseShellExecute = false;
			info.CreateNoWindow = true;

			Process = new Process();
			Process.StartInfo = info;

			if (Process.Start())
			{
				Connection = new Connection(Process.StandardOutput.BaseStream, Process.StandardInput.BaseStream);
			}
		}

		public async Task<Connection> ActivateAsync(CancellationToken token)
		{
			await Task.Yield();

			StartServer();

			return Connection;
		}

		public async Task OnLoadedAsync()
		{
			await StartAsync.InvokeAsync(this, EventArgs.Empty);
		}

		public Task OnServerInitializeFailedAsync(Exception e)
		{
			return Task.CompletedTask;
		}

		public Task OnServerInitializedAsync()
		{
			return Task.CompletedTask;
		}

		[Import(typeof(SVsServiceProvider))]
		public IServiceProvider Site;

		public Task<InitializationFailureContext> OnServerInitializeFailedAsync(ILanguageClientInitializationInfo initializationState)
		{
			MessageBox.Show(initializationState.StatusMessage);
			return (Task<InitializationFailureContext>)Task.CompletedTask;
		}
	}




	class SolutionEvents : IVsSolutionEvents, IVsSolutionEvents7
	{
		PsLanguageClient _client;
		CancellationToken _cancellationToken;
		public SolutionEvents(PsLanguageClient client, CancellationToken cancellationToken)
		{
			_client = client;
			_cancellationToken = cancellationToken;
		}

		public void OnAfterOpenFolder(string folderPath)
		{

			//var task = Task.Run(async () => await _client.ActivateAsync(_cancellationToken));
			//var connection = task.Result;

			

		}

		public void OnBeforeCloseFolder(string folderPath)
		{

		}

		public void OnQueryCloseFolder(string folderPath, ref int pfCancel)
		{

		}

		public void OnAfterCloseFolder(string folderPath)
		{

		}

		public void OnAfterLoadAllDeferredProjects()
		{

		}

		public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
		{
			return VSConstants.S_OK;
		}

		public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
		{
			return VSConstants.S_OK;
		}

		public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
		{
			return VSConstants.S_OK;
		}

		public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
		{
			return VSConstants.S_OK;
		}

		public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
		{
			return VSConstants.S_OK;
		}

		public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
		{
			return VSConstants.S_OK;
		}

		public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
		{
			return VSConstants.S_OK;
		}

		public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
		{
			return VSConstants.S_OK;
		}

		public int OnBeforeCloseSolution(object pUnkReserved)
		{
			return VSConstants.S_OK;
		}

		public int OnAfterCloseSolution(object pUnkReserved)
		{
			return VSConstants.S_OK;
		}
	}
}
