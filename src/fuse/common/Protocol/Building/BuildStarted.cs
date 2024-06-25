using System;

namespace Outracks.Fuse.Protocol
{
	public enum BuildTypeData
	{
		FullCompile,
		LoadMarkup,
	}

	public enum BuildTarget
	{
		Unknown,
		DotNet,
		Android,
		iOS,
		Native
	}

	[PayloadTypeName("Fuse.BuildStarted")]
	public class BuildStartedData : IEventData
	{
		[PluginComment("")]
		public BuildTypeData BuildType;

		[PluginComment("")]
		public Guid BuildId;

		[PluginComment("")]
		public string BuildTag = "";

		[PluginComment("")]
		public Guid ProjectId;

		[PluginComment("")]
		public string ProjectPath;

		[PluginComment("")]
		public BuildTarget Target;
	}
}
