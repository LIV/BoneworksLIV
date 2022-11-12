using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace LIVPathfinderSDK
{
    public enum PathfinderType
    {
        Container = 0,
        Pointer = 40,
        Buffer = 39,
        String = 36,
        Int = 9,
        UInt64 = 12,
        Float = 13,
        Texture = 27,
        Quaternion = 8,
        Matrix4x4 = 6,
        Vector3 = 30,
        RigidTransform = 38,
        Boolean = 42,
        CopyPath = 251,
        Struct = 252
    }

    // Contains an unmanaged pointer to a pathfinder object (copy of existing, or new if not found)
    public struct PFPointer
    {
        public IntPtr addr;
        public static int _type = (int)PathfinderType.Pointer;
    }

    public struct PFBuffer
    {
        public IntPtr addr;
        public int length;
        public int type;
        public static int _type = (int)PathfinderType.Buffer;
    }

    public static class SDKPathfinder
    {
        #region Interop
        [DllImport("LIV_Bridge.dll", EntryPoint = "LivSDKSet")]
        [SuppressUnmanagedCodeSecurity]
        private static extern int SetPFValue(string path, IntPtr value, int valuelength, int valuetype);

        //[DllImport("LIV_Bridge.dll", EntryPoint = "LivSDKSet")]
        //[SuppressUnmanagedCodeSecurity]
        //private static extern int PFSetVect3(string path, [In,Out] vect3 value, int valuelength, int valuetype);

        [DllImport("LIV_Bridge.dll", EntryPoint = "LivSDKGet")]
        [SuppressUnmanagedCodeSecurity]
        private static extern int PFGetValue(string path, IntPtr value, int valuelength, int valuetype);

        //[DllImport("LIV_Bridge.dll", EntryPoint = "LivSDKGet")]
        //[SuppressUnmanagedCodeSecurity]
        //private static extern int PFGetVect3(string path, [In,Out] vect3 value, int valuelength, int valuetype);

        [DllImport("LIV_Bridge.dll", EntryPoint = "NJSet")]
        [SuppressUnmanagedCodeSecurity]
        public static extern int PFSet(IntPtr nj, string path, IntPtr value, int valuelength, int valuetype);

        [DllImport("LIV_Bridge.dll", EntryPoint = "NJGet")]
        [SuppressUnmanagedCodeSecurity]
        public static extern int PFGet(IntPtr nj, string path, IntPtr value, int valuelength, int valuetype);

        [DllImport("LIV_Bridge.dll", CallingConvention = CallingConvention.Cdecl)]
        [SuppressUnmanagedCodeSecurity]
        public static extern IntPtr PFGetSmall(IntPtr nj, string path, int type);

        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        [SuppressUnmanagedCodeSecurity]
        private static extern int memcmp(IntPtr intPtr1, IntPtr intPtr2, UIntPtr count);
        #endregion

        #region Pathfinder Methods
        public static bool ComparePFPointer(PFPointer pf1, PFPointer pf2)
        {
            // check cases where pointer is zero for both or either
            if (pf1.addr == IntPtr.Zero && pf2.addr == IntPtr.Zero) return true;
            if (pf1.addr == IntPtr.Zero && pf2.addr != IntPtr.Zero) return false;
            if (pf1.addr != IntPtr.Zero && pf2.addr == IntPtr.Zero) return false;

            // read the first 2 bytes to get the length of each object, as per pathfinder, length starts at the pointer
            var len1 = Marshal.ReadInt16(pf1.addr);
            var len2 = Marshal.ReadInt16(pf2.addr);

            // if lengths are different, then the two objects are different
            if (len1 != len2)
            {
                return false;
            }

            // perform memory compare of objects
            var retVal = memcmp(pf1.addr, pf2.addr, (UIntPtr)len1);
            return retVal == 0;
        }

        public static PFPointer CreatePathfinderObject(int size = 65536)
        {
            var addr = Marshal.AllocHGlobal(size);
            Marshal.WriteInt32(addr, 262148);
            var pfPointer = new PFPointer();
            pfPointer.addr = addr;
            return pfPointer;
        }

        public static void InitializeRootNode(string rootNode, string pathCheck)
        {
            var pfCheck = SDKPathfinder.AsPFPointer(pathCheck);
            if (pfCheck.addr == IntPtr.Zero)
            {
                InitializeRootNode(rootNode);
            }
        }

        public static void InitializeRootNode(string rootNode)
        {
            var pf = SDKPathfinder.CreatePathfinderObject();
            SDKPathfinder.SetPFObject(rootNode, pf);
            SDKPathfinder.FreePFPointer(pf);
        }

        public static string GetPFPointerKey(PFPointer pf)
        {
            if (pf.addr == IntPtr.Zero) return null;

            if (Marshal.ReadByte(pf.addr + 2) == 4)
            {
                return string.Empty;
            }

            var bytes = new List<byte>();
            var loc = pf.addr + 4;

            while (true)
            {
                var charBuffer = Marshal.ReadByte(loc);
                if (charBuffer == 1) break;
                bytes.Add(charBuffer);
                loc += 1;
            }

            return Encoding.UTF8.GetString(bytes.ToArray());
        }

        public static object GetPFPointerValue(PFPointer pf)
        {
            if (pf.addr == IntPtr.Zero) return null;

            var type = Marshal.ReadByte(pf.addr + 3);

            var dataOffset = Marshal.ReadByte(pf.addr + 2) & ((1 << 6) - 1);
            var dataStart = pf.addr + dataOffset;

            var obj = new object();

            if (type == 36)
            {
                var data = Marshal.PtrToStringAnsi(dataStart);
                obj = data;
            }
            else if (type == 0)
            {
                var pfPointer = new PFPointer { addr = dataStart };
                obj = pfPointer;
            }
            else
            {
                if (type == 38) type = 14;

                var dataType = Type.GetType("System." + Enum.GetName(typeof(TypeCode), type));
                obj = Marshal.PtrToStructure(dataStart, dataType);
            }

            return obj;
        }


        public static bool GetPFValue<T>(string key, out T myStruct, int structType)
        {
            // C# managed code thunking - kind of annoying - can probably be made better - but this will do for initial implementation

            // Allocate T on stack (unmanaged)
            // pin it - mainly so we can get its address
            // pass it to getvalue (which will initialize its contents)
            // copy the value to mystruct target

            T value = default(T);

            GCHandle gc2 = GCHandle.Alloc(value, GCHandleType.Pinned);

            var result = PFGetValue(key, gc2.AddrOfPinnedObject(), Marshal.SizeOf(value), structType);

            myStruct = (T)gc2.Target;

            gc2.Free();

            return result != 0;
        }

        public static bool GetPFValue<T>(PFPointer pfPointer, string key, out T myStruct, int structType)
        {
            T value = default(T);

            GCHandle gc2 = GCHandle.Alloc(value, GCHandleType.Pinned);

            var result = PFGet(pfPointer.addr, key, gc2.AddrOfPinnedObject(), Marshal.SizeOf(value), structType);

            myStruct = (T)gc2.Target;

            gc2.Free();

            return result != 0;
        }

        public static bool SetPFValue<T>(string key, ref T mystruct, int structtype)
        {
            GCHandle gc1 = GCHandle.Alloc(mystruct, GCHandleType.Pinned);
            var result = SetPFValue(key, gc1.AddrOfPinnedObject(), Marshal.SizeOf(mystruct), structtype);
            gc1.Free();
            return result != 0;
        }

        public static bool SetPFValue(string key, ref float[] myarray, int size)
        {
            var addr = Marshal.AllocHGlobal(size * 4);
            Marshal.Copy(myarray, 0, addr, size);
            var result = SetPFValue(key, addr, size * 4, 252);
            Marshal.FreeHGlobal(addr);
            return result != 0;
        }

        public static bool SetPFValue<T>(PFPointer pfPointer, string key, ref T structValue, PathfinderType structType)
        {
            var resultHandle = GCHandle.Alloc(structValue, GCHandleType.Pinned);
            var result = PFSet(pfPointer.addr, key, resultHandle.AddrOfPinnedObject(), Marshal.SizeOf(structValue), (int)structType);

            resultHandle.Free();

            return result != 0;
        }

        public static bool SetPFObject(string key, PFPointer pfPointer)
        {
            var result = SetPFValue(key, pfPointer.addr, Marshal.ReadInt16(pfPointer.addr), (int)PathfinderType.Container);

            return result != 0;
        }

        public static bool SetPFObject(PFPointer pfOwner, string key, PFPointer pfPointer)
        {
            var result = PFSet(pfOwner.addr, key, pfPointer.addr, Marshal.ReadInt16(pfPointer.addr), (int)PathfinderType.Container);

            return result != 0;
        }

        public static bool CopyPath(string key, string path)
        {
            var utf8 = Encoding.UTF8;

            byte[] utfstring = utf8.GetBytes(path + char.MinValue);

            GCHandle gc1 = GCHandle.Alloc(utfstring, GCHandleType.Pinned);
            var result = SetPFValue(key, gc1.AddrOfPinnedObject(), utfstring.Length, (int)PathfinderType.CopyPath);
            gc1.Free();
            return result != 0;
        }

        public static bool GetPFString(string key, ref string mystring)
        {
            PFBuffer buf = default;
            GCHandle gc1 = GCHandle.Alloc(buf, GCHandleType.Pinned);
            var result = PFGetValue(key, gc1.AddrOfPinnedObject(), Marshal.SizeOf(buf), PFBuffer._type);
            if (result != 0)
            {
                mystring = Marshal.PtrToStringAnsi(((PFBuffer)gc1.Target).addr);
            }
            gc1.Free();

            return result != 0;
        }

        public static bool GetPFString(PFPointer pfPointer, string key, ref string mystring)
        {
            PFBuffer buf = default;
            GCHandle gc1 = GCHandle.Alloc(buf, GCHandleType.Pinned);
            var result = PFGet(pfPointer.addr, key, gc1.AddrOfPinnedObject(), Marshal.SizeOf(buf), PFBuffer._type);
            if (result != 0)
            {
                mystring = Marshal.PtrToStringAnsi(((PFBuffer)gc1.Target).addr);
            }

            gc1.Free();

            return result != 0;
        }

        public static bool SetPFString(string key, string stringValue)
        {
            var utf8 = Encoding.UTF8;

            byte[] utfstring = utf8.GetBytes(stringValue + char.MinValue);

            GCHandle gc1 = GCHandle.Alloc(utfstring, GCHandleType.Pinned);
            var result = SetPFValue(key, gc1.AddrOfPinnedObject(), utfstring.Length, (int)PathfinderType.String);
            gc1.Free();
            return result != 0;
        }

        public static bool SetPFString(PFPointer pfPointer, string key, string stringValue)
        {
            var utf8 = Encoding.UTF8;

            byte[] utfstring = utf8.GetBytes(key + char.MinValue);

            GCHandle gc1 = GCHandle.Alloc(utfstring, GCHandleType.Pinned);
            var result = PFSet(pfPointer.addr, key, gc1.AddrOfPinnedObject(), utfstring.Length, (int)PathfinderType.String);
            gc1.Free();
            return result != 0;
        }

        public static string AsString(string key)
        {
            var result = "";
            GetPFString(key, ref result);
            return result;
        }


        public static string AsString(PFPointer pfPointer, string key)
        {
            string result = "";
            GetPFString(pfPointer, key, ref result);
            return result;
        }

        public static int AsInt(string key)
        {
            GetPFValue(key, out int value, (int)PathfinderType.Int);
            return value;
        }

        public static int AsInt(PFPointer pfPointer, string key)
        {
            GetPFValue(pfPointer, key, out int value, (int)PathfinderType.Int);
            return value;
        }

        public static int AsIntQuick(PFPointer pfPointer, string key)
        {
            var retValue = PFGetSmall(pfPointer.addr, key, (int)PathfinderType.Int);
            var longVal = (long)retValue & 4294967295;
            return (int)longVal;
        }

        public static float AsFloat(string key)
        {
            GetPFValue(key, out float value, (int)PathfinderType.Float);
            return value;
        }

        public static float AsFloat(PFPointer pfPointer, string key)
        {
            GetPFValue(pfPointer, key, out float value, (int)PathfinderType.Float);
            return value;
        }

        public static UInt64 AsUInt64(string key)
        {
            GetPFValue(key, out UInt64 value, (int) PathfinderType.UInt64);
            return value;
        }
        
        public static UInt64 AsUInt64(PFPointer pfPointer, string key)
        {
            GetPFValue(pfPointer, key, out UInt64 value, (int) PathfinderType.UInt64);
            return value;
        }

        public static bool AsBool(string key)
        {
            GetPFValue(key, out int value, (int)PathfinderType.Boolean);
            return value != 0;
        }

        public static bool AsBool(PFPointer pfPointer, string key)
        {
            GetPFValue(pfPointer, key, out int value, (int)PathfinderType.Boolean);
            return value != 0;
        }

        public static PFPointer AsPFPointer(string key)
        {
            GetPFValue(key, out PFPointer value, 40);
            return value;
        }

        public static PFPointer AsPFPointer(PFPointer pfPointer, string key)
        {
            var ptr = PFGetSmall(pfPointer.addr, key, 40);
            var value = new PFPointer { addr = ptr };
            return value;
        }

        public static void FreePFPointer(PFPointer pfPointer)
        {
            Marshal.FreeHGlobal(pfPointer.addr);
        }
        #endregion
    }
}
