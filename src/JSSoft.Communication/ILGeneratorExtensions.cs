// <copyright file="ILGeneratorExtensions.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System.Reflection.Emit;

namespace JSSoft.Communication;

internal static class ILGeneratorExtensions
{
    private static readonly OpCode[] LdcI4 =
    [
        OpCodes.Ldc_I4_0, OpCodes.Ldc_I4_1, OpCodes.Ldc_I4_2, OpCodes.Ldc_I4_3,
        OpCodes.Ldc_I4_4, OpCodes.Ldc_I4_5, OpCodes.Ldc_I4_6, OpCodes.Ldc_I4_7, OpCodes.Ldc_I4_8,
    ];

    private static readonly OpCode[] Ldarg =
    [
        OpCodes.Ldarg_0, OpCodes.Ldarg_1, OpCodes.Ldarg_2, OpCodes.Ldarg_3,
    ];

    public static void EmitLdc_I4(this ILGenerator il, int n)
    {
        if (n < LdcI4.Length)
        {
            il.Emit(LdcI4[n]);
        }
        else
        {
            il.Emit(OpCodes.Ldc_I4_S, n);
        }
    }

    public static void EmitLdarg(this ILGenerator il, int n)
    {
        if (n < Ldarg.Length)
        {
            il.Emit(Ldarg[n]);
        }
        else
        {
            il.Emit(OpCodes.Ldarg_S, n);
        }
    }
}
