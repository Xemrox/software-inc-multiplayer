﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Multiplayer.Debugging;

namespace RoWa
{
    public static class XML
	{
        /// <summary>
        /// A Dictionary that can be saved to an XML file/string
        /// </summary>
        public class XMLDictionary
		{
            [XmlElement]
            public List<XMLDictionaryPair> Pairs = new List<XMLDictionaryPair>();
            public XMLDictionary() { }
            public XMLDictionary(Dictionary<object, object> dict)
			{
                foreach(KeyValuePair<object, object> kvp in dict)
                    Pairs.Add(new XMLDictionaryPair(kvp.Key, kvp.Value));
			}

            public bool Contains(object key)
			{
                if (Pairs.Find(x => x.Key == key) != null)
                    return true;
                else
                    return false;
			}

            public object GetValue(object key)
			{
                return Pairs.Find(x => x.Key == key);
			}

            public void Add(XMLDictionaryPair pair)
			{
                Pairs.Add(pair);
			}

            public void Add(object key, object value)
			{
                Add(new XMLDictionaryPair(key, value));
			}
		}

        /// <summary>
        /// A keyvaluepair of a XMLDictionary
        /// </summary>
        public class XMLDictionaryPair
		{
            [XmlElement]
            public object Key;
            [XmlElement]
            public object Value;
            public XMLDictionaryPair() { }
            public XMLDictionaryPair(object key, object value) { Key = key; Value = value; }
		}

        /// <summary>
        /// Transforms the object of type T into a XML string
        /// </summary>
        /// <typeparam name="T">The type of the object</typeparam>
        /// <param name="obj">The object</param>
        /// <returns>An XML representation fo the string</returns>
        public static string To<T>(T obj)
		{
            using (MemoryStream ms = new MemoryStream())
            {
                XmlSerializer serializer = new XmlSerializer(obj.GetType());
                using (StreamWriter stream = new StreamWriter(ms, Encoding.GetEncoding("UTF-8")))
                {
                    using (XmlWriter xmlWriter = XmlWriter.Create(stream, new XmlWriterSettings { Indent = false }))
                    {
                        serializer.Serialize(xmlWriter, obj);
                    }
                    ms.Position = 0;
                    return new StreamReader(ms).ReadToEnd();
                }
            }
        }

        /// <summary>
        /// Transforms the XML string to an object of type T
        /// </summary>
        /// <typeparam name="T">The type of the object</typeparam>
        /// <param name="xml">The XML string</param>
        /// <returns>The Object of type T from the XML string</returns>
        public static T From<T>(string xml)
		{
            using (MemoryStream ms = new MemoryStream(1024))
            {
                ms.Write(Encoding.UTF8.GetBytes(xml), 0, Encoding.UTF8.GetBytes(xml).Length);
                ms.Seek(0, SeekOrigin.Begin);

                //fix for encoding:
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                using (StreamReader stream = new StreamReader(ms, Encoding.GetEncoding("UTF-8"))) //using fires stream.close
                {
                    ms.Position = 0;
                    object o;
                    try
                    {
                        o = serializer.Deserialize(stream);
                    }
                    catch (System.Exception ex) { Logging.Error(ex.Message); o = null; }
                    return (T)o;
                }
            }
        }
	}
}
