using Polly;
using Polly.Retry;
using Proxy.Configs;
using Proxy.Models;
using Proxy.Utilities;
using static Proxy.Models.Crossing;

namespace Proxy.Adapters;

public class MaxTimeAdapter : IAdapter
{
    // Constants
    private const int PedDetectorInputType = 6;

    // Private fields
    private int _extendedButtonPressTime = 3500;
    private readonly int _standardButtonPressTime = 500;
    private readonly HttpClient _intersectionClient;
    private readonly IntersectionConfig _intersectionConfig;
    private readonly Dictionary<int, PedDetector> _phasesToPedDetectors = new();
    private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;

    // Internal record types
    // ReSharper disable NotAccessedPositionalProperty.Local
    // ReSharper disable once ClassNeverInstantiated.Local
    private record MibResponseItem<T>(string Name, IDictionary<int, T> Data);

    private record MibsRequest<T>(IEnumerable<MibsRequestItem<T>> Data);

    private record MibsRequestItem<T>(string Name, IDictionary<string, T> Data);

    private record PedDetector(int Id, int Module, int Point, int ExtPushTime);
    // ReSharper enable NotAccessedPositionalProperty.Local

    public MaxTimeAdapter(ILogger<MaxTimeAdapter> logger, IHttpClientFactory httpClientFactory,
        IntersectionConfig intersectionConfig)
    {
        _intersectionConfig = intersectionConfig;
        _intersectionClient = httpClientFactory.CreateClient();

        _intersectionClient.BaseAddress = new Uri($"http://{_intersectionConfig.Controller.Address}/maxtime/api/");
        _intersectionClient.DefaultRequestHeaders.Add("Accept", "application/json");

        _retryPolicy = Policy
            .Handle<HttpRequestException>()
            .OrResult<HttpResponseMessage>(response => !response.IsSuccessStatusCode)
            .WaitAndRetryAsync(3, _ => TimeSpan.FromSeconds(3),
                onRetry: (outcome, _, retryAttempt, _) =>
                {
                    logger.LogWarning("Retrying due to '{ExceptionMessage}' on attempt '{RetryAttempt}'",
                        outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString(), retryAttempt);
                });
    }

