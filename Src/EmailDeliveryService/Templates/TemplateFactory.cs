using EmailDeliveryService.Model;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace EmailDeliveryService.Templates
{
    /// <summary>
    /// Abstract factory pattern
    /// https://en.wikipedia.org/wiki/Abstract_factory_pattern
    /// </summary>
    class TemplateFactory
    {
        static Dictionary<string, Type> trackTypes;
        TemplateFactory()
        {
            LoadTypesICanReturn();
        }

        public static ITemplate<Mail> CreateInstance(string templateName)
        {
            LoadTypesICanReturn();
            Type t = GetTypeToCreate(templateName);

            if (t == null)
            {
                throw new NotSupportedException();
            }
            return Activator.CreateInstance(t) as ITemplate<Mail>;
        }

        private static Type GetTypeToCreate(string typeName)
        {
            foreach (var track in trackTypes)
            {
                if (track.Key.Contains(typeName))
                {
                    return trackTypes[track.Key];
                }
            }
            return null;
        }

        private static void LoadTypesICanReturn()
        {
            trackTypes = new Dictionary<string, Type>();

            Type[] typesInThisAssembly = Assembly.GetExecutingAssembly().GetTypes();

            foreach (Type type in typesInThisAssembly)
            {
               //if (type.GetInterface(typeof(ITemplate<>).ToString()) != null)
                    trackTypes.Add(type.FullName, type);
            }
        }

        internal object CreateInstance<T1>(T1 templateName)
        {
            throw new NotImplementedException();
        }
    }
}
