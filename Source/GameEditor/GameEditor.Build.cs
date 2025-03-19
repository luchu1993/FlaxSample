using Flax.Build;
using Flax.Build.NativeCpp;

public class GameEditor : GameEditorModule
{
    /// <inheritdoc />
    public override void Setup(BuildOptions options)
    {
        base.Setup(options);

        BuildNativeCode = false; // Enable this to use C++ scripts
        options.ScriptingAPI.IgnoreMissingDocumentationWarnings = true;
        options.PublicDependencies.Add("Game");
    }
}