    public async Task<IAdapter> InitializeAsync()
    {
        // Get pedestrian detector and input settings from the intersection controller
        var mibs = await GetMibsAsync<int>([
            "activePedestrianDetectorPlan", "auxPedDetectorCallPhase-1", "auxPedDetectorButtonPushTime-1",
            "cabinetIOModuleType", "cabinetInputPointControlType-1", "cabinetInputPointControlIndex-1"
        ]);

        // Lookup the active pedestrian detector plan and its call phase and button push times
        var activePedPlan = mibs["activePedestrianDetectorPlan"][0];
        var pedDetectorCallPhaseMibName = $"auxPedDetectorCallPhase-{activePedPlan}";
        var pedDetectorButtonPushTimeMibName = $"auxPedDetectorButtonPushTime-{activePedPlan}";

        if (activePedPlan != 1)
        {
            // Get the pedestrian detector configs if the first plan is not active
            var activePedPlanMibs = await GetMibsAsync<int>([
                pedDetectorCallPhaseMibName, pedDetectorButtonPushTimeMibName
            ]);

            // Add the pedestrian detector settings to the mibs
            foreach (var activePedPlanMib in activePedPlanMibs)
                mibs[activePedPlanMib.Key] = activePedPlanMib.Value;
        }

        // Lookup the pedestrian detectors for each phase
        var pedDetectorCallPhases = mibs[pedDetectorCallPhaseMibName]
            .Select((callPhase, index) => (pedDetector: index + 1, callPhase))
            .Where(pair => pair.callPhase > 0)
            .ToDictionary();

        // Lookup the active input module ids
        var inputModuleIds = mibs["cabinetIOModuleType"]
            .Select((ioModuleType, index) => (ioModuleType, ioModuleId: index + 1))
            .Where(pair => pair.ioModuleType > 1)
            .Select(pair => pair.ioModuleId);

        foreach (var inputModuleId in inputModuleIds)
        {
            var inputModuleTypeMibName = $"cabinetInputPointControlType-{inputModuleId}";
            var inputModuleIndexMibName = $"cabinetInputPointControlIndex-{inputModuleId}";

            if (inputModuleId > 1)
            {
                // Get the next input module configs if not the first module
                var inputModuleMibs =
                    await GetMibsAsync<int>([inputModuleTypeMibName, inputModuleIndexMibName]);

                // Add the input module settings to the mibs
                foreach (var inputModuleMib in inputModuleMibs)
                    mibs[inputModuleMib.Key] = inputModuleMib.Value;
            }

            // Lookup the pedestrian detectors for each input point
            var pedDetectorToInputPointIds = mibs[inputModuleIndexMibName]
                .Select((pedDetectorId, index) => (pedDetectorId, inputPointId: index + 1))
                .Where((_, index) => mibs[inputModuleTypeMibName][index] == PedDetectorInputType);

            foreach (var (pedDetectorId, inputPointId) in pedDetectorToInputPointIds)
            {
                // Lookup the pedestrian detector external push time
                var extPushTime = mibs[pedDetectorButtonPushTimeMibName][pedDetectorId - 1];

                // Get the call phase for the pedestrian detector
                var callPhase = pedDetectorCallPhases[pedDetectorId];

                // Register new pedestrian detector
                var detector = new PedDetector(pedDetectorId, inputModuleId, inputPointId, extPushTime);
                _phasesToPedDetectors.Add(callPhase, detector);
            }

            // Break if all pedestrian detectors for configured crossings were found
            if (_intersectionConfig.Crossings.All(crossing => _phasesToPedDetectors.ContainsKey(crossing.Value.Phase)))
                break;
        }

        // Find the max pedestrian detector button press times and convert it from deca seconds to milli seconds
        _extendedButtonPressTime = _phasesToPedDetectors.Values.Max(detector => detector.ExtPushTime * 100)
                                   + _standardButtonPressTime;

        return this;
    }

    private async Task<IDictionary<string, T[]>> GetMibsAsync<T>(string[] mibNames)
    {
        var mibNamesString = string.Join(',', mibNames);

        var response = await _retryPolicy.ExecuteAsync(() =>
            _intersectionClient.GetAsync($"mibs/{mibNamesString}"));

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Request failed with status code {response.StatusCode}.");

        var mibResults = await response.Content.ReadFromJsonAsync<IEnumerable<MibResponseItem<T>>>();

        if (mibResults is null)
            throw new HttpRequestException($"Request returned with invalid results.");

        // Convert the results to a dictionary of mib name to an array of mib data of type T
        return mibResults.ToDictionary(
            mibItem => mibItem.Name,
            mibItem => mibItem.Data
                .OrderBy(data => data.Key)
                .Select(data => data.Value)
                .ToArray()
        );
    }

    private async Task SetMibsAsync<T>(IDictionary<string, T[]> mibs)
    {
        var mibsRequestItems = mibs
            .Select(mib => new MibsRequestItem<T>(mib.Key, mib.Value
                .Select((dataValue, dataIndex) => ((dataIndex + 1).ToString(), dataValue))
                .ToDictionary()));

        var response = await _retryPolicy.ExecuteAsync(() =>
            _intersectionClient.PostAsJsonAsync("mibs", new MibsRequest<T>(mibsRequestItems)));

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Request failed with status code {response.StatusCode}.");
    }

    private Task<IDictionary<string, byte[]>> GetCrossingInputStatesAsync(
        IEnumerable<IntersectionCrossingConfig> crossingConfigs)
    {
        var mibKeys = crossingConfigs
            .Select(crossing => _phasesToPedDetectors[crossing.Phase].Module)
            .Distinct()
            .Select(module => $"inputPointGroupControl-{module}")
            .ToArray();

        return GetMibsAsync<byte>(mibKeys);
    }

