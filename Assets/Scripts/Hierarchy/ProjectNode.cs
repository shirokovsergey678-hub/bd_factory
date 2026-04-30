using System.Collections.Generic;
//Узел проекта
public class ProjectNode
{
    public int Id;
    public int ProjectId;
    public int? ParentId;
    public string Name;

    public List<ProjectNode> Children;

    // UI состояние
    public bool IsExpanded;
    public int Level;
}