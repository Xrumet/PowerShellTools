using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PowerShell.EditorServices.Commands;
using Microsoft.PowerShell.EditorServices.Hosting;

namespace ConsoleApp1
{
	internal class Program
	{
		static void Main(string[] args)
		{

			//var logger = new HostLogger(PsesLogLevel.Normal);
			//var config = new EditorServicesConfig()
			//var server = EditorServicesLoader.Create(logger, null,null);

			//server.LoadAndRunEditorServicesAsync();


			var test = new StartEditorServicesCommand();
			
		}
	}
}
