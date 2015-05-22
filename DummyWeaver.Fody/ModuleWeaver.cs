using System;
using Mono.Cecil;

namespace DummyWeaver.Fody
{
    public class ModuleWeaver
    {
        // Will log an MessageImportance.High message to MSBuild.
        public Action<string> LogInfo { get; set; }

        public ModuleDefinition ModuleDefinition { get; set; }

        public ModuleWeaver()
        {
            LogInfo = delegate { };
        }

        public void Execute()
        {
            LogInfo("Executing DummyWeaver.");
        }

        // Will be called when a request to cancel the build occurs. OPTIONAL
        public void Cancel()
        {
            LogInfo("Cancelling DummyWeaver.");
        }

        // Will be called after all weaving has occurred and the module has been saved. OPTIONAL
        public void AfterWeaving()
        {
            LogInfo("Weaving finished.");
        }
    }
}
