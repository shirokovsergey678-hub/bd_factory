using System.Collections.Generic;
using System.Linq;

public class NodeTreeBuilder
{
    public List<ProjectNode> Build(List<ProjectNode> flat)
    {
        var lookup = flat.ToDictionary(n => n.Id);

        foreach (var node in flat)
        {
            if (node.ParentId.HasValue && lookup.ContainsKey(node.ParentId.Value))
            {
                lookup[node.ParentId.Value].Children.Add(node);
            }
        }

        return flat.Where(n => n.ParentId == null).ToList();
    }
}