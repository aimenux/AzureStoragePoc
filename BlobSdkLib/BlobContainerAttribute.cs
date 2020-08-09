using System;
using System.Reflection;

namespace BlobSdkLib
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class BlobContainerAttribute : Attribute
    {
        public string ContainerName { get; set; }

        public static string GetContainerName<TBlobDocument>()
        {
            var blobDocumentType = typeof(TBlobDocument);
            var attribute = GetAttribute<BlobContainerAttribute>(blobDocumentType);
            if (attribute == null)
            {
                throw new Exception($"Missing {nameof(BlobContainerAttribute)} on type {blobDocumentType}");
            }

            return attribute.ContainerName.ToLower();
        }

        private static TAttribute GetAttribute<TAttribute>(MemberInfo type, bool inherit = true) where TAttribute : Attribute
        {
            return (TAttribute) Attribute.GetCustomAttribute(type, typeof(TAttribute), inherit);
        }
    }
}
