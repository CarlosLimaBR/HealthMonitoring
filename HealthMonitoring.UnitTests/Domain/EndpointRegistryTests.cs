﻿using System;
using System.Threading;
using HealthMonitoring.Configuration;
using HealthMonitoring.Model;
using HealthMonitoring.Monitors;
using HealthMonitoring.UnitTests.Helpers;
using Moq;
using Xunit;

namespace HealthMonitoring.UnitTests.Domain
{
    public class EndpointRegistryTests
    {
        private readonly EndpointRegistry _registry;
        private readonly Mock<IHealthMonitorRegistry> _monitorRegistry;
        private readonly Mock<IEndpointConfigurationStore> _configurationStore;
        private readonly Mock<IEndpointStatsRepository> _statsRepository;

        public EndpointRegistryTests()
        {
            _monitorRegistry = new Mock<IHealthMonitorRegistry>();
            _configurationStore = new Mock<IEndpointConfigurationStore>();
            _statsRepository = new Mock<IEndpointStatsRepository>();
            _registry = new EndpointRegistry(_monitorRegistry.Object, _configurationStore.Object, _statsRepository.Object);
        }

        [Fact]
        public void EndpointRegistry_should_load_endpoints_from_store()
        {
            var endpoint = new Endpoint(Guid.NewGuid(), MonitorMock.Mock("monitor"), "address", "name", "group", new[] { "t1", "t2" });
            _configurationStore.Setup(s => s.LoadEndpoints(_monitorRegistry.Object)).Returns(new[] { endpoint });

            var registry = new EndpointRegistry(_monitorRegistry.Object, _configurationStore.Object, _statsRepository.Object);

            Assert.Same(endpoint, registry.GetById(endpoint.Id));
        }

        [Fact]
        public void RegisterOrUpdate_should_register_new_endpoint_and_emit_NewEndpointAdded_event()
        {
            MockMonitor("monitor");

            Endpoint endpoint = null;
            _registry.NewEndpointAdded += e => { endpoint = e; };

            var id = _registry.RegisterOrUpdate("monitor", "address", "group", "name", new[] { "t1" });

            Assert.NotNull(endpoint);
            Assert.Equal("monitor", endpoint.MonitorType);
            Assert.Equal("address", endpoint.Address);
            Assert.Equal("name", endpoint.Name);
            Assert.Equal("group", endpoint.Group);
            Assert.Equal(id, endpoint.Id);
            Assert.Equal("t1", endpoint.Tags[0]);
            Assert.True(endpoint.LastModifiedTime > DateTime.UtcNow.AddSeconds(-1), "LastModifiedTime should be updated");
        }

        [Fact]
        public void RegisterOrUpdate_shouldnt_update_tags_if_null_passed()
        {
            MockMonitor("monitor");

            Endpoint endpoint = null;
            _registry.NewEndpointAdded += e => { endpoint = e; };

            _registry.RegisterOrUpdate("monitor", "address", "group", "name", new[] { "t1" });

            Assert.NotNull(endpoint);
            Assert.Equal("t1", endpoint.Tags[0]);

            _registry.RegisterOrUpdate("monitor", "address", "group", "name", null);

            Assert.NotNull(endpoint);
            Assert.Equal("t1", endpoint.Tags[0]);
        }

        [Fact]
        public void RegisterOrUpdate_should_not_uptate_existing_tags_if_null_passed()
        {
            MockMonitor("monitor");

            Endpoint endpoint = null;
            _registry.NewEndpointAdded += e => { endpoint = e; };

            _registry.RegisterOrUpdate("monitor", "address", "group", "name", new[] { "t1", "t2" });

            Assert.NotNull(endpoint);

            _registry.RegisterOrUpdate("monitor", "address", "group", "name", null);

            CollectionAssert.AreEquivalent(endpoint.Tags, new[] {"t1", "t2"});
        }

        [Fact]
        public void RegisterOrUpdate_should_save_new_endpoint_to_store_when_it_is_created_or_updated()
        {
            MockMonitor("monitor");

            var id = _registry.RegisterOrUpdate("monitor", "address", "group", "name", new[] { "t1", "t2" });

            _configurationStore.Verify(s => s.SaveEndpoint(It.Is<Endpoint>(e => e.Id == id)));

            var newName = "name1";
            _registry.RegisterOrUpdate("monitor", "address", "group", newName, new[] { "t1", "t2" });
            _configurationStore.Verify(s => s.SaveEndpoint(It.Is<Endpoint>(e => e.Id == id && e.Name == newName)));
        }

