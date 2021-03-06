using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace ObjectPrinting
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class PrintingConfig<TOwner> : IPrintingConfig<TOwner>
    {
        private static readonly Type[] FinalTypes =
        {
            typeof(int), typeof(double), typeof(float), typeof(string),
            typeof(DateTime), typeof(TimeSpan), typeof(Guid)
        };

        private static readonly Type[] NumberTypes = {typeof(int), typeof(double), typeof(float)};


        private readonly Dictionary<MemberInfo, CultureInfo> customMemberCulture =
            new Dictionary<MemberInfo, CultureInfo>();

        private readonly HashSet<Type> excludedTypes = new HashSet<Type>();
        private readonly HashSet<MemberInfo> excludedMembers = new HashSet<MemberInfo>();

        private readonly Dictionary<Type, Func<object, string>> customTypeSerializers =
            new Dictionary<Type, Func<object, string>>();

        private readonly Dictionary<MemberInfo, Func<TOwner, string>> customMemberSerializers =
            new Dictionary<MemberInfo, Func<TOwner, string>>();

        private readonly Dictionary<MemberInfo, int> stringMembersMaxLength = new Dictionary<MemberInfo, int>();

        private int maxNestingLevel = 5;
        private int maxEnumerableElementsToPrint = 10;

        public string PrintToString(TOwner obj)
        {
            return PrintToString(obj, 0);
        }

        private string PrintToString(object obj, int nestingLevel, params object[] parents)
        {
            const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance;

            if (obj == null)
                return "null" + Environment.NewLine;

            if (FinalTypes.Contains(obj.GetType()))
                return obj + Environment.NewLine;

            var parentsIndex = Array.FindIndex(parents, o => ReferenceEquals(o, obj));
            if (parentsIndex != -1)
            {
                return $"Cyclic reference to level {parentsIndex}{Environment.NewLine}";
            }

            var indentation = new string('\t', nestingLevel + 1);
            var sb = new StringBuilder();
            var type = obj.GetType();
            sb.AppendLine(new string('\t', nestingLevel) + type.Name);

            if (obj is IEnumerable enumerable)
            {
                var firstElements = enumerable.Cast<object>().Take(maxEnumerableElementsToPrint + 1).ToList();
                if (firstElements.Count == maxEnumerableElementsToPrint + 1)
                    firstElements[maxEnumerableElementsToPrint] = indentation + "...";

                sb.AppendLine(indentation + "[");
                foreach (var element in firstElements)
                {
                    sb.Append(PrintToString(element, nestingLevel + 1, parents.Concat(new[] {obj}).ToArray()));
                }

                sb.AppendLine(indentation + "]");
                return sb.ToString();
            }

            if (nestingLevel >= maxNestingLevel) return sb.ToString();

            foreach (var memberInfo in type
                .GetFields(bindingFlags)
                .Cast<MemberInfo>()
                .Concat(type.GetProperties(bindingFlags))
                .Where(m => !excludedTypes.Contains(m.GetMemberType()))
                .Where(m => !excludedMembers.Contains(m))
                .OrderBy(m => m.Name))
            {
                string memberValue;
                if (customMemberSerializers.TryGetValue(memberInfo, out var serializer))
                    memberValue = serializer((TOwner) obj) + Environment.NewLine;
                else if (customMemberCulture.TryGetValue(memberInfo.GetMemberType(), out var culture))
                    memberValue = memberInfo.GetValue(obj).ToStringWithCulture(culture) + Environment.NewLine;
                else if (customTypeSerializers.TryGetValue(memberInfo.GetMemberType(), out var typeSerializer))
                    memberValue = typeSerializer(memberInfo.GetValue(obj)) + Environment.NewLine;
                else
                    memberValue = PrintToString(memberInfo.GetValue(obj), nestingLevel + 1,
                        parents.Concat(new[] {obj}).ToArray());

                if (memberInfo.GetMemberType() == typeof(string) &&
                    stringMembersMaxLength.TryGetValue(memberInfo, out var maxLength))
                {
                    memberValue =
                        memberValue.Substring(0, memberValue.Length - Environment.NewLine.Length).Truncate(maxLength) +
                        Environment.NewLine;
                }

                sb.Append($"{indentation}{memberInfo.Name} = {memberValue}");
            }

            return sb.ToString();
        }

        public PrintingConfig<TOwner> Exclude<TPropType>()
        {
            excludedTypes.Add(typeof(TPropType));
            return this;
        }

        public TypePrintingConfig<TOwner, TPropType> Serializing<TPropType>()
        {
            return new TypePrintingConfig<TOwner, TPropType>(this);
        }

        public PropertyPrintingConfig<TOwner, TPropType> Serializing<TPropType>(
            Expression<Func<TOwner, TPropType>> selector)
        {
            return new PropertyPrintingConfig<TOwner, TPropType>(this, selector);
        }

        public PrintingConfig<TOwner> Exclude<TPropType>(Expression<Func<TOwner, TPropType>> selector)
        {
            if (selector.Body.NodeType != ExpressionType.MemberAccess)
            {
                throw new ArgumentException();
            }

            var member = ((MemberExpression) selector.Body).Member;
            member.CheckCanParticipateInSerialization();
            excludedMembers.Add(member);

            return this;
        }

        public PrintingConfig<TOwner> WithMaxNestingLevel(int newValue)
        {
            maxNestingLevel = newValue;
            return this;
        }

        public PrintingConfig<TOwner> WithMaxEnumerableElementsToPrint(int newValue)
        {
            maxEnumerableElementsToPrint = newValue;
            return this;
        }

        void IPrintingConfig<TOwner>.SetCultureFor<TPropType>(CultureInfo cultureInfo)
        {
            customMemberCulture[typeof(TPropType)] = cultureInfo;
        }

        void IPrintingConfig<TOwner>.SetTrimmingFor(MemberInfo memberInfo, int maxLength)
        {
            memberInfo.CheckCanParticipateInSerialization();

            stringMembersMaxLength[memberInfo] = maxLength;
        }

        void IPrintingConfig<TOwner>.SetSerializerFor(MemberInfo memberInfo, Func<TOwner, string> serializer)
        {
            memberInfo.CheckCanParticipateInSerialization();

            customMemberSerializers[memberInfo] = serializer;
        }

        void IPrintingConfig<TOwner>.SetSerializerFor<T>(Func<T, string> serializer)
        {
            customTypeSerializers[typeof(TOwner)] = o => serializer((T) o);
        }
    }
}
