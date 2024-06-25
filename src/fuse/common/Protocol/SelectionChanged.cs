
namespace Outracks.Fuse.Protocol
{
	[PayloadTypeName("Fuse.Preview.SelectionChanged")]
	public class SelectionChanged : IEventData
	{
		[PluginComment("")]
		public string Path;
		[PluginComment("")]
		public string Text;
		[PluginComment("")]
		public Messages.TextPosition CaretPosition;
	}
}