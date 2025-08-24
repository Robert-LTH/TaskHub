using MsGraphServicePlugin;
using System.Runtime.Serialization;
using Xunit;

namespace MsGraphServicePlugin.Tests;

public class MsGraphServicePluginTests
{
    [Fact]
    public void NameIsMsGraph()
    {
        var plugin = (MsGraphServicePlugin)FormatterServices.GetUninitializedObject(typeof(MsGraphServicePlugin));
        Assert.Equal("msgraph", plugin.Name);
    }
}
