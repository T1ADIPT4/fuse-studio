﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Fuse.Preview;
using Mono.Options;
using Outracks.IO;
using Outracks.IPC;
using Outracks.Simulator;
using Uno.Build;

namespace Outracks.Fuse
{
	public class PreviewCommand : CliCommand
	{
		public static PreviewCommand CreatePreviewCommand()
		{
			var shell = new Shell();
			var projectDetector = new ProjectDetector(shell);
			var coloredConsole = ColoredTextWriter.Out;
			var fuse = FuseApi.Initialize("fuse", new List<string>());

			return new PreviewCommand(
				coloredConsole,
				new PreviewArgumentResolver(
					projectDetector,
					shell),
				new PreviewMain(
					shell, fuse,
					new PreviewExported(coloredConsole),
					coloredConsole),
					fuse);
		}

		readonly PreviewArgumentResolver _argumentResolver;
		readonly ColoredTextWriter _textWriter;
		readonly PreviewMain _preview;
		readonly HelpArguments _helpArguments;
		readonly OptionSet _optionSet;
		readonly IFuse _fuse;

		bool _promtOnError = false;
		Optional<string> _buildTag;
		Optional<IPEndPoint> _endPoint;
		string _previewTarget;
		bool _isVerboseBuild = false;
		bool _buildLibraries = false;
		bool _printUnoConfig = false;

