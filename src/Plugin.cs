using System;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.EmberEDL;

/// <summary>Read-only authenticated EDL sidecar delivery for Ember.</summary>
public sealed class Plugin : BasePlugin<EmberEDLConfiguration>
{
    public Plugin(IApplicationPaths paths, IXmlSerializer serializer) : base(paths, serializer) { }
    public override string Name => "Ember EDL Sidecars";
    public override string Description => "Serves adjacent .edl sidecars to authorized Jellyfin clients; never edits media.";
    public override Guid Id => Guid.Parse("e15c6f0b-823e-42f3-b2de-11d55b9a8341");
}

public sealed class EmberEDLConfiguration : BasePluginConfiguration { }
