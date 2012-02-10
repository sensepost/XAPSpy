using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace XAPspy
{
    class XAPAssembly
    {
        private AssemblyDefinition _asmDef;
        private string _asmPath;       

        public XAPAssembly(string asmPath)
        {
            try
            {
                AssemblyDefinition def = AssemblyFactory.GetAssembly(asmPath);
                _asmDef = def;
                _asmPath = asmPath;
            }
            catch
            {
                throw new Exception("Error loading assembly:"+asmPath);
            }
            
        }
        public void InjectProlouge()
        {
            foreach (TypeDefinition tDef in _asmDef.MainModule.Types)
            {
                if (tDef.Name == "<Module>") continue;

                foreach (MethodDefinition mDef in tDef.Methods)
                {
                    //process method

                    PatchMethod(mDef,tDef);
                }
                //asmTarget.MainModule.Import(def2);  //check

            }
            AssemblyFactory.SaveAssembly(_asmDef, _asmPath);
        }
        private void PatchMethod(MethodDefinition methodDef, TypeDefinition typeDef)
        {
            
            MethodReference refWritelnStr = _asmDef.MainModule.Import(typeof(Console).GetMethod("WriteLine", new Type[] { typeof(string) }));
            MethodReference refWritelnInt = _asmDef.MainModule.Import(typeof(Console).GetMethod("WriteLine", new Type[] { typeof(Int32) }));
            MethodReference refWritelnObj = _asmDef.MainModule.Import(typeof(Console).GetMethod("WriteLine", new Type[] { typeof(object) }));
            MethodReference refBytetoString = _asmDef.MainModule.Import(typeof(BitConverter).GetMethod("ToString", new Type[] { typeof(byte[]) }));
            if (!methodDef.HasBody) return;    //check
            MethodBody body = methodDef.Body;

            //new injector code to dump parameters
            ushort i = 0;
            int varCount = 0;
            int paramCount = 0;
            Instruction varMethodName = body.CilWorker.Create(OpCodes.Ldstr, "==========\n" + "*Type:" + typeDef.FullName + " method name:" + methodDef.Name);
            body.CilWorker.InsertBefore(methodDef.Body.Instructions[0], varMethodName);
            Instruction logMethodName = body.CilWorker.Create(OpCodes.Call, refWritelnStr);
            body.CilWorker.InsertAfter(varMethodName, logMethodName);
            if (methodDef.HasParameters)
            {
                ParameterDefinitionCollection passedParams = methodDef.Parameters;
                if (!methodDef.IsStatic) paramCount = 1;               

                foreach (ParameterDefinition param in passedParams)
                {
                    varCount = methodDef.Body.Variables.Count;
                    i = System.Convert.ToUInt16(varCount);
                    if (param.ParameterType.Name == "String" || param.ParameterType.Name == "Int32" || param.ParameterType.Name == "Byte[]")
                    {
                        TypeReference paramType = param.ParameterType;

                        Instruction varParamName = body.CilWorker.Create(OpCodes.Ldstr, "*Param Name:" + param.Name);
                        body.CilWorker.InsertAfter(logMethodName, varParamName);
                        Instruction logParamName = body.CilWorker.Create(OpCodes.Call, refWritelnStr);
                        body.CilWorker.InsertAfter(varParamName, logParamName);
                        switch (param.ParameterType.Name)
                        {
                            case "String":

                                Instruction varStrValue = body.CilWorker.Create(OpCodes.Ldarg, paramCount);
                                body.CilWorker.InsertAfter(logParamName, varStrValue);
                                Instruction logStr = body.CilWorker.Create(OpCodes.Call, refWritelnStr);
                                body.CilWorker.InsertAfter(varStrValue, logStr);
                                break;
                            case "Int32":

                                Instruction varIntValue = body.CilWorker.Create(OpCodes.Ldarg, paramCount);
                                body.CilWorker.InsertAfter(logParamName, varIntValue);
                                Instruction logInt = body.CilWorker.Create(OpCodes.Call, refWritelnInt);
                                body.CilWorker.InsertAfter(varIntValue, logInt);
                                break;
                            case "Byte[]":
                                Instruction varByteValue = body.CilWorker.Create(OpCodes.Ldarg, paramCount);
                                body.CilWorker.InsertAfter(logParamName, varByteValue);
                                Instruction toString = body.CilWorker.Create(OpCodes.Call, refBytetoString);
                                body.CilWorker.InsertAfter(varByteValue, toString);
                                Instruction logByte = body.CilWorker.Create(OpCodes.Call, refWritelnObj);
                                body.CilWorker.InsertAfter(toString, logByte);
                                break;

                            default:
                                break;
                        }
                        
                    }
                    paramCount++;
                }
            }
        }
    }

    public class XAPAssemblyInfoEventArgs : EventArgs
    {
 
    }

}
