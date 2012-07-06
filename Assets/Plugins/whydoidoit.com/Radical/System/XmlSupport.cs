using UnityEngine;
using System.Collections;
using System.Xml.Serialization;
using System.IO;
using System.Text;
using System;

public static class XmlSupport 
{
	public static T DeserializeXml<T> (this string xml) where T : class
	{
		var s = new XmlSerializer (typeof(T));
		using (var m = new MemoryStream (Encoding.UTF8.GetBytes (xml)))
		{
			return (T)s.Deserialize (m);
		}
		
	
	}
	
	public static object DeserializeXml(this string xml, Type tp) 
	{
		var s = new XmlSerializer (tp);
		using (var m = new MemoryStream (Encoding.UTF8.GetBytes (xml)))
		{
			return s.Deserialize (m);
		}
	}

	
	public static string SerializeXml (this object item)
	{
		var s = new XmlSerializer (item.GetType ());
		using (var m = new MemoryStream())
		{
			s.Serialize (m, item);
			m.Flush ();
			return Encoding.UTF8.GetString (m.GetBuffer ());
		}
		
		
	}
	
}