        [Fact]
        public void RegisterOrUpdate_should_register_new_endpoint_if_monitor_and_address_pair_is_different()
        {
            MockMonitor("monitor");
            MockMonitor("monitor1");

            var id1 = _registry.RegisterOrUpdate("monitor", "address", "group", "name", new[] { "t1", "t2" });
            var id2 = _registry.RegisterOrUpdate("monitor1", "address", "group", "name", new[] { "t1", "t2" });
            var id3 = _registry.RegisterOrUpdate("monitor", "address1", "group", "name", new[] { "t1", "t2" });

            Assert.NotEqual(id1, id2);
            Assert.NotEqual(id1, id3);
            Assert.NotEqual(id2, id3);
        }

        [Fact]
        public void RegisterOrUpdate_should_update_existing_endpoint_and_return_same_id_but_not_emit_NewEndpointAdded_event()
        {
            MockMonitor("monitor");
            var id = _registry.RegisterOrUpdate("monitor", "address", "group", "name", new[] { "t1", "t2" });
            var lastModifiedTime = _registry.GetById(id).LastModifiedTime;

            Endpoint newEndpointCapture = null;
            _registry.NewEndpointAdded += e => { newEndpointCapture = e; };

            Thread.Sleep(100);
            var id2 = _registry.RegisterOrUpdate("monitor", "ADDRESS", "group2", "name2", new[] { "t1", "t2" });

            Assert.Equal(id, id2);
            Assert.Null(newEndpointCapture);

            var endpoint = _registry.GetById(id);
            Assert.NotNull(endpoint);
            Assert.Equal("monitor", endpoint.MonitorType);
            Assert.Equal("address", endpoint.Address);
            Assert.Equal("name2", endpoint.Name);
            Assert.Equal("group2", endpoint.Group);
            Assert.True(_registry.GetById(id2).LastModifiedTime > lastModifiedTime, "LastModifiedTime should be updated");
        }

        [Fact]
        public void GetById_should_return_registered_endpoint()
        {
            MockMonitor("monitor");
            var id = _registry.RegisterOrUpdate("monitor", "address", "group", "name", new[] { "t1", "t2" });
            var endpoint = _registry.GetById(id);

            Assert.NotNull(endpoint);
            Assert.Equal("monitor", endpoint.MonitorType);
            Assert.Equal("address", endpoint.Address);
            Assert.Equal("name", endpoint.Name);
            Assert.Equal("group", endpoint.Group);
        }

        [Fact]
        public void TryUnregister_should_remove_endpoint_and_dispose_it()
        {
            MockMonitor("monitor");
            var id = _registry.RegisterOrUpdate("monitor", "address", "group", "name", new[] { "t1", "t2" });
            var endpoint = _registry.GetById(id);
            Assert.True(_registry.TryUnregisterById(id), "Endpoint should be unregistered");
            Assert.True(endpoint.IsDisposed, "Endpoint should be disposed");
            Assert.Null(_registry.GetById(id));

            _configurationStore.Verify(s => s.DeleteEndpoint(id));
            _statsRepository.Verify(s => s.DeleteStatistics(id));
        }

        [Fact]
        public void TryUnregister_should_return_false_if_endpoint_is_not_registered()
        {
            Assert.False(_registry.TryUnregisterById(Guid.NewGuid()));
        }

        [Fact]
        public void GetById_should_return_null_for_unknown_id()
        {
            Assert.Null(_registry.GetById(Guid.NewGuid()));
        }

        [Fact]
        public void RegisterOrUpdate_should_throw_UnsupportedMonitorException_if_monitor_is_not_recognized()
        {
            _monitorRegistry.Setup(r => r.FindByName("monitor")).Returns((IHealthMonitor)null);
            var exception = Assert.Throws<UnsupportedMonitorException>(() => _registry.RegisterOrUpdate("monitor", "a", "b", "c", new[] { "t1", "t2" }));
            Assert.Equal("Unsupported monitor: monitor", exception.Message);
        }

        private void MockMonitor(string monitorType)
        {
            _monitorRegistry
                .Setup(r => r.FindByName(monitorType))
                .Returns(MonitorMock.Mock(monitorType));
        }
    }
}