#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace UnityIsekaiGame.Development.Automation
{
    public sealed class PrototypeTestLabAutomationResetCoordinator : ITestLabAutomationResetCoordinator
    {
        public TestLabAutomationStepResult Reset(TestLabAutomationContext context, string reason)
        {
            PrototypeTestLabService service = context?.GetHost<PrototypeTestLabAutomationHost>()?.Service;
            if (service == null)
            {
                return TestLabAssertions.Fail("reset", "Reset runtime state", "NotNull", "PrototypeTestLabService", "null", "Test Lab service is required before scenarios can reset.");
            }

            service.ResetAutomationRuntimeState();
            context.EventCapture?.Clear();
            return TestLabAssertions.Pass("reset", "Reset runtime state", reason ?? string.Empty);
        }
    }
}
#endif
