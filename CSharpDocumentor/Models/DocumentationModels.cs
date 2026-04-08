namespace CSharpDocumentor.Models
{
    public class MemberDocumentationRequest
    {
        public string MemberName { get; set; } = string.Empty;
        public string Signature { get; set; } = string.Empty;
    }

    public class ClassContext
    {
        public string ClassName { get; set; } = string.Empty;
        public string FullSource { get; set; } = string.Empty;
        public List<MemberDocumentationRequest> TargetMembers { get; set; } = new();
        public string ProjectName { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
    }

    public class DocumentationResponse
    {
        public string MemberName { get; set; } = string.Empty;
        public string XmlDoc { get; set; } = string.Empty;
    }
}
