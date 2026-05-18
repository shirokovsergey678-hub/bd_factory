using System.Collections.Generic;

public class ProjectNode
{
    public int Id;
    public int ProjectId;
    public int? ParentId;

    public string Name;
    public string Description;
    public int Quantity;

    public List<string> Schemes = new();
    public List<ProjectNode> Children;

    public bool IsExpanded;
    public int Level;
}
