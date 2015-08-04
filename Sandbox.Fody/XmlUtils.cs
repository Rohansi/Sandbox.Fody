using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Sandbox.Fody
{
    static class XmlUtils
    {
        public static string AttributeValue(this XElement element, string name)
        {
            var attr = element.Attribute(name);
            return attr != null ? attr.Value : null;
        }

        public static IEnumerable<XElement> ElementsOf(this XElement parent, string elementName)
        {
            var element = parent.Element(elementName);
            if (element == null)
                return Enumerable.Empty<XElement>();

            return element.Elements();
        }
    }
}
