// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.AutoInstrumentation.OpAmp.Listeners;
using OpenTelemetry.AutoInstrumentation.PluginApi.OpAmp;
using OpenTelemetry.AutoInstrumentation.Plugins;
using OpenTelemetry.OpAmp.Client;
using OpenTelemetry.OpAmp.Client.Listeners;
using OpenTelemetry.OpAmp.Client.Messages;
using OpenTelemetry.OpAmp.Client.Settings;
using OpenTelemetry.Resources;

namespace OpenTelemetry.AutoInstrumentation.OpAmp;

internal class OpAmpManager : IDisposable
{
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger("OpAmp");
    private readonly PluginManager _pluginManager;
    private readonly CancellationTokenSource _cts = new();

    private OpAmpClient? _client;
    private PluginOpAmpClient? _pluginClient;
    private ServerSentCapabilities _serverSentCapabilities;
    private OpAmpCapabilities _clientCapabilities;
    private OpAmpPluginCapabilities _pluginCapabilities;
    private IEnumerable<string>? _serverCustomCapabilities;
    private IEnumerable<string>? _clientCustomCapabilities;
    private CapabilitiesListener _capabilitiesListener;
    private FlagsMessageListener _flagsListener;

    public OpAmpManager(PluginManager pluginManager)
    {
        _pluginManager = pluginManager;
        _capabilitiesListener = new CapabilitiesListener(this);
        _flagsListener = new FlagsMessageListener(this);
    }

    public bool IsRunning { get; private set; }

    public async Task StartClientAsync(Resource resources, OpAmpSettings opAmpSettings)
    {
        try
        {
            _client = new OpAmpClient(settings => ConfigureClient(settings, opAmpSettings, resources));
            _pluginClient = new PluginOpAmpClient(this);

            _client.Subscribe<ServerCapabilitiesMessage>(_capabilitiesListener);
            _client.Subscribe<CustomCapabilitiesMessage>(_capabilitiesListener);
            _client.Subscribe(_flagsListener);

            try
            {
                using var suppressInstrumentation = SuppressInstrumentationScope.Begin();

                IsRunning = true;

                await _client.StartAsync(_cts.Token).ConfigureAwait(false);

                // Notify plugins that OpAmp client is available
                _pluginManager.AfterOpAmpClientStarted(_pluginClient);
            }
            catch (Exception ex)
            {
                IsRunning = false;

                Logger.Warning(ex, "OpAmp client stopped unexpectedly.");
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "An error occurred while initializing the OpAmp client.");
        }
    }

