#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace UnityIsekaiGame.Development.Automation
{
    public sealed class PrototypeTestLabAutomationResetCoordinator : ITestLabAutomationResetCoordinator
    {
        public TestLabAutomationStepResult Reset(TestLabAutomationContext context, string reason)
        {
            if (context?.Service == null)
            {
                return TestLabAssertions.Fail("reset", "Reset runtime state", "NotNull", "PrototypeTestLabService", "null", "Test Lab service is required before scenarios can reset.");
            }

            PrototypeTestLabService service = context.Service;
            service.ResetAutomationRuntimeState();
            context.EventCapture?.Clear();
            return TestLabAssertions.Pass("reset", "Reset runtime state", reason ?? string.Empty);
        }
    }
}
#endif
