using EmailDeliveryService.Model;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace EmailDeliveryService.Infrastructure
{
    class TemplateFactory
    {
        static Dictionary<string, Type> trackTypes;
        TemplateFactory()
        {
            LoadTypesICanReturn();
        }

        public static ITemplate<MailData> CreateInstance(string templateName)
        {
            LoadTypesICanReturn();
            Type t = GetTypeToCreate(templateName);

            if (t == null)
            {
                throw new NotSupportedException();
            }
            return Activator.CreateInstance(t) as ITemplate<MailData>;
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
               //if(type.BaseType != null && type.BaseType is BaseTemplate)
                    trackTypes.Add(type.Name, type);
            }
        }

        internal object CreateInstance<T1>(T1 templateName)
        {
            throw new NotImplementedException();
        }
    }
}
