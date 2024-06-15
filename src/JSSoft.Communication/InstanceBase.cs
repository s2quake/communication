// <copyright file="InstanceBase.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

// https://sharplab.io/#v2:C4LglgNgPgAgTARgLACgYAYAEMEBYDcqG2CArISqgHYCGAtgKYDOADjQMYOYBywATgwYA3AHQBhAXRriA9nToBXKmHY1gYGVVQBvVJn3Y4mAJJUmwGlU4AhGkwZ6DulAdfYAzNlwmqQmQGsGAAocLFpGABpMABUATxYGAG0AXUxgeOYomQAjACsGdmAUzBo+AHMmAEpHN0xnWv0ASGAACz4ZAHdMKgYu7hlgYzoWCAZGKmAGABMAUQAPThZ1TSDKigbMAF9UGrcYT2ifP0CAHmiAPhCEMPoGKLiE4vSEpiy8gqLU0orqlwb6hrNNqdbq9HgDIYjMYMCbTeaLZZUVbrBrbSh/Wr7bAANiOAQYAEEmLErFcbpFMGw+PQmJgcvlCsVvlVdq4AbUge0uj0+hDhqNxpNZgsGEsNEi1qyDGipfosTBsWdznjAkSSewlWTurcolSaXT3oyvuUWRi3Oy3JyQTzwYN+dDYcKEeLkbKtrs0a5dvAfOZLDY7AxIRBMCBfRYrAxbPZdhbXPLvKZjsFQiVyggomAJmmynBfhs4wZGkJSmkMrSALygro4EQPJKpbRlhIyABmZMqUWeDDbQSzwEqWxRG30Jb4Ocr1YNDM+dRzGZzRk2w8t2UDIiT+KC4R77c3gU7zcyE8lZtcnoabqx/ZVDAA4jCGHwVFrvgub98827C7Ux0fJzatb1sUTbdr2ADK/BZmUh5ge2/aDsubquH+zKYFWNr0h8IHzlEn5Dm6jQwAA7Jga72Buvj4ic/aXDuvb7vej7PuwsHlnhJqnhsF61FengKreaqkqmb6Ztmn5RNkMgyCG3zuPm/yEX+3YAWCQEZDhcFBJBz5UDBXYZL2CEGS27ZSTJiErhsqEmuhU5YUac6iYuHFlJ4SFnkWJFkeujFCew263AxVGqsSVhsS8rlVFZ0punxOI0RMyqMQ+PQsf5r7pmJwCLgptQ/pgxalipdmAQgdYaY2R4QVBekRbufYTJZyEGDZFSlWCDmzk2zn4R5GxEaR5EMJRyb+YlwB0UFe4hUxaUqP59WvCeMX6Dx7ooGiQA
// https://sharplab.io/#v2:C4LglgNgPgAgTARgLACgYAYAEMEDoAqAFgE4CmAhgCZgB2A5gNyobYICsTKqN5AtqQGcADuQDGpTADlgZUgDdcAYTK9ySgPa9eAVxphR5YGHU1UAb1SYr2OJgCSNAcHI1xAIXIDSl6xZTWA7ABmbAAWexo5dQBrUgAKHCwefgAaTHwATyFSAG0AXUxgLME09QAjACtSUWB8zHJiOgEASh9AzD92qwBIYBJ1AHdMGlIhyXVgO14hCFJ+GmBSSgBRAA9xISMTOObOLswAX1Q2wJgQ/Aio2IAefAA+BIQkvlI0zOy6ouyBUsrq2oKDSarX8XU6XV6/SGIzGEymMzmpAWSzWGy2NB2ey6Ry4oPaZ2wADZLjFSABBAQZVyPZ6pdLFT7FH6YcpVGp1IHMxQucQQCCGYw0fCkmiYAyuUh8gUmYWxGgg/bg9qQ4iDYajKRw6azeaLFbrUibQWYk4BHGm6wEmCE253EmxClU0S2mnDF5vBkFL4lFl/dmAxpcnmS/no2VIsXBqVhkUKsEWnp9VXQjXjSbaxHI/Vo427BOHE44gIneARJzBjxeeEQTAgMvOCWV7x4jr5q3hBxXeKJeqNBBpWjAXt0OBx9pKwLdOQNQpMzAAXnVQxwBE9HVn2XUADMac00t7t3FB81Dlj9lZp8RhwIF0vfWyAeugf3h7YDmfJ2VPKRcJ3SXFklIQ8/1iPcNx9Tk8xbM18zbEJB3tUgAHEkVIYh9FdZ8BwWV8x0CCd9kvcCb0XGFWFXD4CjMcDDwAZRkWg6DAg8d2PU98wCIjOVvMjWX+OpqKw192Og6xuhgAB2TAvy8X9IlJa5BweQDgPk2IUJGdDRGYpk0kgj8YNEqw4KJRDHVcBBML7bChyBOA0jKdR1BrIEgjSbkJWjQVw1FcVeVDbzY3zAixKI70SLvFd3lyKiaJ3ej0PoHTN1YhZkqAndHOck93w46wuMDHiNT4/0n2s189MaEJcqMzBxKkmSfxA8lKWpFSd2a8ztP3XTrzSPyQ2lIVYwM6wiy6EzrTM1rRDgKy6BfBC7IcpyXKqvCAhCnowrnUiNSitdqJYuIEsY9LD2PHqUriLKIBy0b2gKpoiqGErH0E8rluHaqHsCIjRFvDz/KGnzcHGEZfvq6TvzkrsuoAl5VLhmb0uZTl3KjAKZRFMGTFIKD9nG9pJptJTEI0tD9HhnshKWxp7MwIHBpjOVI08rHhrlDbfHzKcZ3Cl7yOigS4pOhikqujKjzSkTz3ymduL216/Xe4cXzs2WIUk6HZM6mbFIWZTEY6tTkNQrSutRyqmn6zGQZG/MiYLFAcSAA=
namespace JSSoft.Communication;

