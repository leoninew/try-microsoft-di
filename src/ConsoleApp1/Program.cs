using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using DependencyInjectionCore.DependencyInjection.ServiceLookup;
using Microsoft.Extensions.DependencyInjection;
using Mono.Options;
using NLog;

namespace ConsoleApp1
{
    class Program1
    {
        static ILogger _logger;
        static IServiceCollection _services;
        static CompiledServiceProviderEngine _expEngine;
        static ILEmitServiceProviderEngine _emitEngine;
        static Stopwatch _watch;

        static void Main(string[] args)
        {
            _logger = LogManager.GetCurrentClassLogger();
            _services = new ServiceCollection();
            _watch = new Stopwatch();

            var method = default(String);
            var target = default(String);
            var cache = false;
            var number = 0;
            var loop = 0;

            var options = new OptionSet {
                { "m|method=", "should be ref/emit/exp",   (String s) => method = s },
                { "c|cache=",  "should be true/false", (String s) => cache = Boolean.Parse(s) },
                { "t|target=", "should be foo/bar",   (String s) => target = s },
                { "n|number=", "should be 100/1000/5000/10000", (String s) => number = Int32.Parse(s) },
                { "l|loop=", "should be number between 1 and 10", (String s) => loop = Int32.Parse(s) },
            };

            if (args == null || args.Length == 0)
            {
                _logger.Debug("Usage: -m [method] -c [cache] -t [target] -n [number] \n{0}", LogOptionSet(options));
                _logger.Debug("Argument empty, type as simple: -m ref -c true -t foo -n 10000 -l 10");
                var input = Console.ReadLine();
                args = input.Split(' ');
            }
            options.Parse(args);

            _logger.Debug("Configure service begin");
            var types = Assembly.GetExecutingAssembly().GetTypes();
            var fooInterfaces = types.Where(x => x.IsInterface && x.Name.StartsWith("IFoo_"));
            foreach (var item in fooInterfaces)
            {
                var impl = types.Single(x => x.IsClass && item.IsAssignableFrom(x));
                _services.AddTransient(item, impl);
            }
            _services.AddTransient<IBar_100, Bar_100>();
            _services.AddTransient<IBar_1000, Bar_1000>();
            _services.AddTransient<IBar_5000, Bar_5000>();
            _services.AddTransient<IBar_10000, Bar_10000>();

            _expEngine = new CompiledServiceProviderEngine(_services, null);
            _emitEngine = new ILEmitServiceProviderEngine(_services, null);
            _logger.Debug("Configure service end");

            _logger.Debug("Create callSite begin");
            foreach (var item in _services)
            {
                if (item.ImplementationType != null)
                {
                    _expEngine.CallSiteFactory.CreateCallSite(item.ServiceType, new CallSiteChain());
                }
                if (item.ImplementationType != null)
                {
                    _emitEngine.CallSiteFactory.CreateCallSite(item.ServiceType, new CallSiteChain());
                }
            }
            _logger.Debug("Create callSite end");

            Action<Type, Boolean> handle = default;
            if (method == "ref")
            {
                handle = GetRefService;
            }
            else if (method == "exp")
            {
                handle = GetExpService;
            }
            else if (method == "emit")
            {
                handle = GetEmitService;
            }
            else
            {
                _logger.Debug("Usage: -m [method] -c [cache] -t [target] -n [number] \n{0}", LogOptionSet(options));
                return;
            }

            if (target == "foo")
            {
                TestFoo(handle, method, false, number);
                for (int i = 1; i < loop; i++)
                {
                    TestFoo(handle, method, cache, number);
                }
            }
            else
            {
                Type bar = default;
                if (number == 100)
                {
                    bar = typeof(IBar_100);
                }
                else if (number == 1000)
                {
                    bar = typeof(IBar_1000);
                }
                else if (number == 5000)
                {
                    bar = typeof(IBar_5000);
                }
                else if (number == 10000)
                {
                    bar = typeof(IBar_10000);
                }
                else
                {
                    _logger.Debug("Usage: -m [method] -c [cache] -t [target] -n [number] \n{0}", LogOptionSet(options));
                    return;

                }

                TestBar(handle, method, false, bar);
                for (int i = 1; i < loop; i++)
                {
                    TestBar(handle, method, cache, bar);
                }
            }
        }

