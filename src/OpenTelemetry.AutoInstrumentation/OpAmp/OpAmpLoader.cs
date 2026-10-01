// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Plugins;
using OpenTelemetry.Resources;

namespace OpenTelemetry.AutoInstrumentation.OpAmp;

internal static class OpAmpLoader
{
    private static OpAmpManager? _opAmpManager;
    private static Task? _startTask;

    public static void EnableOpAmpClient(Resource resources, OpAmpSettings opAmpSettings, PluginManager pluginManager)
    {
        if (_opAmpManager != null)
        {
            throw new InvalidOperationException("OpAMP is already enabled");
        }

        _opAmpManager = new OpAmpManager(pluginManager);

        _startTask = Task.Run(() => _opAmpManager.StartClientAsync(resources, opAmpSettings));
    }

    public static void StopOpAmpClientIfRunning()
    {
        if (_startTask == null)
        {
            return;
        }

        if (!_startTask.IsCompleted)
        {
            _startTask.Wait();
        }

        Task.Run(() => _opAmpManager?.StopOpAmpClientAsync()).Wait();

        // Reset state
        _opAmpManager?.Dispose();
        _startTask.Dispose();
        _opAmpManager = null;
        _startTask = null;
    }
}