public abstract class InstanceBase
{
    public const string InvokeMethod = "Invoke";
    public const string InvokeOneWayMethod = "InvokeOneWay";
    public const string InvokeGenericMethod = "InvokeGeneric";
    public const string InvokeAsyncMethod = "InvokeAsync";
    public const string InvokeGenericAsyncMethod = "InvokeGenericAsync";

    private IAdaptor? _adaptor;
    private IService? _service;
    private IPeer? _peer;

    internal static InstanceBase Empty { get; } = new Instance();

    internal IAdaptor Adaptor
    {
        get => _adaptor ?? throw new InvalidOperationException("adaptor is not set.");
        set => _adaptor = value;
    }

    internal IService Service
    {
        get => _service ?? throw new InvalidOperationException("service is not set.");
        set => _service = value;
    }

    internal string ServiceName => Service.Name;

    internal IPeer Peer
    {
        get => _peer ?? throw new InvalidOperationException("peer is not set.");
        set => _peer = value;
    }

    [InstanceMethod(InvokeMethod)]
    protected void Invoke(string name, Type[] types, object?[] args)
        => Adaptor.Invoke(new InvokeOptions
        {
            Instance = this,
            Name = name,
            Types = types,
            Args = args,
        });

    protected void Invoke((string Name, Type[] Types, object?[] Args) info)
        => Adaptor.Invoke(new InvokeOptions
        {
            Instance = this,
            Name = info.Name,
            Types = info.Types,
            Args = info.Args,
        });

    [InstanceMethod(InvokeGenericMethod)]
    protected T Invoke<T>(string name, Type[] types, object?[] args)
        => Adaptor.Invoke<T>(new InvokeOptions
        {
            Instance = this,
            Name = name,
            Types = types,
            Args = args,
        });

    protected T Invoke<T>((string Name, Type[] Types, object?[] Args) info)
        => Adaptor.Invoke<T>(new InvokeOptions
        {
            Instance = this,
            Name = info.Name,
            Types = info.Types,
            Args = info.Args,
        });

    [InstanceMethod(InvokeOneWayMethod)]
    protected void InvokeOneWay(string name, Type[] types, object?[] args)
        => Adaptor.InvokeOneWay(new InvokeOptions
        {
            Instance = this,
            Name = name,
            Types = types,
            Args = args,
        });

    protected void InvokeOneWay((string Name, Type[] Types, object?[] Args) info)
        => Adaptor.Invoke(new InvokeOptions
        {
            Instance = this,
            Name = info.Name,
            Types = info.Types,
            Args = info.Args,
        });

    [InstanceMethod(InvokeAsyncMethod)]
    protected Task InvokeAsync(
        string name, Type[] types, object?[] args, CancellationToken cancellationToken)
    {
        var options = new InvokeOptions
        {
            Instance = this,
            Name = name,
            Types = types,
            Args = args,
        };
        return Adaptor.InvokeAsync(options, cancellationToken);
    }

    protected Task InvokeAsync(
        (string Name, Type[] Types, object?[] Args) info, CancellationToken cancellationToken)
    {
        var options = new InvokeOptions
        {
            Instance = this,
            Name = info.Name,
            Types = info.Types,
            Args = info.Args,
        };
        return Adaptor.InvokeAsync(options, cancellationToken);
    }

    [InstanceMethod(InvokeGenericAsyncMethod)]
    protected Task<T> InvokeAsync<T>(
        string name, Type[] types, object?[] args, CancellationToken cancellationToken)
    {
        var options = new InvokeOptions
        {
            Instance = this,
            Name = name,
            Types = types,
            Args = args,
        };
        return Adaptor.InvokeAsync<T>(options, cancellationToken);
    }

    protected Task<T> InvokeAsync<T>(
        (string Name, Type[] Types, object?[] Args) info, CancellationToken cancellationToken)
    {
        var options = new InvokeOptions
        {
            Instance = this,
            Name = info.Name,
            Types = info.Types,
            Args = info.Args,
        };
        return Adaptor.InvokeAsync<T>(options, cancellationToken);
    }

    private sealed class Instance : InstanceBase
    {
    }
}
