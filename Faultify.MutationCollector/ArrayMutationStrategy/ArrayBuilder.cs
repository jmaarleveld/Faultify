using System.Collections.Generic;
using Faultify.Core.Extensions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Utils;

namespace Faultify.MutationCollector.ArrayMutationStrategy
{
    /// <summary>
    ///     Builder for building arrays in IL-code.
    /// </summary>
    public class ArrayBuilder
    {

        /// <summary>
        ///     Creates array with the given length and array type.
        /// </summary>
        /// <param name="processor"></param>
        /// <param name="length"></param>
        /// <param name="arrayType"></param>
        /// <returns></returns>
        public List<Instruction> CreateArray(ILProcessor processor, int length, TypeReference arrayType)
        {
            OpCode opcodeTypeValueAssignment = arrayType.GetLdcOpCodeByTypeReference();
            OpCode stelem = arrayType.GetStelemByTypeReference();
            var arraySystemType = arrayType.ToSystemType();

            if (arraySystemType == typeof(long) || arraySystemType == typeof(ulong))
            {
                opcodeTypeValueAssignment = OpCodes.Ldc_I4;
            }

            List<Instruction> list = new List<Instruction>
            {
                processor.Create(OpCodes.Ldc_I4, length),
                processor.Create(OpCodes.Newarr, arrayType),
            };
            for (var i = 0; i < length; i++)
            {
                object random = RandomValueGenerator.GenerateValueForField(arraySystemType, 0);

                list.Add(processor.Create(OpCodes.Dup));

                if (length > int.MaxValue && length < int.MinValue)
                {
                    list.Add(processor.Create(OpCodes.Ldc_I8, i));
                }
                else
                {
                    list.Add(processor.Create(OpCodes.Ldc_I4, i));
                }

                if (arraySystemType == typeof(char))
                {
                    int charCode = (int) char.GetNumericValue((char) random);
                    list.Add(processor.Create(opcodeTypeValueAssignment, charCode));

                } else
                {
                    list.Add(processor.Create(opcodeTypeValueAssignment, random));
                }

                if (arraySystemType == typeof(long) || arraySystemType == typeof(ulong))
                {
                    list.Add(processor.Create(OpCodes.Conv_I8));
                }

                list.Add(processor.Create(stelem));
            }

            return list;
        }
    }
}
