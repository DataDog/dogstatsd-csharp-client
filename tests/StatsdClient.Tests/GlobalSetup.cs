using System;
using NUnit.Framework;
using StatsdClient;

[SetUpFixture]
public class GlobalSetup
{
    private static readonly string[] EnvironmentVariables =
    {
        StatsdConfig.ServiceEnvVar, StatsdConfig.EnvironmentEnvVar, StatsdConfig.VersionEnvVar,
    };

    private readonly string[] _values = new string[EnvironmentVariables.Length];

    [OneTimeSetUp]
    public void Setup()
    {
        for (int i = 0; i < EnvironmentVariables.Length; i++)
        {
            _values[i] = Environment.GetEnvironmentVariable(EnvironmentVariables[i]);
            Environment.SetEnvironmentVariable(EnvironmentVariables[i], null);

        }
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        for (int i = 0; i < EnvironmentVariables.Length; i++)
        {
            Environment.SetEnvironmentVariable(EnvironmentVariables[i], _values[i]);
        }
    }
}
