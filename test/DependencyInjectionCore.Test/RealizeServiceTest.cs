using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using DependencyInjectionCore.DependencyInjection.ServiceLookup;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace DependencyInjectionCore.Test
{
    public class RealizeServiceTest
    {
        private readonly ITestOutputHelper _outputHelper;

        public RealizeServiceTest(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Fact]
        public void Test1()
        {
            var services = new ServiceCollection()
                .AddTransient<IFoo, Foo>()
                //.AddTransient<IBar, Bar>()
                .BuildServiceProvider();

            var engine = ReflectionExtensions.GetNonPublicField(services, "_engine");
            var realizedServices = (System.Collections.IDictionary)ReflectionExtensions.GetNonPublicProperty(engine, "RealizedServices");

            for (int i = 0; i < 3; i++)
            {
                services.GetRequiredService<IFoo>(); //组件实例化
                foreach (DictionaryEntry item in realizedServices)
                {
                    var title = String.Format("Loop {0}, type {1}, hash {2}", i, ((Type)item.Key).FullName, item.Value.GetHashCode());
                    _outputHelper.WriteLine(title); 
                }
                Thread.Sleep(10); //确保异步任务完成
            }
        }

        class ReflectionExtensions
        {
            public static Object GetNonPublicField(Object instance, String name)
            {
                var type = instance.GetType();
                var field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
                return field.GetValue(instance);
            }

            public static Object GetNonPublicProperty(Object instance, String name)
            {
                var type = instance.GetType();
                var property = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Instance);
                return property.GetValue(instance);
            }
        }

        interface IFoo
        {
        }

        class Foo : IFoo
        {
            public Foo(IServiceProvider serviceProvider)
            {
                ;
            }
        }

        interface IBar
        {
        }

        class Bar : IBar
        {
            public Bar(IFoo foo)
            {
                ;
            }
        }
    }
}