        private static String LogOptionSet(OptionSet options)
        {
            using (var stream = new MemoryStream())
            using (var writer = new StreamWriter(stream))
            {
                options.WriteOptionDescriptions(writer);
                writer.Flush();
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        static void GetRefService(Type type, Boolean cache)
        {
            var site = _expEngine.CallSiteFactory.CreateCallSite(type, new CallSiteChain());
            Func<ServiceProviderEngineScope, object> func;
            if (cache)
            {
                func = _expEngine.RealizedServices.GetOrAdd(type, scope => _expEngine.RuntimeResolver.Resolve(site, scope));
            }
            else
            {
                func = scope => _expEngine.RuntimeResolver.Resolve(site, scope);
                _expEngine.RealizedServices[type] = func;
            }
            if (func == null)
            {
                _logger.Warn("Cache miss");
                return;
            }
            var obj = func(_expEngine.Root);
            if (obj == null)
            {
                throw new NotImplementedException();
            }
        }


        static void GetExpService(Type type, Boolean cache)
        {
            var site = _expEngine.CallSiteFactory.CreateCallSite(type, new CallSiteChain());
            Func<ServiceProviderEngineScope, object> func;
            if (cache)
            {
                func = _expEngine.RealizedServices.GetOrAdd(type, _ => _expEngine.ExpressionResolverBuilder.Build(site));
            }
            else
            {
                func = _expEngine.ExpressionResolverBuilder.Build(site);
                _expEngine.RealizedServices[type] = func;
            }
            if (func == null)
            {
                _logger.Warn("Cache miss");
                return;
            }
            var obj = func(_expEngine.Root);
            
            if (obj == null)
            {
                throw new NotImplementedException();
            }
        }

        static void GetEmitService(Type type, Boolean cache)
        {
            var site = _emitEngine.CallSiteFactory.CreateCallSite(type, new CallSiteChain());
            Func<ServiceProviderEngineScope, object> func;
            if (cache)
            {
                func = _expEngine.RealizedServices.GetOrAdd(type, _ => _emitEngine.ExpressionResolverBuilder.Build(site));
            }
            else
            {
                func = _emitEngine.ExpressionResolverBuilder.Build(site);
                _emitEngine.RealizedServices[type] = func;
            }
            if (func == null)
            {
                _logger.Warn("Cache miss");
                return;
            }
            var obj = func(_emitEngine.Root);
            if (obj == null)
            {
                throw new NotImplementedException();
            }
        }

        static void TestFoo(Action<Type, Boolean> handle, String method, Boolean cache, Int32 count = -1)
        {
            _watch.Restart();
            var fooInterfaces = Assembly.GetExecutingAssembly().GetTypes()
                .Where(x => x.IsInterface && x.Name.StartsWith("IFoo"));
            if (count > -1)
            {
                fooInterfaces = fooInterfaces.Where(x => Int32.Parse(x.Name.Split('_')[1]) < count);
            }

            foreach (var item in fooInterfaces)
            {
                handle(item, cache);
            }
            _watch.Stop();
            _logger.Info("method {0}, cache {1}, target {2}, cost {3}",
                method, cache, "from IFoo_0 to IFoo_" + count, _watch.ElapsedMilliseconds);
        }

        static void TestBar(Action<Type, Boolean> handle, String method, Boolean cache, Type type)
        {
            _watch.Restart();
            handle(type, cache);
            _watch.Stop();
            _logger.Info("method {0}, cache {1}, target {2}, cost {3}",
                method, cache, type.Name, _watch.ElapsedMilliseconds);
        }
    }
}
