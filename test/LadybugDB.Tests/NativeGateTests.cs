using System;
using Xunit;

namespace LadybugDB.Tests;

/// <summary>
/// CI gate: most tests SKIP when the native library is unavailable (see <see cref="TestEnvironment"/>),
/// which is the right behavior for managed-only/PR builds. The release pipeline, however, stages a
/// freshly built native lib and must FAIL hard if it does not actually load - otherwise a broken
/// per-RID binary would slip through as "skipped" and get published. Setting
/// <c>LADYBUG_REQUIRE_NATIVE=1</c> turns that latent skip into a real failure.
/// </summary>
public sealed class NativeGateTests
{
    [Fact]
    public void NativeLibrary_LoadsWhenRequired()
    {
        if (Environment.GetEnvironmentVariable("LADYBUG_REQUIRE_NATIVE") != "1")
        {
            return; // Not enforced locally or in managed-only CI.
        }

        Assert.True(
            TestEnvironment.NativeAvailable,
            "LADYBUG_REQUIRE_NATIVE=1 but the native Ladybug library failed to load. " +
            "Check that lib/runtimes/<rid>/native contains a loadable shared library for this RID.");
    }
}
