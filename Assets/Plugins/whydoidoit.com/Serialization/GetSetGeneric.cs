using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Permissions;
using System.Collections.Generic;

namespace Serialization
{
	//Using reflection to get and set properties so there is no
	//JIT compiliation
	public class GetSetGeneric : GetSet
	{
    	
		public GetSetGeneric (PropertyInfo info)
		{
			Name = info.Name;
			Info = info;
			CollectionType = Info.PropertyType.GetInterface ("IEnumerable", true) != null;
			var getMethod = info.GetGetMethod (true);
			var setMethod = info.GetSetMethod (true);
			Get = (o) => getMethod.Invoke (o, null);
			Set = (o,v) => {
				try {
					setMethod.Invoke (o, new [] {v});
				} catch (Exception e) {
					Radical.LogNow ("When setting {0} to {1} found {2}:", o.ToString(), v.ToString(), e.ToString ());
				}
			};
		}

		public GetSetGeneric (FieldInfo info)
		{
			Name = info.Name;
			FieldInfo = info;
			Get = info.GetValue;
			Set = info.SetValue;
			CollectionType = FieldInfo.FieldType.GetInterface ("IEnumerable", true) != null;
			return;
		}

		public GetSetGeneric (Type t, string name)
		{
			Name = name;
			var p = t.GetProperty (name);
			if (p == null) {
				FieldInfo = t.GetField (Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				Get = FieldInfo.GetValue;
				Set = FieldInfo.SetValue;
				CollectionType = FieldInfo.FieldType.GetInterface ("IEnumerable", true) != null;
				return;
			}
			Info = p;
			CollectionType = Info.PropertyType.GetInterface ("IEnumerable", true) != null;
			var getMethod = p.GetGetMethod (true);
			var setMethod = p.GetSetMethod (true);
			Get = (o) => getMethod.Invoke (o, null);
			Set = (o,v) => setMethod.Invoke (o, new [] {v});
			
			
			
		}

	}
	
	
	

}