    public async Task StopOpAmpClientAsync()
    {
        if (!IsRunning)
        {
            return;
        }

        _pluginManager?.BeforeOpAmpClientStopped();

        try
        {
            using var suppressInstrumentation = SuppressInstrumentationScope.Begin();

            if (_client != null)
            {
                _client.Unsubscribe<ServerCapabilitiesMessage>(_capabilitiesListener);
                _client.Unsubscribe<CustomCapabilitiesMessage>(_capabilitiesListener);
                _client.Unsubscribe(_flagsListener);

                await _client.StopAsync()
                    .ConfigureAwait(false);
            }

            _client?.Dispose();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "An error occurred while stopping the OpAmp client.");
        }
        finally
        {
            IsRunning = false;
        }
    }

    public void Dispose()
    {
        _cts.Dispose();
        _client?.Dispose();
    }

    internal void UpdateServerCapabilities(ServerSentCapabilities capabilities)
    {
        _serverSentCapabilities = capabilities;

        // Client can now send common messages.
        UpdateEffectiveConfig();
    }

    internal void UpdateServerCustomCapabilities(ICollection<string> capabilities)
    {
        _serverCustomCapabilities = capabilities;

        // Client can now send custom messages.
    }

    internal void UpdateClientCustomCapabilities(IEnumerable<string> capabilities)
    {
        _clientCustomCapabilities = capabilities;
    }

    internal void SendCustomMessage(string capability, string type, ReadOnlyMemory<byte> data)
    {
        if (_clientCustomCapabilities == null ||
            !_clientCustomCapabilities.Contains(capability))
        {
            Logger.Error("Cannot send custom message. Capability '{0}' is not supported.", capability);

            return;
        }

        if (_serverCustomCapabilities == null ||
            !_serverCustomCapabilities.Contains(capability))
        {
            Logger.Error("Cannot send custom message. Server does not support capability: {0}.", capability);

            return;
        }

        _client?.SendCustomMessageAsync(capability, type, data);
    }

    internal void Subscribe<T>(IOpAmpListener<T> listener)
        where T : OpAmpMessage
    {
        _client?.Subscribe(listener);
    }

    internal void Unsubscribe<T>(IOpAmpListener<T> listener)
        where T : OpAmpMessage
    {
        _client?.Unsubscribe(listener);
    }

    internal void SendRemoteConfigStatus(RemoteConfigStatusReport report)
    {
        if (!IsRemoteConfigStatusSupported())
        {
            Logger.Error("Plugin tried to report remote config status but server or client does not support reporting.");

            return;
        }

        _client?.SendRemoteConfigStatusAsync(report).Wait();
    }

    internal void SendFullStateReport()
    {
        var report = new FullStateReport();

        if (IsEffectiveConfigSupported())
        {
            report.EffectiveConfigFiles = GetEffectiveConfig();
        }

        if (IsRemoteConfigStatusSupported())
        {
            report.RemoteConfigStatus = GetRemoteConfigStatus();
        }

        report.CustomCapabilities = _clientCustomCapabilities;

        _client?.SendFullStateReportAsync(report).Wait();
    }

    private void ConfigureClient(OpAmpClientSettings settings, OpAmpSettings opAmpSettings, Resource resources)
    {
        // Configure server URL.
        var serverUrl = opAmpSettings.ServerUrl;
        if (serverUrl != null)
        {
            settings.ServerUrl = serverUrl;
            settings.ConnectionType = OpAmpHelper.GetConnectionType(serverUrl);
        }

        // Configure resource attributes for identification.
        foreach (var resourceAttribute in resources.Attributes)
        {
            if (resourceAttribute.Key == null ||
                resourceAttribute.Value == null)
            {
                continue;
            }

            settings.Identification.AddAttribute(resourceAttribute.Key, resourceAttribute.Value);
        }

        settings.Identification.AddNonIdentifyingAttribute("opamp.version", OpAmpHelper.GetOpAmpVersion());

        // Add possibility for plugins to override settings
        _pluginManager.ConfigureOpAmpOptions(settings);

        // Configure capabilities
        ConfigureClientCapabilities(settings);
    }

    private void ConfigureClientCapabilities(OpAmpClientSettings settings)
    {
        // Fetch plugin capabilities
        _pluginCapabilities = _pluginManager.ConfigurePluginCapabilities();

        if (settings.EffectiveConfigurationReporting.EnableReporting ||
            _pluginCapabilities.HasFlag(OpAmpPluginCapabilities.ReportsEffectiveConfig))
        {
            _clientCapabilities |= OpAmpCapabilities.ReportsEffectiveConfig;
        }

        if (settings.RemoteConfiguration.ReportsRemoteConfigStatus ||
            _pluginCapabilities.HasFlag(OpAmpPluginCapabilities.ReportsRemoteConfigStatus))
        {
            _clientCapabilities |= OpAmpCapabilities.ReportsRemoteConfigStatus;
        }
    }

    private void UpdateEffectiveConfig()
    {
        if (!IsEffectiveConfigSupported())
        {
            return;
        }

        var effectiveConfig = GetEffectiveConfig();

        _client?.SendEffectiveConfigAsync(effectiveConfig).Wait();
    }

    private bool IsEffectiveConfigSupported()
    {
        if (!_serverSentCapabilities.HasFlag(ServerSentCapabilities.AcceptsEffectiveConfig))
        {
            Logger.Debug("Server does not support effective config.");

            return false;
        }

        if (!_clientCapabilities.HasFlag(OpAmpCapabilities.ReportsEffectiveConfig))
        {
            Logger.Debug("Client does not support effective config reporting.");

            return false;
        }

        return true;
    }

    private bool IsRemoteConfigStatusSupported()
    {
        if (!_serverSentCapabilities.HasFlag(ServerSentCapabilities.OffersRemoteConfig))
        {
            Logger.Debug("Server does not support remote config.");

            return false;
        }

        if (!_clientCapabilities.HasFlag(OpAmpCapabilities.ReportsRemoteConfigStatus))
        {
            Logger.Debug("Client does not support remote config reporting.");

            return false;
        }

        return true;
    }

    private IEnumerable<EffectiveConfigFile> GetEffectiveConfig()
    {
        if (_pluginCapabilities.HasFlag(OpAmpPluginCapabilities.ReportsEffectiveConfig))
        {
            return _pluginManager.RequestEffectiveConfig();
        }

        // TODO: auto-inst must report effective config directly.
        throw new NotImplementedException();
    }

    private RemoteConfigStatusReport GetRemoteConfigStatus()
    {
        if (_pluginCapabilities.HasFlag(OpAmpPluginCapabilities.ReportsRemoteConfigStatus))
        {
            return _pluginManager.RequestRemoteConfigStatus();
        }

        // TODO: auto-inst must report remote config status directly.
        throw new NotImplementedException();
    }
}
