using System.Xml.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using Docxtor.OpenXml.Internal;

namespace Docxtor.OpenXml.Merge;

internal sealed class RelationshipCopier
{
    private static readonly XNamespace RelationshipsNamespace = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace RelationshipsNamespaceStrict = "http://purl.oclc.org/ooxml/officeDocument/relationships";

    public void RewriteRelationshipsInElement(
        OpenXmlElement root,
        OpenXmlPartContainer sourceOwner,
        OpenXmlPartContainer destinationOwner,
        MergeContext context)
    {
        foreach (var element in OpenXmlPartHelpers.SelfAndDescendants(root))
        {
            var attributes = element.GetAttributes();
            var changed = false;

            for (var index = 0; index < attributes.Count; index++)
            {
                var attribute = attributes[index];
                if ((attribute.NamespaceUri == RelationshipsNamespace.NamespaceName ||
                     attribute.NamespaceUri == RelationshipsNamespaceStrict.NamespaceName) &&
                    !string.IsNullOrWhiteSpace(attribute.Value))
                {
                    attributes[index] = new OpenXmlAttribute(
                        attribute.Prefix,
                        attribute.LocalName,
                        attribute.NamespaceUri,
                        CopyRelationshipId(sourceOwner, destinationOwner, attribute.Value, context));
                    changed = true;
                }
            }

            if (changed)
            {
                element.SetAttributes(attributes);
            }
        }
    }

    public string CopyRelationshipId(
        OpenXmlPartContainer sourceOwner,
        OpenXmlPartContainer destinationOwner,
        string sourceRelationshipId,
        MergeContext context)
    {
        var cacheKey = $"{GetOwnerKey(sourceOwner)}->{GetOwnerKey(destinationOwner)}:{sourceRelationshipId}";
        if (context.RelationshipIdMap.TryGetValue(cacheKey, out var existing))
        {
            return existing;
        }

        var sourceLookup = GetSourceRelationshipLookup(sourceOwner, context);

        if (sourceLookup.InternalPartsById.TryGetValue(sourceRelationshipId, out var internalPart))
        {
            if (internalPart is ImagePart imagePart && context.Policy.ImageDeduplication)
            {
                var hash = OpenXmlPartHelpers.ComputeHash(imagePart);
                if (context.ImagePartsByHash.TryGetValue(hash, out var existingImagePart))
                {
                    var reusedPart = destinationOwner.AddPart(existingImagePart);
                    var reusedRelationshipId = GetRelationshipId(destinationOwner, reusedPart);
                    context.RelationshipIdMap[cacheKey] = reusedRelationshipId;
                    context.RemapSummary.RelationshipIds++;
                    context.RemapSummary.ImagesDeduplicated++;
                    return reusedRelationshipId;
                }
            }

            var addedPart = destinationOwner.AddPart(internalPart);
            var newRelationshipId = GetRelationshipId(destinationOwner, addedPart);
            context.RelationshipIdMap[cacheKey] = newRelationshipId;
            context.RemapSummary.RelationshipIds++;

            if (addedPart is ImagePart addedImagePart && context.Policy.ImageDeduplication)
            {
                var hash = OpenXmlPartHelpers.ComputeHash(addedImagePart);
                context.ImagePartsByHash.TryAdd(hash, addedImagePart);
            }

            return newRelationshipId;
        }

        if (sourceLookup.ExternalRelationshipsById.TryGetValue(sourceRelationshipId, out var externalRelationship))
        {
            var newRelationship = destinationOwner.AddExternalRelationship(
                externalRelationship.RelationshipType,
                externalRelationship.Uri);
            context.RelationshipIdMap[cacheKey] = newRelationship.Id;
            context.RemapSummary.RelationshipIds++;
            return newRelationship.Id;
        }

        if (destinationOwner is OpenXmlPart destinationPart &&
            sourceLookup.HyperlinkRelationshipsById.TryGetValue(sourceRelationshipId, out var hyperlinkRelationship))
        {
            var newRelationship = destinationPart.AddHyperlinkRelationship(
                hyperlinkRelationship.Uri,
                hyperlinkRelationship.IsExternal);
            context.RelationshipIdMap[cacheKey] = newRelationship.Id;
            context.RemapSummary.RelationshipIds++;
            return newRelationship.Id;
        }

        throw new InvalidOperationException(
            $"Relationship '{sourceRelationshipId}' could not be resolved for '{GetOwnerKey(sourceOwner)}'.");
    }

    private static MergeContext.RelationshipLookup GetSourceRelationshipLookup(
        OpenXmlPartContainer sourceOwner,
        MergeContext context)
    {
        if (context.SourceRelationshipLookups.TryGetValue(sourceOwner, out var lookup))
        {
            return lookup;
        }

        lookup = MergeContext.RelationshipLookup.Create(sourceOwner);
        context.SourceRelationshipLookups[sourceOwner] = lookup;
        return lookup;
    }

    private static string GetRelationshipId(OpenXmlPartContainer owner, OpenXmlPart part)
    {
        return owner.GetIdOfPart(part);
    }

    private static string GetOwnerKey(OpenXmlPartContainer owner)
    {
        return owner is OpenXmlPart part ? part.Uri.ToString() : owner.GetType().Name;
    }
}
