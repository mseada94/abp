﻿using System;
using System.Collections.Generic;
using Shouldly;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection
{
    public abstract class DependencyInjection_Standard_Tests : AbpIntegratedTest<DependencyInjection_Standard_Tests.TestModule>
    {
        [Fact]
        public void Singleton_Service_Should_Resolve_Dependencies_Independent_From_The_Scope()
        {
            MySingletonService singletonService;
            MyEmptyTransientService emptyTransientService;

            using (var scope = ServiceProvider.CreateScope())
            {
                var transientService1 = scope.ServiceProvider.GetRequiredService<MyTransientService1>();
                emptyTransientService = scope.ServiceProvider.GetRequiredService<MyEmptyTransientService>();

                transientService1.DoIt();
                transientService1.DoIt();

                singletonService = transientService1.SingletonService;
                singletonService.TransientInstances.Count.ShouldBe(2);
            }

            Assert.Equal(singletonService, GetRequiredService<MySingletonService>());

            singletonService.TransientInstances.Count.ShouldBe(2);
            singletonService.TransientInstances.ForEach(ts => ts.IsDisposed.ShouldBeFalse());

            emptyTransientService.IsDisposed.ShouldBeTrue();
        }

        [Fact]
        public void Should_Inject_Services_As_Properties()
        {
            GetRequiredService<ServiceWithPropertyInject>().ProperyInjectedService.ShouldNotBeNull();
        }

        [Fact]
        public void Should_Inject_Services_As_Properties_For_Generic_Classes()
        {
            GetRequiredService<GenericServiceWithPropertyInject<int>>().ProperyInjectedService.ShouldNotBeNull();
        }

        [Fact]
        public void Should_Inject_Services_As_Properties_For_Generic_Concrete_Classes()
        {
            GetRequiredService<ConcreteGenericServiceWithPropertyInject>().ProperyInjectedService.ShouldNotBeNull();
        }

        public class MySingletonService : ISingletonDependency
        {
            public List<MyEmptyTransientService> TransientInstances { get; }

            public IServiceProvider ServiceProvider { get; }

            public MySingletonService(IServiceProvider serviceProvider)
            {
                ServiceProvider = serviceProvider;
                TransientInstances = new List<MyEmptyTransientService>();
            }

            public void ResolveTransient()
            {
                TransientInstances.Add(
                    ServiceProvider.GetRequiredService<MyEmptyTransientService>()
                );
            }
        }

        public class MyTransientService1 : ITransientDependency
        {
            public MySingletonService SingletonService { get; }

            public MyTransientService1(MySingletonService singletonService)
            {
                SingletonService = singletonService;
            }

            public void DoIt()
            {
                SingletonService.ResolveTransient();
            }
        }

        public class MyEmptyTransientService : ITransientDependency, IDisposable
        {
            public bool IsDisposed { get; set; }

            public void Dispose()
            {
                IsDisposed = true;
            }
        }

        public class TestModule : AbpModule
        {
            public override void ConfigureServices(IServiceCollection services)
            {
                services.AddType<MySingletonService>();
                services.AddType<MyTransientService1>();
                services.AddType<MyEmptyTransientService>();
                services.AddType<ServiceWithPropertyInject>();
                services.AddTransient(typeof(GenericServiceWithPropertyInject<>));
                services.AddTransient(typeof(ConcreteGenericServiceWithPropertyInject));
            }
        }

        public class ServiceWithPropertyInject : ITransientDependency
        {
            public MyEmptyTransientService ProperyInjectedService { get; set; }
        }

        public class GenericServiceWithPropertyInject<T> : ITransientDependency
        {
            public MyEmptyTransientService ProperyInjectedService { get; set; }

            public T Value { get; set; }
        }

        public class ConcreteGenericServiceWithPropertyInject : GenericServiceWithPropertyInject<string>
        {

        }
    }
}