		public PreviewCommand(ColoredTextWriter textWriter, PreviewArgumentResolver argumentResolver, PreviewMain preview, IFuse fuse)
			: base("preview", "Preview an app")
		{
			_textWriter = textWriter;
			_preview = preview;
			_argumentResolver = argumentResolver;
			_fuse = fuse;
			_optionSet = CreateOptions();
			_helpArguments = new HelpArguments(
				new HelpHeader("fuse " + Name, Description),
				new HelpSynopsis("fuse preview [target] [options] [project-path]"),
				new HelpDetailedDescription(@"Previews an app by using a simulator.
Your app will instantly reload when there are any saved changes to an UX file.
[project-path] can be a relative or an absolute path to a project file.
It can also be the relative or absolute path to the containing directory of the project.
Working directory is used as [project-path] if not specified otherwise.

NOTE: The project must contain an UX file with an App tag for preview to work."),
				new HelpOptions(new []
				{
					_optionSet.ToTable(),
					new Table("Available preview targets", new []
					{
						new Row("dotnet",	"Preview locally. (default)"),
						new Row("android",	"Preview on an Android device."),
 						new Row("ios",		"Preview on an iOS device. NOTE: Only macOS can target this device."),
 						new Row("native",	"Preview locally (native).")
					}),
				}));
		}

		OptionSet CreateOptions()
		{
			var optionSet = new OptionSet()
			{
				{ "t=|target=", "Preview target (see: Available preview targets)", a => _previewTarget = a },
				{ "endpoint=", "Custom endpoint to proxy server. Formatted as ADDRESS:PORT.", a => _endPoint = ParseEndpoint(a) },
				{ "n=|name=", "Identify the preview build with a name returned in the Fuse.BuildStarted event as a tag.", a => _buildTag = NullToNone.ToOptional(a) },
				{ "v|verbose", "Verbose output.", a => _isVerboseBuild = true },
				{ "l|libs", "Build libraries.", a => _buildLibraries = true },
				{ "prompt-on-error", "Console window will not close if there are build errors or fatal crashes in this mode.", a => _promtOnError = true },
				{ "print-unoconfig", "Print uno config before starting preview.", a => _printUnoConfig = true },
			};

			return optionSet;
		}

		public override void Help()
		{
			_textWriter.WriteHelp(_helpArguments);
		}

		public override void Run(string[] args, CancellationToken ct)
		{
			try
			{
				RunInternal(args);
			}
			catch (ExitWithError e)
			{
				if (_promtOnError)
				{
					Console.WriteLine("fuse: " + e.ErrorOutput);
					Console.WriteLine("Press any key to exit.");
					Console.ReadKey(true);
				}
				throw;
			}
			catch (Exception e)
			{
				if (_promtOnError)
				{
					Console.WriteLine("fuse: unhandled exception: " + e.Message);
					Console.WriteLine(e.StackTrace);
					Console.WriteLine("Press any key to exit.");
					Console.ReadKey(true);
				}
				throw;
			}
		}

		void RunInternal(string[] args)
		{
			try
			{
				BuildTarget target;
				VersionWriter.Write(_textWriter, _fuse.Version);
				var parsedArgs = _optionSet.Parse(args);

				try
				{
					// Convert legacy name
					if (_previewTarget?.ToUpper() == "LOCAL")
						_previewTarget = "dotnet";

					target = BuildTargets.Get(_previewTarget, parsedArgs);
				}
				catch (ArgumentException)
				{
					throw new UnknownPreviewTarget(_previewTarget ?? "dotnet");
				}
				
				var previewArgs = _argumentResolver
					.Resolve(parsedArgs)
					.With(
						target: target,
						buildTag: _buildTag,
						isVerboseBuild: _isVerboseBuild,
						buildLibraries: _buildLibraries,
						printUnoConfig: _printUnoConfig);

				if (_endPoint.HasValue)
					previewArgs = previewArgs.With(endpoints: ImmutableHashSet.Create(_endPoint.Value));

				_preview.Preview(previewArgs);
			}
			catch (InvalidPath e)
			{
				throw new ExitWithError(
					"The specified path '" + e.Path + "' is invalid" +
						(e.InnerException != null ? ": " + e.InnerException.Message : ""));
			}
			catch (DaemonException e)
			{
				throw new ExitWithError(e.Message);
			}
			catch (FailedToCreateUniqueDirectory e)
			{
				throw new ExitWithError(e.Message);
			}
			catch (FailedToCreateOutputDir e)
			{
				throw new ExitWithError(e.Message);
			}
			catch (BuildFailed)
			{
				throw new ExitWithError("Failed to compile project");
			}
			catch (ProjectNotFound)
			{
				throw new ExitWithError("Could not find a fuse X project to preview");
			}
			catch (UnknownPreviewTarget e)
			{
				throw new ExitWithError(
					"Unknown preview target " + e.Target + ". Please run 'fuse help preview' for all available targets");
			}
			catch (FileNotFoundException)
			{
				throw new ExitWithError("Could not find the file specified");
			}
			catch (InvalidEndpointString)
			{
				throw new ExitWithError(
					"Invalid endpoint string, excepted it to be formatted as [Address]:[Port]. For example 127.0.0.1:12124");
			}
			catch (SocketException e)
			{
				throw new ExitWithError(
					"A network error occurred: " + e.Message + "\nPlease check your network setup and try again.");
			}
			catch (UnableToResolveHostNameException e)
			{
				throw new ExitWithError(
					"A network error occurred: " + e.Message + "\nPlease check your network setup and try again.");
			}
			catch (ExportTargetNotSupported e)
			{
				throw new ExitWithError("Previewing of target " + e.ExportTarget + " is not supported on this operating system.");
			}
			catch (RunFailed e)
			{
				throw new ExitWithError(e.Message);
			}
		}

		static IPEndPoint ParseEndpoint(string endpoint)
		{
			try
			{
				var ipPort = endpoint.Split(new[] { ":" }, 2, StringSplitOptions.RemoveEmptyEntries);
				return new IPEndPoint(IPAddress.Parse(ipPort[0]), ipPort.Length > 1 ? int.Parse(ipPort[1]) : 12124);
			}
			catch (Exception e)
			{
				throw new InvalidEndpointString(e);
			}
		}
	}

	class InvalidEndpointString : Exception
	{
		public InvalidEndpointString(Exception innerException) : base("Invalid endpoint string", innerException)
		{
		}
	}

	class UnknownPreviewTarget : Exception
	{
		public readonly string Target;

		public UnknownPreviewTarget(string target)
			: base("Unknown target name: " + target)
		{
			Target = target;
		}
	}
}
