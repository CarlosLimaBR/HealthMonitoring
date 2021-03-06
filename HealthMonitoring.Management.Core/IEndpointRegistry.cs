﻿using System;
using System.Collections.Generic;
using HealthMonitoring.Model;

namespace HealthMonitoring.Management.Core
{
    public interface IEndpointRegistry
    {
        Guid RegisterOrUpdate(string monitorType, string address, string group, string name, string[] tags);
        bool TryUpdateEndpointTags(Guid id, string[] tags);
        Endpoint GetById(Guid id);
        bool TryUnregisterById(Guid id);

        IEnumerable<Endpoint> Endpoints { get; }
        void UpdateHealth(Guid endpointId, EndpointHealth health);
    }
}