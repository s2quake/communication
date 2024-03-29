// MIT License
// 
// Copyright (c) 2024 Jeesu Choi
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

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

    internal IAdaptor Adaptor
    {
        get => _adaptor ?? throw new InvalidOperationException();
        set => _adaptor = value;
    }

    internal IService Service
    {
        get => _service ?? throw new InvalidOperationException();
        set => _service = value;
    }

    internal string ServiceName => Service.Name;

    internal IPeer Peer
    {
        get => _peer ?? throw new InvalidOperationException();
        set => _peer = value;
    }

    internal static InstanceBase Empty { get; } = new Instance();

    [InstanceMethod(InvokeMethod)]
    protected void Invoke(string name, Type[] types, object?[] args)
    {
        Adaptor.Invoke(this, name, types, args);
    }

    protected void Invoke((string name, Type[] types, object?[] args) info)
    {
        Adaptor.Invoke(this, info.name, info.types, info.args);
    }

    [InstanceMethod(InvokeOneWayMethod)]
    protected void InvokeOneWay(string name, Type[] types, object?[] args)
    {
        Adaptor.InvokeOneWay(this, name, types, args);
    }

    protected void InvokeOneWay((string name, Type[] types, object?[] args) info)
    {
        Adaptor.Invoke(this, info.name, info.types, info.args);
    }

    [InstanceMethod(InvokeGenericMethod)]
    protected T Invoke<T>(string name, Type[] types, object?[] args)
    {
        return Adaptor.Invoke<T>(this, name, types, args);
    }

    protected T Invoke<T>((string name, Type[] types, object?[] args) info)
    {
        return Adaptor.Invoke<T>(this, info.name, info.types, info.args);
    }

    [InstanceMethod(InvokeAsyncMethod)]
    protected Task InvokeAsync(string name, Type[] types, object?[] args, CancellationToken cancellationToken)
    {
        return Adaptor.InvokeAsync(this, name, types, args, cancellationToken);
    }

    protected Task InvokeAsync((string name, Type[] types, object?[] args) info, CancellationToken cancellationToken)
    {
        return Adaptor.InvokeAsync(this, info.name, info.types, info.args, cancellationToken);
    }

    [InstanceMethod(InvokeGenericAsyncMethod)]
    protected Task<T> InvokeAsync<T>(string name, Type[] types, object?[] args, CancellationToken cancellationToken)
    {
        return Adaptor.InvokeAsync<T>(this, name, types, args, cancellationToken);
    }

    protected Task<T> InvokeAsync<T>((string name, Type[] types, object?[] args) info, CancellationToken cancellationToken)
    {
        return Adaptor.InvokeAsync<T>(this, info.name, info.types, info.args, cancellationToken);
    }

    protected static (string, Type[], object?[]) Info<P>(MethodInfo methodInfo, Type serviceType, P arg)
    {
        return (MethodDescriptor.GenerateName(methodInfo, serviceType), new Type[] { typeof(P) }, new object?[] { arg });
    }

    protected static (string, Type[], object?[]) Info<P1, P2>(MethodInfo methodInfo, Type serviceType, P1 arg1, P2 arg2)
    {
        return (MethodDescriptor.GenerateName(methodInfo, serviceType), new Type[] { typeof(P1), typeof(P2) }, new object?[] { arg1, arg2 });
    }

    protected static (string, Type[], object?[]) Info<P1, P2, P3>(MethodInfo methodInfo, Type serviceType, P1 arg1, P2 arg2, P3 arg3)
    {
        return (MethodDescriptor.GenerateName(methodInfo, serviceType), new Type[] { typeof(P1), typeof(P2), typeof(P3) }, new object?[] { arg1, arg2, arg3 });
    }

    protected static (string, Type[], object?[]) Info<P1, P2, P3, P4>(MethodInfo methodInfo, Type serviceType, P1 arg1, P2 arg2, P3 arg3, P4 arg4)
    {
        return (MethodDescriptor.GenerateName(methodInfo, serviceType), new Type[] { typeof(P1), typeof(P2), typeof(P3), typeof(P4) }, new object?[] { arg1, arg2, arg3, arg4 });
    }

    #region Instance

    sealed class Instance : InstanceBase
    {
    }

    #endregion
}