    private Task SetCrossingInputStatesAsync(IDictionary<string, byte[]> inputStates,
        IEnumerable<IntersectionCrossingConfig> crossingConfigs, bool value)
    {
        var data = crossingConfigs
            .GroupBy(crossingConfig => _phasesToPedDetectors[crossingConfig.Phase].Module)
            .ToDictionary(
                group => $"inputPointGroupControl-{group.Key}",
                group => inputStates[$"inputPointGroupControl-{group.Key}"]
                    .SetBits(group
                        .Select(config => _phasesToPedDetectors[config.Phase].Point - 1)
                        .ToArray(), value)
            );

        return SetMibsAsync(data);
    }

    private Dictionary<string, IntersectionCrossingConfig> GetCrossingConfigs(string[]? crossingIds)
    {
        // Return all crossing configurations if no crossing IDs are specified
        if (crossingIds is null) return _intersectionConfig.Crossings;

        // Get the crossing configurations for the specified crossing IDs
        var crossings = new Dictionary<string, IntersectionCrossingConfig>();

        foreach (var crossingId in crossingIds)
        {
            // Throw an exception if the crossing ID is not found
            if (!_intersectionConfig.Crossings.TryGetValue(crossingId, out var crossingConfig))
                throw new KeyNotFoundException($"Crossing '{crossingId}' not found at intersection.");

            crossings.Add(crossingId, crossingConfig);
        }

        // Return the crossing configurations
        return crossings;
    }

    public async Task<Crossing[]> GetCrossingStatesAsync(string[]? crossingIds = default)
    {
        // Get the crossing configurations for the specified crossing IDs
        var crossingConfigs = GetCrossingConfigs(crossingIds);

        // Get the pedestrian calls and states MIBs
        var mibs =
            await GetMibsAsync<byte>(["PedCalls", "AltPedCa", "Walks", "PedClrs", "DWalks"]);

        // Return the intersection crossing states
        return crossingConfigs
            .Select(crossing => new Crossing
            {
                Id = crossing.Key,
                Description = crossing.Value.Description,
                Phase = crossing.Value.Phase,
                Signal =
                    mibs["Walks"].GetBit(crossing.Value.Phase - 1) ? SignalState.Walk :
                    mibs["PedClrs"].GetBit(crossing.Value.Phase - 1) ? SignalState.Clear :
                    mibs["DWalks"].GetBit(crossing.Value.Phase - 1) ? SignalState.Stop :
                    SignalState.Off,
                Calls =
                    mibs["AltPedCa"].GetBit(crossing.Value.Phase - 1) ? CallState.Extended :
                    mibs["PedCalls"].GetBit(crossing.Value.Phase - 1) ? CallState.Standard :
                    CallState.None
            })
            .ToArray();
    }

    public async Task<Crossing[]> CallCrossingsAsync(string[] crossingIds, bool extended = false)
    {
        try
        {
            // Get the crossing configurations for the specified crossing IDs
            var crossingConfigs = GetCrossingConfigs(crossingIds).Values;

            // Get the current input point states
            var originalInputStates = await GetCrossingInputStatesAsync(crossingConfigs);

            // Update the input point to trigger the pedestrian calls for the specified crossings
            await SetCrossingInputStatesAsync(originalInputStates, crossingConfigs, true);

            // Wait for the call to be accepted
            await Task.Delay(extended ? _extendedButtonPressTime : _standardButtonPressTime);

            // Clear the specified the input points
            await SetCrossingInputStatesAsync(originalInputStates, crossingConfigs, false);

            // Get the updated intersection crossing states
            var crossingStates = await GetCrossingStatesAsync(crossingIds);

            // Check if the crossing states updated as expected
            if (crossingStates.Any(crossingState => (extended)
                    ? crossingState.Calls != CallState.Extended
                    : crossingState.Calls != CallState.Standard && crossingState.Calls != CallState.Extended))
                throw new ApplicationException($"Crossings states did not update correctly at controller.");

            // Return the updated crossing states
            return crossingStates;
        }
        catch (Exception)
        {
            // Return empty array when any exception occurs
            return Array.Empty<Crossing>();
        }
    }
}