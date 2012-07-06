using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Permissions;

namespace Serialization
{
    public class GetWritableAttributes
    {
        private static readonly Dictionary<RuntimeTypeHandle, GetSet[][][]> PropertyAccess = new Dictionary<RuntimeTypeHandle, GetSet[][][]>();
        /// <summary>
        /// Dictionary of all the used objects to check if properties are different
        /// to those set during construction
        /// </summary>
        private static readonly Dictionary<RuntimeTypeHandle, object> Vanilla = new Dictionary<RuntimeTypeHandle, object>();

        public static Entry[] GetProperties(object obj)
        {
            var type = obj.GetType().TypeHandle;
            var accessors = GetAccessors(type);
			
			Radical.Log ("[Available Properties]");
			Radical.IndentLog();
			accessors[0].Select(a => {  Radical.Log ("{0}",a.Info.Name); return a;}).ToList();
			Radical.OutdentLog();
			Radical.Log ("[/Available Properties]");
			
			
            return (from a in accessors[0]
                    let value = a.Get(obj)
                    where value != null  && !value.Equals(a.Vanilla)
                    select new Entry()
                               {
                                   PropertyInfo = a.Info,
                                   MustHaveName = true,
                                   Value = value
                               }).ToArray();
        }

        public static Entry[] GetFields(object obj)
        {
            var type = obj.GetType().TypeHandle;
            var accessors = GetAccessors(type);

			Radical.Log ("[Available Fields]");
			Radical.IndentLog();
			accessors[1].Select(a => {  Radical.Log ("{0}", a.FieldInfo.Name); return a;}).ToList();
			Radical.OutdentLog();
			Radical.Log ("[/Available Fields]");
			

            return (from a in accessors[1]
                    let value = a.Get(obj)
                    where value != null && !value.Equals(a.Vanilla)
                    select new Entry()
                               {
                                   FieldInfo = a.FieldInfo,
                                   MustHaveName = true,
                                   Value = value
                               }).ToArray();
        }

        private static object GetVanilla(RuntimeTypeHandle type)
        {
			try
			{
	            object vanilla = null;
	            lock (Vanilla)
	            {
	                if (!Vanilla.TryGetValue(type, out vanilla))
	                {
	                    vanilla = UnitySerializer.CreateObject(Type.GetTypeFromHandle(type));
	                    Vanilla[type] = vanilla;
	                }
	            }
	            return vanilla;
			}
			catch
			{
				return null;
			}
        }

        private static GetSet[][] GetAccessors(RuntimeTypeHandle type)
        {
            lock (PropertyAccess)
            {
                var index = (UnitySerializer.IsChecksum ? 1 : 0) + (UnitySerializer.IsChecksum && UnitySerializer.IgnoreIds ? 1 : 0);
                
                GetSet[][][] collection;
                if (!PropertyAccess.TryGetValue(type, out collection))
                {
                    collection = new GetSet[3][][];
                    PropertyAccess[type] = collection;
                }
                var accessors = collection[index];
                if (accessors == null)
                {
                    object vanilla = GetVanilla(type);
					bool canGetVanilla = false;
					if(vanilla != null) {
						canGetVanilla = !vanilla.Equals(null);
					} 
                    var acs = new List<GetSet>();
                    var props = UnitySerializer.GetPropertyInfo(type)
					    .Select(p=> new { priority = ((SerializationPriorityAttribute)p.GetCustomAttributes(false).FirstOrDefault(a=>a is SerializationPriorityAttribute)) ?? new SerializationPriorityAttribute(100),
							             info=p})
						.OrderBy(p=>p.priority.Priority).Select(p=>p.info);
                    foreach (var p in props)
                    {
                        var getSet = new GetSetGeneric(p);
						if(!canGetVanilla) 
						{
							getSet.Vanilla = null;
						}
						else
						{
							getSet.Vanilla = getSet.Get(vanilla);
						}
                        acs.Add(getSet);
                    }
                    accessors = new GetSet[2][];
                    accessors[0] = acs.ToArray();
                    acs.Clear();
                    var fields = UnitySerializer.GetFieldInfo(type)
					              .Select(p=> new { priority = ((SerializationPriorityAttribute)p.GetCustomAttributes(false).FirstOrDefault(a=>a is SerializationPriorityAttribute)) ?? new SerializationPriorityAttribute(100),
							             info=p})
						.OrderBy(p=>p.priority.Priority).Select(p=>p.info);
                    foreach (var f in fields)
                    {
                        var getSet = new GetSetGeneric(f);
                        if(!canGetVanilla) 
						{
							getSet.Vanilla = null;
						}
						else
						{
							getSet.Vanilla = getSet.Get(vanilla);
						} 
                        acs.Add(getSet);
                    }
                    accessors[1] = acs.ToArray();

                    collection[index] = accessors;
                }
                return accessors;
            }
        }
    }
}