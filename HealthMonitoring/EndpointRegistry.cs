﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using HealthMonitoring.Configuration;
using HealthMonitoring.Model;

namespace HealthMonitoring
{
    public class EndpointRegistry : IEndpointRegistry
    {
        private readonly IHealthMonitorRegistry _healthMonitorRegistry;
        private readonly IEndpointConfigurationStore _endpointConfigurationStore;
        private readonly IEndpointStatsRepository _statsRepository;
        private readonly ConcurrentDictionary<string, Endpoint> _endpoints = new ConcurrentDictionary<string, Endpoint>();
        private readonly ConcurrentDictionary<Guid, Endpoint> _endpointsByGuid = new ConcurrentDictionary<Guid, Endpoint>();

        public IEnumerable<Endpoint> Endpoints { get { return _endpoints.Select(p => p.Value); } }
        public event Action<Endpoint> NewEndpointAdded;

        public EndpointRegistry(IHealthMonitorRegistry healthMonitorRegistry, IEndpointConfigurationStore endpointConfigurationStore, IEndpointStatsRepository statsRepository)
        {
            _healthMonitorRegistry = healthMonitorRegistry;
            _endpointConfigurationStore = endpointConfigurationStore;
            _statsRepository = statsRepository;

            foreach (var endpoint in _endpointConfigurationStore.LoadEndpoints(healthMonitorRegistry))
            {
                if (_endpoints.TryAdd(GetKey(endpoint.MonitorType, endpoint.Address), endpoint))
                    _endpointsByGuid.TryAdd(endpoint.Id, endpoint);
            }
        }

        public Guid RegisterOrUpdate(string monitorType, string address, string group, string name, string[] tags)
        {
            var monitor = _healthMonitorRegistry.FindByName(monitorType);
            if (monitor == null)
                throw new UnsupportedMonitorException(monitorType);

            var key = GetKey(monitorType, address);
            var newId = Guid.NewGuid();
            var endpoint = _endpoints.AddOrUpdate(key, new Endpoint(newId, monitor, address, name, group, tags), (k, e) => e.Update(group, name, tags));
            _endpointsByGuid[endpoint.Id] = endpoint;

            if (endpoint.Id == newId)
                NewEndpointAdded?.Invoke(endpoint);

            _endpointConfigurationStore.SaveEndpoint(endpoint);

            return endpoint.Id;
        }

        public bool TryUpdateEndpointTags(Guid id, string[] tags)
        {
            Endpoint endpoint;
            if(!_endpointsByGuid.TryGetValue(id, out endpoint))
                return false;

            endpoint.UpdateTags(tags);
            _endpointConfigurationStore.SaveEndpoint(endpoint);
            return true;
        }

        public Endpoint GetById(Guid id)
        {
            Endpoint endpoint;
            return _endpointsByGuid.TryGetValue(id, out endpoint) ? endpoint : null;
        }

        public bool TryUnregisterById(Guid id)
        {
            Endpoint endpoint;

            if (!_endpointsByGuid.TryRemove(id, out endpoint) ||
                !_endpoints.TryRemove(GetKey(endpoint.MonitorType, endpoint.Address), out endpoint))
                return false;

            endpoint.Dispose();

            _endpointConfigurationStore.DeleteEndpoint(endpoint.Id);
            _statsRepository.DeleteStatistics(endpoint.Id);
            return true;
        }

        private static string GetKey(string monitor, string address)
        {
            return $"{monitor}|{address.ToLowerInvariant()}";
        }

        public IEnumerable<string> HealthStatuses()
        {
            return Enum.GetNames(typeof(EndpointStatus)).Select(m => m.ToLower());
        }
    }
}