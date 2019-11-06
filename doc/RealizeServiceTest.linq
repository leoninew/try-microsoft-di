<Query Kind="Program">
  <NuGetReference>Microsoft.Extensions.Configuration</NuGetReference>
  <NuGetReference Version="2.1.1">Microsoft.Extensions.DependencyInjection</NuGetReference>
  <Namespace>Microsoft.Extensions.Configuration</Namespace>
  <Namespace>Microsoft.Extensions.DependencyInjection</Namespace>
  <Namespace>Microsoft.Extensions.DependencyInjection.Extensions</Namespace>
  <Namespace>System.Collections.Concurrent</Namespace>
  <Namespace>System.Configuration</Namespace>
</Query>

void Main() {
    var configuration = new ConfigurationBuilder().Build();
    var services = new ServiceCollection()
        //.AddSingleton(configuration)
        .AddTransient<IFoo1, Foo1>()
        //.AddTransient<IFoo2>(sp => new Foo2(sp.GetRequiredService<IFoo1>()));
        //.AddSingleton<Foo3>();
        .BuildServiceProvider();
    var engine = ReflectionExtensions.GetNonPublicField(services, "_engine");
    var realizedServices = (System.Collections.IDictionary)ReflectionExtensions.GetNonPublicProperty(engine, "RealizedServices");

    for (int i = 0; i < 3; i++) {
        services.GetRequiredService<IFoo1>();
        foreach (DictionaryEntry item in realizedServices) {
            var title = String.Format("Loop {0}, type {1}, hash {2}", i, ((Type)item.Key).FullName, item.Value.GetHashCode());
            //var context = new RealizedContext(((Type)item.Key).FullName, item.Value, item.Value.GetHashCode());
            item.Value.Dump(title, depth: 2); //仅被 LINQPad 支持
        }
        Thread.Sleep(10);
    }


    //    for (int i = 0; i < 4; i++) {
    //        services.GetRequiredService<IFoo1>();
    //        var list = new List<RealizedContext>();
    //        foreach (DictionaryEntry item in realizedServices) {
    //            var title = String.Format("{0} {1}, hash {2}", ((Type)item.Key).FullName, item.Value.GetType().Name, item.Value.GetHashCode());
    //            list.Add(new RealizedContext(((Type)item.Key).FullName, item.Value, item.Value.GetHashCode()));
    //        }
    //
    //        list.Dump("Loop #" + i, depth: 3); //仅被 LINQPad 支持
    //        Thread.Sleep(10);
    //    }
}

struct RealizedContext {
    public String Type;
    public Object Func;
    public Int32 Hash;

    public RealizedContext(String type, Object func, Int32 hash) {
        Type = type;
        Hash = hash;
        Func = func;
    }
}

class ReflectionExtensions {
    public static Object GetNonPublicField(Object instance, String name) {
        var type = instance.GetType();
        var field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
        return field.GetValue(instance);
    }

    public static Object GetNonPublicProperty(Object instance, String name) {
        var type = instance.GetType();
        var property = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Instance);
        return property.GetValue(instance);
    }
}

interface IFoo1 {
    void Hello();
}

class Foo1 : IFoo1 {
    public void Hello() {
        Console.WriteLine("Foo1.Hello()");
    }
}

//interface IFoo2 {
//    void Hi();
//}
//
//class Foo2 : IFoo2 {
//    private readonly IFoo1 _foo;
//    public Foo2(IFoo1 foo) {
//        _foo = foo;
//    }
//
//    public void Hi() {
//        _foo.Hello();
//        Console.WriteLine("Foo2.Hi()");
//    }
//}
//
//class Foo3 {
//    private readonly IConfigurationRoot _confg;
//    public Foo3(IConfigurationRoot config) {
//        _confg = config;
//    }
//
//    public void Greeting() {
//    }
//}